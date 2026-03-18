using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ArchitectureDiffValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ArchitectureDiff";
        const string TASK_QUEUE_RELATIVE = "docs/ai/TASK_QUEUE.yaml";
        const string DIFF_REPORTS_DIR = "docs/ai/diff_reports";

        static readonly Regex REGEX_NAME = new Regex(@"^\s*-?\s*name:\s*(\w+)");
        static readonly Regex REGEX_DIFF_RISK = new Regex(@"^\s*arch_diff_risk:\s*(\w+)");
        static readonly Regex REGEX_DIFF_BLOCKED = new Regex(@"^\s*arch_diff_blocked:\s*(true|false)");
        static readonly Regex REGEX_STATUS = new Regex(@"^\s*status:\s*(\w+)");

        struct TaskDiffInfo
        {
            public string Name;
            public string DiffRisk;
            public bool DiffBlocked;
            public string Status;
        }

        public int Validate(ValidationReport report)
        {
            string queuePath = Path.Combine(Application.dataPath, "..", TASK_QUEUE_RELATIVE);
            if (!File.Exists(queuePath))
                return 0;

            string[] lines = File.ReadAllLines(queuePath);
            int scannedCount = 0;

            TaskDiffInfo current = new TaskDiffInfo();
            bool inModule = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match nameMatch = REGEX_NAME.Match(lines[i]);
                if (nameMatch.Success)
                {
                    if (inModule)
                    {
                        scannedCount++;
                        ValidateTask(current, report);
                    }
                    current = new TaskDiffInfo { Name = nameMatch.Groups[1].Value };
                    inModule = true;
                    continue;
                }

                if (!inModule) continue;

                Match m;
                m = REGEX_DIFF_RISK.Match(lines[i]);
                if (m.Success) { current.DiffRisk = m.Groups[1].Value; continue; }

                m = REGEX_DIFF_BLOCKED.Match(lines[i]);
                if (m.Success) { current.DiffBlocked = m.Groups[1].Value == "true"; continue; }

                m = REGEX_STATUS.Match(lines[i]);
                if (m.Success) { current.Status = m.Groups[1].Value; continue; }
            }

            if (inModule)
            {
                scannedCount++;
                ValidateTask(current, report);
            }

            return scannedCount;
        }

        void ValidateTask(TaskDiffInfo task, ValidationReport report)
        {
            if (string.IsNullOrEmpty(task.DiffRisk))
                return;

            if (task.Status == "done" || task.Status == "pending")
                return;

            if (task.DiffBlocked)
            {
                report.AddError(VALIDATOR_NAME,
                    "Task '" + task.Name + "' is blocked by Architecture Diff Analyzer "
                    + "(arch_diff_risk=" + task.DiffRisk + "). "
                    + "Critical architectural risk must be resolved before proceeding.",
                    TASK_QUEUE_RELATIVE);
            }

            if (task.DiffRisk == "critical" && !task.DiffBlocked)
            {
                report.AddError(VALIDATOR_NAME,
                    "Task '" + task.Name + "' has arch_diff_risk=critical but arch_diff_blocked=false. "
                    + "Critical risk must block the pipeline.",
                    TASK_QUEUE_RELATIVE);
            }

            if (task.DiffRisk == "high" || task.DiffRisk == "critical")
            {
                string reportPath = Path.Combine(
                    Application.dataPath, "..", DIFF_REPORTS_DIR, task.Name + "_DIFF.md");
                if (!File.Exists(reportPath))
                {
                    report.AddWarning(VALIDATOR_NAME,
                        "Task '" + task.Name + "' has arch_diff_risk=" + task.DiffRisk
                        + " but no diff report found at " + DIFF_REPORTS_DIR + "/" + task.Name + "_DIFF.md",
                        TASK_QUEUE_RELATIVE);
                }
            }
        }
    }
}
