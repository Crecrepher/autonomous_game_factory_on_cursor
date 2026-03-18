using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class PipelineTruthValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "PipelineTruth";
        const string TASK_QUEUE_RELATIVE = "docs/ai/TASK_QUEUE.yaml";
        const string FEATURE_QUEUE_RELATIVE = "docs/ai/FEATURE_QUEUE.yaml";
        const string VALIDATION_REPORT_SUBFOLDER = "Editor/AI";
        const string VALIDATION_REPORT_FILE = "AIValidationReport.json";

        static readonly Regex REGEX_NAME = new Regex(@"^\s*-?\s*name:\s*(\w[\w\-]*)");
        static readonly Regex REGEX_STATUS = new Regex(@"^\s*status:\s*(\w+)");
        static readonly Regex REGEX_HUMAN_STATE = new Regex(@"^\s*human_state:\s*(\w+)");
        static readonly Regex REGEX_COMMIT_STATE = new Regex(@"^\s*commit_state:\s*(\w+)");
        static readonly Regex REGEX_LEARNING_STATE = new Regex(@"^\s*learning_state:\s*(\w+)");
        static readonly Regex REGEX_FEATURE_GROUP = new Regex(@"^\s*feature_group:\s*([\w\-]+)");
        static readonly Regex REGEX_ARCH_BLOCKED = new Regex(@"^\s*arch_diff_blocked:\s*(true|false)");
        static readonly Regex REGEX_MODULES_ARRAY = new Regex(@"^\s*modules:\s*\[(.+)\]");

        struct TaskTruthInfo
        {
            public string Name;
            public string Status;
            public string HumanState;
            public string CommitState;
            public string LearningState;
            public string FeatureGroup;
            public bool ArchDiffBlocked;
        }

        struct FeatureTruthInfo
        {
            public string Name;
            public string Status;
            public string FeatureGroup;
            public string[] Modules;
        }

        public int Validate(ValidationReport report)
        {
            int scanned = 0;

            scanned += CheckValidationReportTruth(report);
            scanned += CheckTaskQueueTruth(report);
            scanned += CheckFeatureQueueConsistency(report);

            return scanned;
        }

        int CheckValidationReportTruth(ValidationReport report)
        {
            string reportPath = Path.Combine(Application.dataPath, VALIDATION_REPORT_SUBFOLDER, VALIDATION_REPORT_FILE);
            if (!File.Exists(reportPath)) return 0;

            string json = File.ReadAllText(reportPath);

            Regex errorCountRx = new Regex(@"""ErrorCount""\s*:\s*(\d+)");
            Regex passedRx = new Regex(@"""Passed""\s*:\s*(true|false)");

            Match ecMatch = errorCountRx.Match(json);
            Match passMatch = passedRx.Match(json);

            if (!ecMatch.Success || !passMatch.Success) return 1;

            int prevErrorCount;
            if (!int.TryParse(ecMatch.Groups[1].Value, out prevErrorCount)) return 1;

            bool prevPassed = passMatch.Groups[1].Value == "true";

            if (prevErrorCount > 0 && prevPassed)
            {
                report.AddError(VALIDATOR_NAME,
                    "Truth violation: Previous AIValidationReport.json shows ErrorCount="
                    + prevErrorCount + " but Passed=true. "
                    + "Report must not claim PASS when blocking errors exist. "
                    + "Truth source: AIValidationReport.json. Expected: Passed=false.",
                    VALIDATION_REPORT_FILE);
            }

            string queuePath = Path.Combine(Application.dataPath, "..", TASK_QUEUE_RELATIVE);
            if (!File.Exists(queuePath)) return 1;

            string[] lines = File.ReadAllLines(queuePath);
            TaskTruthInfo current = new TaskTruthInfo();
            bool inEntry = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match m = REGEX_NAME.Match(lines[i]);
                if (m.Success)
                {
                    if (inEntry) ValidateReportVsCommitState(current, prevErrorCount, report);
                    current = new TaskTruthInfo { Name = m.Groups[1].Value };
                    inEntry = true;
                    continue;
                }
                if (!inEntry) continue;

                m = REGEX_COMMIT_STATE.Match(lines[i]);
                if (m.Success) { current.CommitState = m.Groups[1].Value; continue; }

                m = REGEX_STATUS.Match(lines[i]);
                if (m.Success) { current.Status = m.Groups[1].Value; continue; }
            }
            if (inEntry) ValidateReportVsCommitState(current, prevErrorCount, report);

            return 1;
        }

        void ValidateReportVsCommitState(TaskTruthInfo task, int prevErrorCount, ValidationReport report)
        {
            if (task.Status == "done") return;
            if (string.IsNullOrEmpty(task.CommitState)) return;

            if (prevErrorCount > 0 && (task.CommitState == "ready" || task.CommitState == "committed"))
            {
                report.AddError(VALIDATOR_NAME,
                    "Truth violation: Task '" + task.Name + "' has commit_state="
                    + task.CommitState + " but last validation report has " + prevErrorCount
                    + " error(s). Truth source: AIValidationReport.json + TASK_QUEUE. "
                    + "Expected: commit_state should not be ready/committed when validation has errors.",
                    TASK_QUEUE_RELATIVE);
            }
        }

        int CheckTaskQueueTruth(ValidationReport report)
        {
            string queuePath = Path.Combine(Application.dataPath, "..", TASK_QUEUE_RELATIVE);
            if (!File.Exists(queuePath)) return 0;

            string[] lines = File.ReadAllLines(queuePath);
            int scanned = 0;
            TaskTruthInfo current = new TaskTruthInfo();
            bool inEntry = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match m = REGEX_NAME.Match(lines[i]);
                if (m.Success)
                {
                    if (inEntry) { scanned++; ValidateTaskTruth(current, report); }
                    current = new TaskTruthInfo { Name = m.Groups[1].Value };
                    inEntry = true;
                    continue;
                }
                if (!inEntry) continue;

                m = REGEX_STATUS.Match(lines[i]);
                if (m.Success) { current.Status = m.Groups[1].Value; continue; }

                m = REGEX_HUMAN_STATE.Match(lines[i]);
                if (m.Success) { current.HumanState = m.Groups[1].Value; continue; }

                m = REGEX_COMMIT_STATE.Match(lines[i]);
                if (m.Success) { current.CommitState = m.Groups[1].Value; continue; }

                m = REGEX_LEARNING_STATE.Match(lines[i]);
                if (m.Success) { current.LearningState = m.Groups[1].Value; continue; }

                m = REGEX_ARCH_BLOCKED.Match(lines[i]);
                if (m.Success) { current.ArchDiffBlocked = m.Groups[1].Value == "true"; continue; }
            }
            if (inEntry) { scanned++; ValidateTaskTruth(current, report); }

            return scanned;
        }

        void ValidateTaskTruth(TaskTruthInfo task, ValidationReport report)
        {
            if (task.Status == "review" || task.Status == "done")
            {
                if (!string.IsNullOrEmpty(task.HumanState)
                    && task.HumanState != "validated"
                    && task.HumanState != "none")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Truth violation: Task '" + task.Name + "' has status="
                        + task.Status + " but human_state=" + task.HumanState
                        + ". Truth source: TASK_QUEUE human_state. "
                        + "Expected: human_state must be 'validated' before status can be review/done.",
                        TASK_QUEUE_RELATIVE);
                }
            }

            if (task.Status == "done")
            {
                if (!string.IsNullOrEmpty(task.CommitState)
                    && task.CommitState != "committed"
                    && task.CommitState != "recommitted"
                    && task.CommitState != "none")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Truth violation: Task '" + task.Name + "' has status=done but commit_state="
                        + task.CommitState + ". Truth source: TASK_QUEUE commit_state. "
                        + "Expected: commit_state must be committed/recommitted for done tasks.",
                        TASK_QUEUE_RELATIVE);
                }
            }

            if (task.ArchDiffBlocked)
            {
                if (task.Status == "planned" || task.Status == "in_progress"
                    || task.Status == "review" || task.Status == "done")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Truth violation: Task '" + task.Name + "' has arch_diff_blocked=true but status="
                        + task.Status + ". Truth source: TASK_QUEUE arch_diff_blocked. "
                        + "Expected: pipeline must be blocked when arch_diff_blocked=true.",
                        TASK_QUEUE_RELATIVE);
                }
            }

            if (!string.IsNullOrEmpty(task.CommitState) && task.CommitState == "ready")
            {
                if (task.Status != "review" && task.Status != "done")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Truth violation: Task '" + task.Name + "' has commit_state=ready but status="
                        + task.Status + ". Truth source: TASK_QUEUE. "
                        + "Expected: commit_state=ready only valid when status is review or done.",
                        TASK_QUEUE_RELATIVE);
                }
            }
        }

        int CheckFeatureQueueConsistency(ValidationReport report)
        {
            string featurePath = Path.Combine(Application.dataPath, "..", FEATURE_QUEUE_RELATIVE);
            string taskPath = Path.Combine(Application.dataPath, "..", TASK_QUEUE_RELATIVE);
            if (!File.Exists(featurePath) || !File.Exists(taskPath)) return 0;

            string[] featureLines = File.ReadAllLines(featurePath);
            string[] taskLines = File.ReadAllLines(taskPath);

            int scanned = 0;
            FeatureTruthInfo currentFeature = new FeatureTruthInfo();
            bool inFeature = false;

            for (int i = 0; i < featureLines.Length; i++)
            {
                Match m = REGEX_NAME.Match(featureLines[i]);
                if (m.Success)
                {
                    if (inFeature) { scanned++; ValidateFeatureVsTasks(currentFeature, taskLines, report); }
                    currentFeature = new FeatureTruthInfo { Name = m.Groups[1].Value };
                    inFeature = true;
                    continue;
                }
                if (!inFeature) continue;

                m = REGEX_STATUS.Match(featureLines[i]);
                if (m.Success) { currentFeature.Status = m.Groups[1].Value; continue; }

                m = REGEX_FEATURE_GROUP.Match(featureLines[i]);
                if (m.Success) { currentFeature.FeatureGroup = m.Groups[1].Value; continue; }

                m = REGEX_MODULES_ARRAY.Match(featureLines[i]);
                if (m.Success)
                {
                    string rawModules = m.Groups[1].Value;
                    string[] parts = rawModules.Split(',');
                    currentFeature.Modules = new string[parts.Length];
                    for (int p = 0; p < parts.Length; p++)
                        currentFeature.Modules[p] = parts[p].Trim();
                }
            }
            if (inFeature) { scanned++; ValidateFeatureVsTasks(currentFeature, taskLines, report); }

            return scanned;
        }

        void ValidateFeatureVsTasks(FeatureTruthInfo feature, string[] taskLines, ValidationReport report)
        {
            if (feature.Status != "done") return;
            if (feature.Modules == null || feature.Modules.Length == 0) return;

            for (int m = 0; m < feature.Modules.Length; m++)
            {
                string moduleName = feature.Modules[m];
                string taskStatus = FindTaskStatus(moduleName, taskLines);

                if (taskStatus != null && taskStatus != "done")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Truth violation: Feature '" + feature.Name + "' has status=done "
                        + "but its module '" + moduleName + "' has task status="
                        + taskStatus + ". Truth source: FEATURE_QUEUE vs TASK_QUEUE. "
                        + "Expected: all feature modules must be done before feature is done.",
                        FEATURE_QUEUE_RELATIVE);
                }
            }
        }

        string FindTaskStatus(string moduleName, string[] taskLines)
        {
            bool found = false;
            for (int i = 0; i < taskLines.Length; i++)
            {
                if (found)
                {
                    Match m = REGEX_STATUS.Match(taskLines[i]);
                    if (m.Success) return m.Groups[1].Value;

                    Match nameM = REGEX_NAME.Match(taskLines[i]);
                    if (nameM.Success) return null;
                }

                Match nm = REGEX_NAME.Match(taskLines[i]);
                if (nm.Success && nm.Groups[1].Value == moduleName)
                    found = true;
            }
            return null;
        }
    }
}
