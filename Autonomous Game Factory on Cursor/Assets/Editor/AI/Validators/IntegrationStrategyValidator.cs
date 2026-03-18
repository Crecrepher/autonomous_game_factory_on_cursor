using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class IntegrationStrategyValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "IntegrationStrategy";
        const string TASK_QUEUE_RELATIVE = "docs/ai/TASK_QUEUE.yaml";

        static readonly Regex REGEX_NAME = new Regex(@"^\s*-?\s*name:\s*(\w+)");
        static readonly Regex REGEX_STRATEGY = new Regex(@"^\s*integration_strategy:\s*(\w+)");
        static readonly Regex REGEX_IMPACT = new Regex(@"^\s*impact_analysis:\s*(\w+)");
        static readonly Regex REGEX_MIGRATION = new Regex(@"^\s*migration_required:\s*(true|false)");
        static readonly Regex REGEX_COMPAT = new Regex(@"^\s*compatibility_review:\s*(\w+)");
        static readonly Regex REGEX_STATUS = new Regex(@"^\s*status:\s*(\w+)");

        struct TaskIntegrationInfo
        {
            public string Name;
            public string Strategy;
            public string ImpactAnalysis;
            public bool MigrationRequired;
            public string CompatibilityReview;
            public string Status;
        }

        public int Validate(ValidationReport report)
        {
            string queuePath = Path.Combine(Application.dataPath, "..", TASK_QUEUE_RELATIVE);
            if (!File.Exists(queuePath))
                return 0;

            string[] lines = File.ReadAllLines(queuePath);
            int scannedCount = 0;

            TaskIntegrationInfo current = new TaskIntegrationInfo();
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
                    current = new TaskIntegrationInfo { Name = nameMatch.Groups[1].Value };
                    inModule = true;
                    continue;
                }

                if (!inModule) continue;

                Match m;
                m = REGEX_STRATEGY.Match(lines[i]);
                if (m.Success) { current.Strategy = m.Groups[1].Value; continue; }

                m = REGEX_IMPACT.Match(lines[i]);
                if (m.Success) { current.ImpactAnalysis = m.Groups[1].Value; continue; }

                m = REGEX_MIGRATION.Match(lines[i]);
                if (m.Success) { current.MigrationRequired = m.Groups[1].Value == "true"; continue; }

                m = REGEX_COMPAT.Match(lines[i]);
                if (m.Success) { current.CompatibilityReview = m.Groups[1].Value; continue; }

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

        void ValidateTask(TaskIntegrationInfo task, ValidationReport report)
        {
            if (string.IsNullOrEmpty(task.Strategy))
                return;

            if (task.Status == "done" || task.Status == "pending")
                return;

            if (task.Strategy == "replace")
            {
                if (task.ImpactAnalysis != "completed")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Task '" + task.Name + "' has integration_strategy=replace but impact_analysis is '"
                        + (task.ImpactAnalysis ?? "null") + "' (must be 'completed')",
                        TASK_QUEUE_RELATIVE);
                }

                if (!task.MigrationRequired)
                {
                    report.AddError(VALIDATOR_NAME,
                        "Task '" + task.Name + "' has integration_strategy=replace but migration_required=false (must be true)",
                        TASK_QUEUE_RELATIVE);
                }
            }

            if (task.Strategy == "extend")
            {
                if (task.Status == "review" && task.CompatibilityReview != "passed")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Task '" + task.Name + "' has integration_strategy=extend and status=review but compatibility_review is '"
                        + (task.CompatibilityReview ?? "null") + "' (must be 'passed' before review)",
                        TASK_QUEUE_RELATIVE);
                }
            }
        }
    }
}
