using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class AutonomousPipeline
    {
        const string MENU_PREFIX = "Tools/AI/";
        const string LOG_PREFIX = "[AutonomousPipeline] ";
        const string FEATURE_RUNS_RELATIVE = "docs/ai/feature_runs";
        const int MAX_PIPELINE_ROUNDS = 20;

        public struct PipelineResult
        {
            public string FeatureName;
            public string FeatureGroup;
            public bool IntakeOk;
            public bool DecomposeOk;
            public bool QueueGenOk;
            public bool SpecGenOk;
            public bool OrchestrationOk;
            public bool ValidationOk;
            public bool CommitOk;
            public string[] GeneratedModules;
            public string[] GeneratedSpecs;
            public string CommitHash;
            public string Error;
        }

        [MenuItem(MENU_PREFIX + "Feature Pipeline/1. Check Feature Queue Status")]
        public static void CheckFeatureQueueStatus()
        {
            Debug.Log(LOG_PREFIX + "=== Feature Queue Status ===");

            FeatureIntake.FeatureEntry[] features = FeatureIntake.LoadFeatureQueue();
            if (features.Length == 0)
            {
                Debug.Log(LOG_PREFIX + "Feature queue is empty");
                EditorUtility.DisplayDialog("Feature Queue", "No features in queue.", "OK");
                return;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < features.Length; i++)
            {
                sb.AppendLine((i + 1) + ". " + features[i].Name + " [" + features[i].Status + "] (group: " + features[i].FeatureGroup + ")");
                if (!string.IsNullOrEmpty(features[i].Description))
                    sb.AppendLine("   " + features[i].Description.Split('\n')[0]);
            }

            Debug.Log(LOG_PREFIX + sb.ToString());
            EditorUtility.DisplayDialog("Feature Queue (" + features.Length + " features)",
                sb.ToString(), "OK");
        }

        [MenuItem(MENU_PREFIX + "Feature Pipeline/2. Check Feature Group Commit Readiness")]
        public static void CheckCommitReadiness()
        {
            Debug.Log(LOG_PREFIX + "=== Feature Group Commit Readiness ===");

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            FeatureGroupTracker.FeatureGroupStatus[] groups = FeatureGroupTracker.ScanTaskQueueForFeatureGroups(graph);

            if (groups.Length == 0)
            {
                Debug.Log(LOG_PREFIX + "No feature groups found in TASK_QUEUE");
                EditorUtility.DisplayDialog("Commit Readiness", "No feature groups found.", "OK");
                return;
            }

            string report = FeatureGroupTracker.FormatGroupStatusReport(groups);
            Debug.Log(LOG_PREFIX + report);

            string[] readyGroups = FeatureGroupTracker.GetCommitReadyGroups(graph);
            string readyMsg = readyGroups.Length > 0
                ? "Commit-ready groups: " + JoinArray(readyGroups)
                : "No groups ready for commit";

            EditorUtility.DisplayDialog("Commit Readiness", report + "\n" + readyMsg, "OK");
        }

        [MenuItem(MENU_PREFIX + "Feature Pipeline/3. Dry Run Commit (Ready Groups)")]
        public static void DryRunCommitReadyGroups()
        {
            Debug.Log(LOG_PREFIX + "=== Dry Run Commit ===");

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            string[] readyGroups = FeatureGroupTracker.GetCommitReadyGroups(graph);

            if (readyGroups.Length == 0)
            {
                Debug.Log(LOG_PREFIX + "No groups ready for commit");
                EditorUtility.DisplayDialog("Dry Run", "No groups ready for commit.", "OK");
                return;
            }

            FeatureGroupTracker.FeatureGroupStatus[] allGroups = FeatureGroupTracker.ScanTaskQueueForFeatureGroups(graph);

            StringBuilder resultSb = new StringBuilder();
            for (int i = 0; i < readyGroups.Length; i++)
            {
                FeatureGroupTracker.FeatureGroupStatus groupStatus = FindGroupStatus(allGroups, readyGroups[i]);
                GitCommitStage.CommitCandidate candidate = GitCommitStage.PrepareCommit(readyGroups[i], groupStatus, graph);
                GitCommitStage.CommitResult result = GitCommitStage.DryRunCommit(candidate);

                resultSb.AppendLine(readyGroups[i] + ": " + (result.Success ? "READY" : "NOT READY — " + result.Error));
            }

            Debug.Log(LOG_PREFIX + resultSb.ToString());
            EditorUtility.DisplayDialog("Dry Run Results", resultSb.ToString(), "OK");
        }

        [MenuItem(MENU_PREFIX + "Feature Pipeline/4. Execute Commit (Ready Groups)")]
        public static void ExecuteCommitReadyGroups()
        {
            Debug.Log(LOG_PREFIX + "=== Execute Commit ===");

            GitCommitStage.ValidationReportSnapshot reportCheck = GitCommitStage.ReadLatestValidationReport();
            if (!reportCheck.Exists)
            {
                Debug.LogError(LOG_PREFIX + "AIValidationReport.json not found — run validation first");
                EditorUtility.DisplayDialog("Commit BLOCKED",
                    "AIValidationReport.json not found.\n\nRun Tools/AI/Validate Generated Modules first.", "OK");
                return;
            }
            if (!reportCheck.Passed)
            {
                Debug.LogError(LOG_PREFIX + "Validation report shows " + reportCheck.ErrorCount
                    + " blocking error(s) — commit blocked");
                EditorUtility.DisplayDialog("Commit BLOCKED",
                    "Validation report has " + reportCheck.ErrorCount + " error(s).\n\nFix all errors and re-run validation before commit.", "OK");
                return;
            }

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            string[] readyGroups = FeatureGroupTracker.GetCommitReadyGroups(graph);

            if (readyGroups.Length == 0)
            {
                Debug.Log(LOG_PREFIX + "No groups ready for commit");
                EditorUtility.DisplayDialog("Commit", "No groups ready for commit.", "OK");
                return;
            }

            FeatureGroupTracker.FeatureGroupStatus[] allGroups = FeatureGroupTracker.ScanTaskQueueForFeatureGroups(graph);

            bool confirm = EditorUtility.DisplayDialog("Confirm Commit",
                "Will commit " + readyGroups.Length + " feature group(s):\n" + JoinArray(readyGroups) +
                "\n\nThis will create git commits. Continue?",
                "Commit", "Cancel");

            if (!confirm)
            {
                Debug.Log(LOG_PREFIX + "Commit cancelled by user");
                return;
            }

            StringBuilder resultSb = new StringBuilder();
            for (int i = 0; i < readyGroups.Length; i++)
            {
                FeatureGroupTracker.FeatureGroupStatus groupStatus = FindGroupStatus(allGroups, readyGroups[i]);
                GitCommitStage.CommitCandidate candidate = GitCommitStage.PrepareCommit(readyGroups[i], groupStatus, graph);
                GitCommitStage.CommitResult result = GitCommitStage.ExecuteCommit(candidate);

                if (result.Success)
                {
                    resultSb.AppendLine(readyGroups[i] + ": COMMITTED (" + result.CommitHash + ")");
                    FeatureIntake.UpdateFeatureStatus(readyGroups[i], "done");
                }
                else
                {
                    resultSb.AppendLine(readyGroups[i] + ": FAILED — " + result.Error);
                }
            }

            Debug.Log(LOG_PREFIX + resultSb.ToString());
            EditorUtility.DisplayDialog("Commit Results", resultSb.ToString(), "OK");
        }

        [MenuItem(MENU_PREFIX + "Feature Pipeline/5. Simulate Full Pipeline (Combat Core Example)")]
        public static void SimulateFullPipeline()
        {
            Debug.Log(LOG_PREFIX + "=== Full Pipeline Simulation (Combat Core) ===");
            Debug.Log(LOG_PREFIX + "");

            PipelineResult pipelineResult = new PipelineResult();

            Debug.Log(LOG_PREFIX + "── Layer A: Feature Intake ──");
            FeatureIntake.FeatureEntry feature = FeatureIntake.CreateFeatureEntry(
                "Combat Core",
                "전투 루프 전체 구현. 체력, 데미지, 탐지, 쿨다운, 버프/디버프 지원.",
                "high",
                "combat-core",
                new string[] { "StatusEffect 모듈 재사용", "Economy 직접 의존 없음" },
                new string[] { "docs/ai/PROJECT_OVERVIEW.md" }
            );
            feature.Modules = new string[]
            {
                "HealthSystem",
                "DamageSystem",
                "EnemyDetection",
                "CooldownSystem",
                "BuffSystem"
            };

            pipelineResult.FeatureName = feature.Name;
            pipelineResult.FeatureGroup = feature.FeatureGroup;
            pipelineResult.IntakeOk = true;
            Debug.Log(LOG_PREFIX + "Feature intake: " + feature.Name + " (group: " + feature.FeatureGroup + ")");
            Debug.Log(LOG_PREFIX + "Modules: " + JoinArray(feature.Modules));
            Debug.Log(LOG_PREFIX + "");

            Debug.Log(LOG_PREFIX + "── Layer B: Feature Decomposition ──");
            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            FeatureDecomposer.DecompositionResult decomposition = FeatureDecomposer.Decompose(feature, graph);

            if (!decomposition.Success)
            {
                Debug.LogError(LOG_PREFIX + "Decomposition failed: " + decomposition.Error);
                pipelineResult.Error = decomposition.Error;
                ShowPipelineResult(pipelineResult);
                return;
            }

            pipelineResult.DecomposeOk = true;
            Debug.Log(LOG_PREFIX + FeatureDecomposer.FormatDecompositionReport(decomposition));
            Debug.Log(LOG_PREFIX + "");

            Debug.Log(LOG_PREFIX + "── Layer C: TASK_QUEUE Generation ──");
            TaskQueueGenerator.GenerationResult queueResult = TaskQueueGenerator.GenerateFromDecomposition(decomposition);

            if (!queueResult.Success)
            {
                Debug.LogWarning(LOG_PREFIX + "Queue generation: " + queueResult.Error);
            }
            else
            {
                pipelineResult.QueueGenOk = true;
                pipelineResult.GeneratedModules = new string[queueResult.Entries.Length];
                for (int i = 0; i < queueResult.Entries.Length; i++)
                {
                    pipelineResult.GeneratedModules[i] = queueResult.Entries[i].Name;
                    Debug.Log(LOG_PREFIX + "Queue entry: " + queueResult.Entries[i].Name +
                        " (group: " + queueResult.Entries[i].FeatureGroup +
                        ", depends: [" + JoinArray(queueResult.Entries[i].DependsOn) + "])");
                }
            }
            Debug.Log(LOG_PREFIX + "");

            Debug.Log(LOG_PREFIX + "── Layer D: Spec Generation ──");
            if (decomposition.Modules != null && decomposition.Modules.Length > 0)
            {
                pipelineResult.GeneratedSpecs = new string[decomposition.Modules.Length];
                for (int i = 0; i < decomposition.Modules.Length; i++)
                {
                    SpecGenerator.ModuleSpec spec = SpecGenerator.CreateSpec(decomposition.Modules[i]);
                    pipelineResult.GeneratedSpecs[i] = spec.ModuleName + "_SPEC.md";
                    Debug.Log(LOG_PREFIX + "Spec generated: " + spec.ModuleName);
                    Debug.Log(LOG_PREFIX + "  Purpose: " + spec.Purpose);
                    Debug.Log(LOG_PREFIX + "  Factory: " + spec.FactoryResponsibility);
                }
                pipelineResult.SpecGenOk = true;
            }
            Debug.Log(LOG_PREFIX + "");

            Debug.Log(LOG_PREFIX + "── Layer E: Orchestration (Simulated) ──");
            Debug.Log(LOG_PREFIX + "Pipeline simulation: Planner → Builder → Reviewer flow");
            Debug.Log(LOG_PREFIX + "  (In production, this delegates to ParallelBuilderOrchestrator)");

            if (queueResult.Success && queueResult.Entries != null)
            {
                SimulateOrchestration(queueResult.Entries);
                pipelineResult.OrchestrationOk = true;
            }
            Debug.Log(LOG_PREFIX + "");

            Debug.Log(LOG_PREFIX + "── Layer F: Validation Gate ──");
            GitCommitStage.ValidationReportSnapshot reportSnapshot = GitCommitStage.ReadLatestValidationReport();
            if (!reportSnapshot.Exists)
            {
                Debug.LogError(LOG_PREFIX + "  AIValidationReport.json NOT FOUND — run Tools/AI/Validate Generated Modules first");
                pipelineResult.ValidationOk = false;
                pipelineResult.Error = "Validation report not found";
                ShowPipelineResult(pipelineResult);
                return;
            }

            if (!reportSnapshot.Passed)
            {
                Debug.LogError(LOG_PREFIX + "  Validation FAILED — " + reportSnapshot.ErrorCount + " error(s), "
                    + reportSnapshot.WarningCount + " warning(s)");
                Debug.LogError(LOG_PREFIX + "  Cannot proceed to commit. Fix all blocking errors first.");
                pipelineResult.ValidationOk = false;
                pipelineResult.Error = "Validation failed with " + reportSnapshot.ErrorCount + " error(s)";
                ShowPipelineResult(pipelineResult);
                return;
            }

            pipelineResult.ValidationOk = true;
            Debug.Log(LOG_PREFIX + "  Validation PASSED (errors: 0, warnings: " + reportSnapshot.WarningCount + ")");
            Debug.Log(LOG_PREFIX + "");

            Debug.Log(LOG_PREFIX + "── Layer G: Git Commit Stage (Simulated) ──");
            Debug.Log(LOG_PREFIX + "  Feature group: " + feature.FeatureGroup);
            Debug.Log(LOG_PREFIX + "  All modules done → COMMIT-READY");

            string commitMessage = BuildSimulatedCommitMessage(feature);
            Debug.Log(LOG_PREFIX + "  Commit message preview:");
            string[] msgLines = commitMessage.Split('\n');
            for (int i = 0; i < msgLines.Length; i++)
                Debug.Log(LOG_PREFIX + "    " + msgLines[i]);

            pipelineResult.CommitOk = true;
            pipelineResult.CommitHash = "(simulated)";
            Debug.Log(LOG_PREFIX + "");

            ShowPipelineResult(pipelineResult);
            WriteFeatureRunLog(pipelineResult);
        }

        static void SimulateOrchestration(TaskQueueGenerator.GeneratedTaskEntry[] entries)
        {
            var statusMap = new Dictionary<string, string>();
            for (int i = 0; i < entries.Length; i++)
                statusMap[entries[i].Name] = "planned";

            int round = 0;
            while (round < MAX_PIPELINE_ROUNDS)
            {
                round++;
                var executable = new List<string>();

                for (int i = 0; i < entries.Length; i++)
                {
                    if (statusMap[entries[i].Name] != "planned") continue;

                    bool ready = true;
                    for (int d = 0; d < entries[i].DependsOn.Length; d++)
                    {
                        string depStatus;
                        if (statusMap.TryGetValue(entries[i].DependsOn[d], out depStatus))
                        {
                            if (depStatus != "done") { ready = false; break; }
                        }
                    }
                    if (ready) executable.Add(entries[i].Name);
                }

                if (executable.Count == 0)
                {
                    bool allDone = true;
                    for (int i = 0; i < entries.Length; i++)
                    {
                        if (statusMap[entries[i].Name] != "done")
                        {
                            allDone = false;
                            break;
                        }
                    }
                    if (allDone)
                    {
                        Debug.Log(LOG_PREFIX + "  Orchestration complete in " + (round - 1) + " rounds");
                    }
                    break;
                }

                Debug.Log(LOG_PREFIX + "  Round " + round + ": parallel build [" + JoinArray(executable.ToArray()) + "]");
                for (int i = 0; i < executable.Count; i++)
                    statusMap[executable[i]] = "done";
            }
        }

        static string BuildSimulatedCommitMessage(FeatureIntake.FeatureEntry feature)
        {
            GitCommitStage.ValidationReportSnapshot snap = GitCommitStage.ReadLatestValidationReport();
            string valLabel = snap.Passed ? "PASS" : "FAIL (errors: " + snap.ErrorCount + ")";

            StringBuilder sb = new StringBuilder();
            sb.Append("feat(");
            sb.Append(feature.FeatureGroup);
            sb.Append("): add ");
            for (int i = 0; i < feature.Modules.Length; i++)
            {
                if (i > 0 && i == feature.Modules.Length - 1)
                    sb.Append(" and ");
                else if (i > 0)
                    sb.Append(", ");
                sb.Append(feature.Modules[i]);
            }
            sb.AppendLine(" modules");
            sb.AppendLine("");
            sb.AppendLine("- modules:");
            for (int i = 0; i < feature.Modules.Length; i++)
                sb.AppendLine("  - " + feature.Modules[i]);
            sb.AppendLine("- validation: " + valLabel);
            if (snap.WarningCount > 0)
                sb.AppendLine("- validation_warnings: " + snap.WarningCount);
            sb.AppendLine("- generated by: Autonomous Game Factory v2");
            return sb.ToString();
        }

        static void ShowPipelineResult(PipelineResult result)
        {
            Debug.Log(LOG_PREFIX + "=== Pipeline Result ===");
            Debug.Log(LOG_PREFIX + "Feature: " + result.FeatureName + " (group: " + result.FeatureGroup + ")");
            Debug.Log(LOG_PREFIX + "Intake:         " + (result.IntakeOk ? "PASS" : "FAIL"));
            Debug.Log(LOG_PREFIX + "Decomposition:  " + (result.DecomposeOk ? "PASS" : "FAIL"));
            Debug.Log(LOG_PREFIX + "Queue Gen:      " + (result.QueueGenOk ? "PASS" : "FAIL"));
            Debug.Log(LOG_PREFIX + "Spec Gen:       " + (result.SpecGenOk ? "PASS" : "FAIL"));
            Debug.Log(LOG_PREFIX + "Orchestration:  " + (result.OrchestrationOk ? "PASS" : "FAIL"));
            Debug.Log(LOG_PREFIX + "Validation:     " + (result.ValidationOk ? "PASS" : "BLOCKED"));
            Debug.Log(LOG_PREFIX + "Commit:         " + (result.CommitOk ? "PASS" : "BLOCKED"));

            if (!string.IsNullOrEmpty(result.Error))
                Debug.LogError(LOG_PREFIX + "Error: " + result.Error);

            bool allPass = result.IntakeOk && result.DecomposeOk && result.QueueGenOk &&
                result.SpecGenOk && result.OrchestrationOk && result.ValidationOk && result.CommitOk;

            string status = allPass ? "ALL PASS — Pipeline Complete" : "BLOCKED — Fix errors before commit";
            EditorUtility.DisplayDialog("Pipeline Result — " + status,
                "Feature: " + result.FeatureName +
                "\nGroup: " + result.FeatureGroup +
                "\nModules: " + (result.GeneratedModules != null ? result.GeneratedModules.Length.ToString() : "0") +
                "\nSpecs: " + (result.GeneratedSpecs != null ? result.GeneratedSpecs.Length.ToString() : "0") +
                "\n\nIntake: " + (result.IntakeOk ? "PASS" : "FAIL") +
                "\nDecompose: " + (result.DecomposeOk ? "PASS" : "FAIL") +
                "\nQueue: " + (result.QueueGenOk ? "PASS" : "FAIL") +
                "\nSpecs: " + (result.SpecGenOk ? "PASS" : "FAIL") +
                "\nOrchestrate: " + (result.OrchestrationOk ? "PASS" : "FAIL") +
                "\nValidate: " + (result.ValidationOk ? "PASS" : "BLOCKED") +
                "\nCommit: " + (result.CommitOk ? "PASS" : "BLOCKED"),
                "OK");
        }

        static void WriteFeatureRunLog(PipelineResult result)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string runDir = Path.Combine(projectRoot, FEATURE_RUNS_RELATIVE);
            if (!Directory.Exists(runDir))
                Directory.CreateDirectory(runDir);

            string slug = result.FeatureGroup != null
                ? result.FeatureGroup.ToLower().Replace(' ', '-').Replace('_', '-')
                : "unknown";
            string logPath = Path.Combine(runDir, slug + "_RUN.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Feature Run: " + result.FeatureName);
            sb.AppendLine("");
            sb.AppendLine("- Feature Group: " + result.FeatureGroup);
            sb.AppendLine("- Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("- Commit: " + (result.CommitHash != null ? result.CommitHash : "none"));
            sb.AppendLine("");
            sb.AppendLine("## Pipeline Status");
            sb.AppendLine("");
            sb.AppendLine("| Layer | Status |");
            sb.AppendLine("|-------|--------|");
            sb.AppendLine("| Intake | " + (result.IntakeOk ? "PASS" : "FAIL") + " |");
            sb.AppendLine("| Decomposition | " + (result.DecomposeOk ? "PASS" : "FAIL") + " |");
            sb.AppendLine("| Queue Generation | " + (result.QueueGenOk ? "PASS" : "FAIL") + " |");
            sb.AppendLine("| Spec Generation | " + (result.SpecGenOk ? "PASS" : "FAIL") + " |");
            sb.AppendLine("| Orchestration | " + (result.OrchestrationOk ? "PASS" : "FAIL") + " |");
            sb.AppendLine("| Validation | " + (result.ValidationOk ? "PASS" : "BLOCKED") + " |");
            sb.AppendLine("| Commit | " + (result.CommitOk ? "PASS" : "BLOCKED") + " |");
            sb.AppendLine("");

            if (result.GeneratedModules != null)
            {
                sb.AppendLine("## Generated Modules");
                sb.AppendLine("");
                for (int i = 0; i < result.GeneratedModules.Length; i++)
                    sb.AppendLine("- " + result.GeneratedModules[i]);
                sb.AppendLine("");
            }

            if (result.GeneratedSpecs != null)
            {
                sb.AppendLine("## Generated Specs");
                sb.AppendLine("");
                for (int i = 0; i < result.GeneratedSpecs.Length; i++)
                    sb.AppendLine("- " + result.GeneratedSpecs[i]);
                sb.AppendLine("");
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                sb.AppendLine("## Error");
                sb.AppendLine("");
                sb.AppendLine(result.Error);
            }

            string existingContent = "";
            if (File.Exists(logPath))
            {
                existingContent = File.ReadAllText(logPath);
                sb.Insert(0, "\n---\n\n");
            }

            File.WriteAllText(logPath, existingContent + sb.ToString());
            Debug.Log(LOG_PREFIX + "Feature run log: " + logPath);
        }

        static FeatureGroupTracker.FeatureGroupStatus FindGroupStatus(
            FeatureGroupTracker.FeatureGroupStatus[] groups, string featureGroup)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].FeatureGroup == featureGroup)
                    return groups[i];
            }
            return new FeatureGroupTracker.FeatureGroupStatus();
        }

        static string JoinArray(string[] arr)
        {
            if (arr == null || arr.Length == 0) return "(none)";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(arr[i]);
            }
            return sb.ToString();
        }
    }
}
