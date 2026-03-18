using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Editor.AI
{
    public static class GitCommitStage
    {
        const string LOG_PREFIX = "[GitCommitStage] ";
        const string COMMIT_LOGS_RELATIVE = "docs/ai/commit_logs";
        const string MODULE_PATH_PREFIX = "Assets/Game/Modules/";
        const string SPECS_PATH_PREFIX = "docs/ai/generated_specs/";
        const string PLANS_PATH_PREFIX = "docs/ai/plans/";
        const string REVIEWS_PATH_PREFIX = "docs/ai/reviews/";
        const string REPORT_SUBFOLDER = "Editor/AI";
        const string REPORT_FILENAME = "AIValidationReport.json";

        public struct CommitCandidate
        {
            public string FeatureGroup;
            public string[] ModuleNames;
            public string[] ModulePaths;
            public string[] FilesToStage;
            public string CommitMessage;
            public bool IsValid;
            public string InvalidReason;
            public string CommitType;
            public bool IsRecommit;
            public int HumanFixCount;
            public bool LearningRecorded;
            public string FailedGate;
            public bool ValidationPassed;
            public int ValidationErrorCount;
            public int ValidationWarningCount;
        }

        public struct CommitResult
        {
            public string FeatureGroup;
            public bool Success;
            public string CommitHash;
            public string Error;
            public string Timestamp;
            public string CommitType;
            public bool IsRecommit;
        }

        public struct GateCheckResult
        {
            public bool Passed;
            public string FailedGate;
            public string Detail;
            public bool ReportPassed;
            public int ReportErrorCount;
            public int ReportWarningCount;
        }

        public struct ValidationReportSnapshot
        {
            public bool Exists;
            public bool Passed;
            public int ErrorCount;
            public int WarningCount;
            public string Timestamp;
        }

        public static ValidationReportSnapshot ReadLatestValidationReport()
        {
            ValidationReportSnapshot snapshot = new ValidationReportSnapshot();
            string path = Path.Combine(Application.dataPath, REPORT_SUBFOLDER, REPORT_FILENAME);

            if (!File.Exists(path))
            {
                snapshot.Exists = false;
                return snapshot;
            }

            snapshot.Exists = true;

            try
            {
                string json = File.ReadAllText(path);
                ReportJsonData data = JsonUtility.FromJson<ReportJsonData>(json);
                snapshot.Passed = data.Passed;
                snapshot.ErrorCount = data.ErrorCount;
                snapshot.WarningCount = data.WarningCount;
                snapshot.Timestamp = data.Timestamp;
            }
            catch (Exception ex)
            {
                Debug.LogError(LOG_PREFIX + "Failed to parse AIValidationReport.json: " + ex.Message);
                snapshot.Passed = false;
                snapshot.ErrorCount = -1;
            }

            return snapshot;
        }

        [Serializable]
        struct ReportJsonData
        {
            public string Timestamp;
            public int ScannedFileCount;
            public int ErrorCount;
            public int WarningCount;
            public bool Passed;
        }

        public static CommitCandidate PrepareCommit(
            string featureGroup,
            FeatureGroupTracker.FeatureGroupStatus groupStatus,
            DependencyGraphBuilder.DependencyGraph graph)
        {
            CommitCandidate candidate = new CommitCandidate();
            candidate.FeatureGroup = featureGroup;

            GateCheckResult gateResult = RunSixGateCheck(groupStatus, graph);
            candidate.ValidationPassed = gateResult.ReportPassed;
            candidate.ValidationErrorCount = gateResult.ReportErrorCount;
            candidate.ValidationWarningCount = gateResult.ReportWarningCount;

            if (!gateResult.Passed)
            {
                candidate.IsValid = false;
                candidate.InvalidReason = gateResult.Detail;
                candidate.FailedGate = gateResult.FailedGate;
                return candidate;
            }

            candidate.ModuleNames = groupStatus.ModuleNames;
            candidate.ModulePaths = new string[groupStatus.ModuleNames.Length];
            for (int i = 0; i < groupStatus.ModuleNames.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule reg;
                if (graph.ModuleMap.TryGetValue(groupStatus.ModuleNames[i], out reg))
                    candidate.ModulePaths[i] = reg.Path;
                else
                    candidate.ModulePaths[i] = MODULE_PATH_PREFIX + groupStatus.ModuleNames[i];
            }

            candidate.IsRecommit = DetectRecommit(groupStatus, graph);
            candidate.CommitType = candidate.IsRecommit ? "fix" : "feat";

            candidate.HumanFixCount = CountHumanFixes(groupStatus, graph);
            candidate.LearningRecorded = groupStatus.LearningComplete;

            candidate.FilesToStage = CollectFilesToStage(candidate.ModuleNames, candidate.ModulePaths);

            candidate.CommitMessage = BuildCommitMessageV3(featureGroup, candidate.ModuleNames,
                graph, candidate.CommitType, candidate.HumanFixCount, candidate.LearningRecorded,
                candidate.ValidationPassed, candidate.ValidationErrorCount, candidate.ValidationWarningCount);

            candidate.IsValid = candidate.FilesToStage.Length > 0;
            if (!candidate.IsValid)
                candidate.InvalidReason = "No files found to stage";

            return candidate;
        }

        public static GateCheckResult RunFiveGateCheck(
            FeatureGroupTracker.FeatureGroupStatus groupStatus,
            DependencyGraphBuilder.DependencyGraph graph)
        {
            return RunSixGateCheck(groupStatus, graph);
        }

        public static GateCheckResult RunSixGateCheck(
            FeatureGroupTracker.FeatureGroupStatus groupStatus,
            DependencyGraphBuilder.DependencyGraph graph)
        {
            GateCheckResult result = new GateCheckResult();

            ValidationReportSnapshot report = ReadLatestValidationReport();
            if (!report.Exists)
            {
                result.Passed = false;
                result.FailedGate = "Validation Report Gate";
                result.Detail = "AIValidationReport.json not found — run Tools/AI/Validate Generated Modules first";
                result.ReportErrorCount = 0;
                result.ReportWarningCount = 0;
                result.ReportPassed = false;
                return result;
            }

            result.ReportErrorCount = report.ErrorCount;
            result.ReportWarningCount = report.WarningCount;
            result.ReportPassed = report.Passed;

            if (!report.Passed)
            {
                result.Passed = false;
                result.FailedGate = "Validation Report Gate";
                result.Detail = "AIValidationReport.json shows " + report.ErrorCount
                    + " blocking error(s) — fix all validation errors before commit";
                return result;
            }

            if (!groupStatus.AllCommitReady)
            {
                result.Passed = false;
                result.FailedGate = "Reviewer Gate";
                result.Detail = "Not all modules have commit_state == ready ("
                    + groupStatus.CommitReadyCount + "/" + groupStatus.TotalCount + ")";
                return result;
            }

            if (!groupStatus.AllHumanValidated)
            {
                result.Passed = false;
                result.FailedGate = "Human Gate";
                result.Detail = "Not all modules have human_state == validated";
                return result;
            }

            bool hasHumanFixes = false;
            for (int i = 0; i < groupStatus.ModuleNames.Length; i++)
            {
                DependencyGraphBuilder.TaskEntry task;
                if (graph.TaskMap.TryGetValue(groupStatus.ModuleNames[i], out task))
                {
                    if (task.Status == "blocked" || task.Status == "escalated")
                    {
                        result.Passed = false;
                        result.FailedGate = "Completeness Gate";
                        result.Detail = "Module '" + task.Name + "' is " + task.Status;
                        return result;
                    }
                }
            }

            hasHumanFixes = CountHumanFixes(groupStatus, graph) > 0;
            if (hasHumanFixes && !groupStatus.LearningComplete)
            {
                result.Passed = false;
                result.FailedGate = "Learning Gate";
                result.Detail = "Human fixes exist but learning_state is not recorded";
                return result;
            }

            result.Passed = true;
            return result;
        }

        static bool DetectRecommit(FeatureGroupTracker.FeatureGroupStatus groupStatus,
            DependencyGraphBuilder.DependencyGraph graph)
        {
            for (int i = 0; i < groupStatus.ModuleNames.Length; i++)
            {
                DependencyGraphBuilder.TaskEntry task;
                if (graph.TaskMap.TryGetValue(groupStatus.ModuleNames[i], out task))
                {
                    if (task.RetryCount > 0)
                        return true;
                }
            }
            return false;
        }

        static int CountHumanFixes(FeatureGroupTracker.FeatureGroupStatus groupStatus,
            DependencyGraphBuilder.DependencyGraph graph)
        {
            int count = 0;
            for (int i = 0; i < groupStatus.ModuleNames.Length; i++)
            {
                if (groupStatus.HumanStates[i] == "validated")
                {
                    DependencyGraphBuilder.TaskEntry task;
                    if (graph.TaskMap.TryGetValue(groupStatus.ModuleNames[i], out task))
                    {
                        if (task.RetryCount > 0) count++;
                    }
                }
            }
            return count;
        }

        static string[] CollectFilesToStage(string[] moduleNames, string[] modulePaths)
        {
            var files = new List<string>();
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            for (int i = 0; i < modulePaths.Length; i++)
            {
                string fullPath = Path.Combine(projectRoot, modulePaths[i]);
                if (Directory.Exists(fullPath))
                {
                    string[] moduleFiles = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                    for (int f = 0; f < moduleFiles.Length; f++)
                    {
                        string relativePath = moduleFiles[f].Replace(projectRoot + Path.DirectorySeparatorChar, "")
                            .Replace(projectRoot + "/", "");
                        files.Add(relativePath);
                    }
                }
            }

            for (int i = 0; i < moduleNames.Length; i++)
            {
                string specPath = Path.Combine(projectRoot, SPECS_PATH_PREFIX + moduleNames[i] + "_SPEC.md");
                if (File.Exists(specPath))
                    files.Add(SPECS_PATH_PREFIX + moduleNames[i] + "_SPEC.md");

                string planPath = Path.Combine(projectRoot, PLANS_PATH_PREFIX + moduleNames[i] + "_PLAN.md");
                if (File.Exists(planPath))
                    files.Add(PLANS_PATH_PREFIX + moduleNames[i] + "_PLAN.md");

                string reviewPath = Path.Combine(projectRoot, REVIEWS_PATH_PREFIX + moduleNames[i] + "_REVIEW.md");
                if (File.Exists(reviewPath))
                    files.Add(REVIEWS_PATH_PREFIX + moduleNames[i] + "_REVIEW.md");
            }

            return files.ToArray();
        }

        static string BuildCommitMessageV2(string featureGroup, string[] moduleNames,
            DependencyGraphBuilder.DependencyGraph graph, string commitType, int humanFixCount,
            bool learningRecorded)
        {
            return BuildCommitMessageV3(featureGroup, moduleNames, graph, commitType,
                humanFixCount, learningRecorded, true, 0, 0);
        }

        static string BuildCommitMessageV3(string featureGroup, string[] moduleNames,
            DependencyGraphBuilder.DependencyGraph graph, string commitType, int humanFixCount,
            bool learningRecorded, bool validationPassed, int validationErrors, int validationWarnings)
        {
            string slug = featureGroup.ToLower().Replace(' ', '-').Replace('_', '-');

            StringBuilder sb = new StringBuilder();
            sb.Append(commitType);
            sb.Append("(");
            sb.Append(slug);
            sb.Append("): ");

            if (commitType == "fix")
            {
                sb.Append("apply human validation fixes for ");
                sb.Append(moduleNames.Length == 1 ? moduleNames[0] : slug);
            }
            else
            {
                sb.Append("add ");
                if (moduleNames.Length == 1)
                {
                    sb.Append(moduleNames[0]);
                    sb.Append(" runtime/config/factory/tests");
                }
                else
                {
                    for (int i = 0; i < moduleNames.Length; i++)
                    {
                        if (i > 0 && i == moduleNames.Length - 1)
                            sb.Append(" and ");
                        else if (i > 0)
                            sb.Append(", ");
                        sb.Append(moduleNames[i]);
                    }
                    sb.Append(" modules");
                }
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("- modules:");
            for (int i = 0; i < moduleNames.Length; i++)
                sb.AppendLine("  - " + moduleNames[i]);

            string validationLabel = validationPassed ? "PASS" : "FAIL (errors: " + validationErrors + ")";
            sb.AppendLine("- validation: " + validationLabel);
            if (validationWarnings > 0)
                sb.AppendLine("- validation_warnings: " + validationWarnings);
            sb.AppendLine("- human_validated: true");
            sb.AppendLine("- human_fixes: " + humanFixCount);

            sb.AppendLine("- dependencies:");
            for (int i = 0; i < moduleNames.Length; i++)
            {
                DependencyGraphBuilder.TaskEntry task;
                if (graph.TaskMap.TryGetValue(moduleNames[i], out task))
                {
                    sb.Append("  - " + moduleNames[i] + " → [");
                    if (task.DependsOn != null && task.DependsOn.Length > 0)
                    {
                        for (int d = 0; d < task.DependsOn.Length; d++)
                        {
                            if (d > 0) sb.Append(", ");
                            sb.Append(task.DependsOn[d]);
                        }
                    }
                    sb.AppendLine("]");
                }
            }

            sb.AppendLine("- learning_recorded: " + (learningRecorded ? "true" : "false"));
            sb.AppendLine("- generated by: Autonomous Game Factory v2");

            return sb.ToString();
        }

        public static CommitResult ExecuteCommit(CommitCandidate candidate)
        {
            CommitResult result = new CommitResult();
            result.FeatureGroup = candidate.FeatureGroup;
            result.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            result.CommitType = candidate.CommitType;
            result.IsRecommit = candidate.IsRecommit;

            if (!candidate.IsValid)
            {
                result.Success = false;
                result.Error = candidate.InvalidReason;
                Debug.LogError(LOG_PREFIX + "Cannot commit '" + candidate.FeatureGroup
                    + "' [" + (candidate.FailedGate ?? "unknown") + "]: " + candidate.InvalidReason);
                return result;
            }

            Debug.Log(LOG_PREFIX + "Staging " + candidate.FilesToStage.Length + " files for "
                + candidate.FeatureGroup + " (" + candidate.CommitType + ")");
            for (int i = 0; i < candidate.FilesToStage.Length; i++)
                Debug.Log(LOG_PREFIX + "  Stage: " + candidate.FilesToStage[i]);

            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            StringBuilder addCommand = new StringBuilder();
            addCommand.Append("git add");
            for (int i = 0; i < candidate.FilesToStage.Length; i++)
            {
                addCommand.Append(" \"");
                addCommand.Append(candidate.FilesToStage[i]);
                addCommand.Append("\"");
            }

            string addResult = RunGitCommand(addCommand.ToString(), projectRoot);
            if (addResult.StartsWith("ERROR:"))
            {
                result.Success = false;
                result.Error = "git add failed: " + addResult;
                Debug.LogError(LOG_PREFIX + result.Error);
                return result;
            }

            string escapedMessage = candidate.CommitMessage.Replace("\"", "\\\"");
            string commitResult = RunGitCommand("git commit -m \"" + escapedMessage + "\"", projectRoot);

            if (commitResult.StartsWith("ERROR:"))
            {
                result.Success = false;
                result.Error = "git commit failed: " + commitResult;
                Debug.LogError(LOG_PREFIX + result.Error);
                return result;
            }

            string hashResult = RunGitCommand("git rev-parse --short HEAD", projectRoot);
            result.CommitHash = hashResult.Trim();
            result.Success = true;

            Debug.Log(LOG_PREFIX + "Commit successful: " + result.CommitHash + " for " + candidate.FeatureGroup);

            WriteCommitLog(candidate, result);

            return result;
        }

        public static CommitResult DryRunCommit(CommitCandidate candidate)
        {
            CommitResult result = new CommitResult();
            result.FeatureGroup = candidate.FeatureGroup;
            result.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            result.CommitType = candidate.CommitType;
            result.IsRecommit = candidate.IsRecommit;

            if (!candidate.IsValid)
            {
                result.Success = false;
                result.Error = candidate.InvalidReason;
                return result;
            }

            result.Success = true;
            result.CommitHash = "(dry-run)";

            Debug.Log(LOG_PREFIX + "[DRY RUN] Would " + candidate.CommitType + " commit "
                + candidate.FilesToStage.Length + " files for " + candidate.FeatureGroup);
            Debug.Log(LOG_PREFIX + "[DRY RUN] Message: " + candidate.CommitMessage.Split('\n')[0]);

            for (int i = 0; i < candidate.FilesToStage.Length; i++)
                Debug.Log(LOG_PREFIX + "[DRY RUN]   " + candidate.FilesToStage[i]);

            return result;
        }

        static void WriteCommitLog(CommitCandidate candidate, CommitResult result)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string logDir = Path.Combine(projectRoot, COMMIT_LOGS_RELATIVE);
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string slug = candidate.FeatureGroup.ToLower().Replace(' ', '-').Replace('_', '-');
            string logPath = Path.Combine(logDir, slug + "_COMMIT.md");

            StringBuilder sb = new StringBuilder();

            bool isAppend = File.Exists(logPath);
            if (isAppend)
                sb.AppendLine("\n---\n");
            else
                sb.AppendLine("# Commit Log: " + candidate.FeatureGroup + "\n");

            string header = candidate.IsRecommit
                ? "## " + result.Timestamp + " — RECOMMIT"
                : "## " + result.Timestamp;
            sb.AppendLine(header);
            sb.AppendLine("");
            sb.AppendLine("- Type: " + candidate.CommitType);
            sb.AppendLine("- Commit Hash: " + result.CommitHash);
            sb.AppendLine("- Modules:");
            for (int i = 0; i < candidate.ModuleNames.Length; i++)
                sb.AppendLine("  - " + candidate.ModuleNames[i] + " (" + candidate.ModulePaths[i] + ")");
            string valLabel = candidate.ValidationPassed ? "PASS" : "FAIL (errors: " + candidate.ValidationErrorCount + ")";
            sb.AppendLine("- Validation: " + valLabel);
            if (candidate.ValidationWarningCount > 0)
                sb.AppendLine("- Validation Warnings: " + candidate.ValidationWarningCount);
            sb.AppendLine("- Human Validated: true");
            sb.AppendLine("- Human Fixes: " + candidate.HumanFixCount);
            sb.AppendLine("- Learning Recorded: " + (candidate.LearningRecorded ? "true" : "false"));
            sb.AppendLine("- Files Staged: " + candidate.FilesToStage.Length);
            for (int i = 0; i < candidate.FilesToStage.Length; i++)
                sb.AppendLine("  - " + candidate.FilesToStage[i]);

            if (isAppend)
            {
                File.AppendAllText(logPath, sb.ToString());
            }
            else
            {
                File.WriteAllText(logPath, sb.ToString());
            }
            Debug.Log(LOG_PREFIX + "Commit log written: " + logPath);
        }

        static string RunGitCommand(string command, string workingDir)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "/bin/bash";
                psi.Arguments = "-c \"cd '" + workingDir + "' && " + command + "\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                Process process = Process.Start(psi);
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    return "ERROR: " + stderr;

                return stdout;
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }
    }
}
