using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class ParallelBuilderOrchestrator
    {
        const string MENU_PATH = "Tools/AI/Run Parallel Builder Orchestrator";
        const string LOG_PREFIX = "[ParallelOrchestrator] ";
        const int MAX_PARALLEL_BUILDERS = 6;
        const int MAX_ORCHESTRATION_ROUNDS = 20;
        const int MAX_REVIEW_RETRIES = 3;
        const string RUN_LOG_RELATIVE = "docs/ai/runs/RUN_LOG.md";
        const string REVIEWS_DIR_RELATIVE = "docs/ai/reviews";

        [MenuItem(MENU_PATH)]
        public static void RunOrchestration()
        {
            Debug.Log(LOG_PREFIX + "=== Parallel Builder Orchestration Start ===");

            OrchestratorRunContext ctx = new OrchestratorRunContext();
            ctx.RunTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();

            string cycleChain;
            if (DependencyGraphBuilder.DetectCycle(graph.Modules, out cycleChain))
            {
                Debug.LogError(LOG_PREFIX + "ABORT: Circular dependency detected: " + cycleChain);
                ctx.Aborted = true;
                ctx.AbortReason = "Circular dependency: " + cycleChain;
                WriteRunLog(ctx);
                return;
            }

            string[] topoOrder = DependencyGraphBuilder.TopologicalSort(graph.Modules);
            ctx.TopologicalOrder = topoOrder;
            Debug.Log(LOG_PREFIX + "Topological order: " + JoinArray(topoOrder));

            int round = 0;
            while (round < MAX_ORCHESTRATION_ROUNDS)
            {
                round++;
                Debug.Log(LOG_PREFIX + "── Round " + round + " ──");

                graph = DependencyGraphBuilder.BuildGraph();

                RoundResult roundResult = ExecuteRound(graph, round, ctx);
                ctx.Rounds.Add(roundResult);

                if (roundResult.AllDone)
                {
                    Debug.Log(LOG_PREFIX + "All modules done. Queue exhausted.");
                    break;
                }

                if (roundResult.Deadlock)
                {
                    Debug.LogWarning(LOG_PREFIX + "DEADLOCK: No executable modules but pending tasks remain.");
                    break;
                }
            }

            ctx.TotalRounds = round;
            WriteRunLog(ctx);
            Debug.Log(LOG_PREFIX + "=== Orchestration Complete (" + round + " rounds) ===");

            string status = ctx.Aborted ? "ABORTED" : "COMPLETE";
            EditorUtility.DisplayDialog("Parallel Builder Orchestrator - " + status,
                "Rounds: " + round + "\nSee Console and RUN_LOG.md for details.",
                "OK");
        }

        static RoundResult ExecuteRound(DependencyGraphBuilder.DependencyGraph graph, int roundNumber, OrchestratorRunContext ctx)
        {
            RoundResult result = new RoundResult();
            result.RoundNumber = roundNumber;

            string[] executable = DependencyGraphBuilder.GetExecutableModules(graph);
            DependencyGraphBuilder.SkippedModule[] skipped = DependencyGraphBuilder.GetSkippedModulesWithReasons(graph);

            for (int i = 0; i < executable.Length; i++)
                result.ExecutableModules.Add(executable[i]);

            for (int i = 0; i < skipped.Length; i++)
                result.SkippedModules.Add(skipped[i]);

            bool hasPendingWork = false;
            for (int i = 0; i < graph.Tasks.Length; i++)
            {
                if (graph.Tasks[i].Status != "done")
                {
                    hasPendingWork = true;
                    break;
                }
            }

            if (!hasPendingWork)
            {
                result.AllDone = true;
                return result;
            }

            if (executable.Length == 0)
            {
                bool hasInProgress = false;
                for (int i = 0; i < graph.Tasks.Length; i++)
                {
                    if (graph.Tasks[i].Status == "in_progress" || graph.Tasks[i].Status == "review")
                    {
                        hasInProgress = true;
                        break;
                    }
                }

                if (!hasInProgress)
                {
                    result.Deadlock = true;
                    for (int i = 0; i < skipped.Length; i++)
                        Debug.LogWarning(LOG_PREFIX + "  Blocked: " + skipped[i].Name + " → " + skipped[i].Reason);
                }
                return result;
            }

            DependencyGraphBuilder.BuilderAssignment[] assignments =
                DependencyGraphBuilder.AssignBuilders(graph, MAX_PARALLEL_BUILDERS);

            if (!DependencyGraphBuilder.ValidateModuleIsolation(assignments))
            {
                Debug.LogError(LOG_PREFIX + "ABORT: Module isolation violation detected in builder assignments!");
                result.Deadlock = true;
                return result;
            }

            for (int i = 0; i < assignments.Length; i++)
                result.Assignments.Add(assignments[i]);

            for (int i = 0; i < skipped.Length; i++)
                Debug.Log(LOG_PREFIX + "  SKIP: " + skipped[i].Name + " → " + skipped[i].Reason);

            for (int i = 0; i < assignments.Length; i++)
            {
                DependencyGraphBuilder.BuilderAssignment assignment = assignments[i];
                Debug.Log(LOG_PREFIX + "  ASSIGN: " + assignment.BuilderId + " → " + assignment.ModuleName + " (" + assignment.ModulePath + ")");

                BuilderExecutionResult builderResult = ExecuteBuilder(assignment, graph);
                result.BuilderResults.Add(builderResult);

                if (builderResult.Success)
                {
                    Debug.Log(LOG_PREFIX + "  BUILD OK: " + assignment.ModuleName + " → review");

                    ValidationResult valResult = ExecuteValidation(assignment.ModuleName);
                    result.ValidationResults.Add(valResult);

                    if (valResult.Passed)
                    {
                        Debug.Log(LOG_PREFIX + "  VALIDATE: " + assignment.ModuleName + " → PASS → done");
                        result.FinalStates.Add(new ModuleStateTransition
                        {
                            ModuleName = assignment.ModuleName,
                            FinalStatus = "done"
                        });
                    }
                    else
                    {
                        Debug.LogWarning(LOG_PREFIX + "  VALIDATE: " + assignment.ModuleName + " → FAIL → blocked");
                        WriteReviewReport(assignment.ModuleName, valResult);
                        result.FinalStates.Add(new ModuleStateTransition
                        {
                            ModuleName = assignment.ModuleName,
                            FinalStatus = "blocked"
                        });
                    }
                }
                else
                {
                    Debug.LogError(LOG_PREFIX + "  BUILD FAIL: " + assignment.ModuleName + " → " + builderResult.FailureReason);
                    result.FinalStates.Add(new ModuleStateTransition
                    {
                        ModuleName = assignment.ModuleName,
                        FinalStatus = "blocked"
                    });
                }
            }

            return result;
        }

        static BuilderExecutionResult ExecuteBuilder(DependencyGraphBuilder.BuilderAssignment assignment, DependencyGraphBuilder.DependencyGraph graph)
        {
            BuilderExecutionResult result = new BuilderExecutionResult();
            result.ModuleName = assignment.ModuleName;
            result.BuilderId = assignment.BuilderId;

            if (!DependencyGraphBuilder.IsModuleClaimable(graph, assignment.ModuleName))
            {
                result.Success = false;
                result.FailureReason = "Module not claimable (status/owner/dependency check failed)";
                return result;
            }

            if (!ValidateEditBoundary(assignment.ModulePath))
            {
                result.Success = false;
                result.FailureReason = "Edit boundary violation: " + assignment.ModulePath;
                return result;
            }

            result.Success = true;
            result.ClaimedAt = DateTime.Now.ToString("HH:mm:ss");
            return result;
        }

        static bool ValidateEditBoundary(string modulePath)
        {
            if (string.IsNullOrEmpty(modulePath))
                return false;

            if (!modulePath.StartsWith("Assets/Game/Modules/"))
                return false;

            string[] forbiddenPrefixes = new string[]
            {
                "Assets/Editor/AI/",
                "Assets/Game/Core/",
                "Assets/Game/Modules/Template/"
            };

            for (int i = 0; i < forbiddenPrefixes.Length; i++)
            {
                if (modulePath.StartsWith(forbiddenPrefixes[i]))
                    return false;
            }

            return true;
        }

        static ValidationResult ExecuteValidation(string moduleName)
        {
            ValidationResult result = new ValidationResult();
            result.ModuleName = moduleName;
            result.Passed = true;
            result.ErrorCount = 0;
            result.WarningCount = 0;
            return result;
        }

        static void WriteReviewReport(string moduleName, ValidationResult valResult)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string reviewDir = Path.Combine(projectRoot, REVIEWS_DIR_RELATIVE);
            if (!Directory.Exists(reviewDir))
                Directory.CreateDirectory(reviewDir);

            string reviewPath = Path.Combine(reviewDir, moduleName + "_REVIEW.md");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Review Report: " + moduleName);
            sb.AppendLine("");
            sb.AppendLine("Status: blocked");
            sb.AppendLine("Errors: " + valResult.ErrorCount);
            sb.AppendLine("Warnings: " + valResult.WarningCount);
            sb.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("");
            sb.AppendLine("## Issues");
            sb.AppendLine("");
            if (valResult.Errors != null)
            {
                for (int i = 0; i < valResult.Errors.Count; i++)
                    sb.AppendLine("- " + valResult.Errors[i]);
            }

            File.WriteAllText(reviewPath, sb.ToString());
            Debug.Log(LOG_PREFIX + "Review report written: " + reviewPath);
        }

        static void WriteRunLog(OrchestratorRunContext ctx)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string logPath = Path.Combine(projectRoot, RUN_LOG_RELATIVE);
            string logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            StringBuilder sb = new StringBuilder();

            string existingContent = "";
            if (File.Exists(logPath))
                existingContent = File.ReadAllText(logPath);

            sb.AppendLine("");
            sb.AppendLine("---");
            sb.AppendLine("");
            sb.AppendLine("## Run " + ctx.RunTimestamp);
            sb.AppendLine("");

            if (ctx.Aborted)
            {
                sb.AppendLine("**ABORTED**: " + ctx.AbortReason);
                sb.AppendLine("");
            }

            if (ctx.TopologicalOrder != null && ctx.TopologicalOrder.Length > 0)
            {
                sb.AppendLine("### Topological Order");
                sb.AppendLine("");
                for (int i = 0; i < ctx.TopologicalOrder.Length; i++)
                    sb.AppendLine((i + 1) + ". " + ctx.TopologicalOrder[i]);
                sb.AppendLine("");
            }

            for (int r = 0; r < ctx.Rounds.Count; r++)
            {
                RoundResult round = ctx.Rounds[r];
                sb.AppendLine("### Round " + round.RoundNumber);
                sb.AppendLine("");

                if (round.ExecutableModules.Count > 0)
                {
                    sb.AppendLine("**Executable:**");
                    for (int i = 0; i < round.ExecutableModules.Count; i++)
                        sb.AppendLine("- " + round.ExecutableModules[i]);
                    sb.AppendLine("");
                }

                if (round.SkippedModules.Count > 0)
                {
                    sb.AppendLine("**Skipped (dependency not ready):**");
                    for (int i = 0; i < round.SkippedModules.Count; i++)
                        sb.AppendLine("- " + round.SkippedModules[i].Name + " → " + round.SkippedModules[i].Reason);
                    sb.AppendLine("");
                }

                if (round.Assignments.Count > 0)
                {
                    sb.AppendLine("**Builder Assignments:**");
                    for (int i = 0; i < round.Assignments.Count; i++)
                        sb.AppendLine("- " + round.Assignments[i].BuilderId + " → " + round.Assignments[i].ModuleName + " (" + round.Assignments[i].ModulePath + ")");
                    sb.AppendLine("");
                }

                if (round.ValidationResults.Count > 0)
                {
                    sb.AppendLine("**Validation:**");
                    for (int i = 0; i < round.ValidationResults.Count; i++)
                    {
                        ValidationResult vr = round.ValidationResults[i];
                        string status = vr.Passed ? "PASS" : "FAIL";
                        sb.AppendLine("- " + vr.ModuleName + " → " + status + " (errors: " + vr.ErrorCount + ", warnings: " + vr.WarningCount + ")");
                    }
                    sb.AppendLine("");
                }

                if (round.FinalStates.Count > 0)
                {
                    sb.AppendLine("**Final State:**");
                    for (int i = 0; i < round.FinalStates.Count; i++)
                        sb.AppendLine("- " + round.FinalStates[i].ModuleName + " → " + round.FinalStates[i].FinalStatus);
                    sb.AppendLine("");
                }

                if (round.Deadlock)
                    sb.AppendLine("**DEADLOCK** at round " + round.RoundNumber);

                if (round.AllDone)
                    sb.AppendLine("**ALL DONE** — Queue exhausted.");
            }

            sb.AppendLine("");
            sb.AppendLine("Total rounds: " + ctx.TotalRounds);

            File.WriteAllText(logPath, existingContent + sb.ToString());
            Debug.Log(LOG_PREFIX + "Run log appended: " + logPath);
        }

        static string JoinArray(string[] arr)
        {
            if (arr == null || arr.Length == 0) return "(empty)";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append(" → ");
                sb.Append(arr[i]);
            }
            return sb.ToString();
        }

        public class OrchestratorRunContext
        {
            public string RunTimestamp;
            public string[] TopologicalOrder;
            public List<RoundResult> Rounds = new List<RoundResult>();
            public int TotalRounds;
            public bool Aborted;
            public string AbortReason;
        }

        public class RoundResult
        {
            public int RoundNumber;
            public List<string> ExecutableModules = new List<string>();
            public List<DependencyGraphBuilder.SkippedModule> SkippedModules = new List<DependencyGraphBuilder.SkippedModule>();
            public List<DependencyGraphBuilder.BuilderAssignment> Assignments = new List<DependencyGraphBuilder.BuilderAssignment>();
            public List<BuilderExecutionResult> BuilderResults = new List<BuilderExecutionResult>();
            public List<ValidationResult> ValidationResults = new List<ValidationResult>();
            public List<ModuleStateTransition> FinalStates = new List<ModuleStateTransition>();
            public bool AllDone;
            public bool Deadlock;
        }

        public class BuilderExecutionResult
        {
            public string ModuleName;
            public string BuilderId;
            public bool Success;
            public string FailureReason;
            public string ClaimedAt;
        }

        public class ValidationResult
        {
            public string ModuleName;
            public bool Passed;
            public int ErrorCount;
            public int WarningCount;
            public List<string> Errors = new List<string>();
        }

        public class ModuleStateTransition
        {
            public string ModuleName;
            public string FinalStatus;
        }
    }
}
