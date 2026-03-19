using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ModuleReuseIntegrityValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ModuleReuseIntegrity";
        const string TASK_QUEUE_RELATIVE = "docs/ai/TASK_QUEUE.yaml";
        const string MODULES_RELATIVE = "Game/Modules";
        const float RESPONSIBILITY_OVERLAP_THRESHOLD = 0.5f;
        const int MIN_INTERFACE_METHODS = 1;

        static readonly Regex REGEX_NAME = new Regex(@"^\s*-?\s*name:\s*(\w[\w\-]*)");
        static readonly Regex REGEX_STATUS = new Regex(@"^\s*status:\s*(\w+)");
        static readonly Regex REGEX_STRATEGY = new Regex(@"^\s*integration_strategy:\s*(\w+)");
        static readonly Regex REGEX_IMPACT = new Regex(@"^\s*impact_analysis:\s*(\w+)");
        static readonly Regex REGEX_MIGRATION = new Regex(@"^\s*migration_required:\s*(true|false)");
        static readonly Regex REGEX_MIGRATION_PLAN = new Regex(@"^\s*migration_plan:\s*(\w+)");
        static readonly Regex REGEX_CANDIDATES = new Regex(@"^\s*existing_module_candidates:\s*\[([^\]]*)\]");
        static readonly Regex REGEX_FEATURE_GROUP = new Regex(@"^\s*feature_group:\s*([\w\-]+)");

        static readonly Regex REGEX_INTERFACE_METHOD =
            new Regex(@"^\s*(?:void|int|float|bool|string|[\w<>\[\]]+)\s+(\w+)\s*\(", RegexOptions.Multiline);

        struct TaskReuseInfo
        {
            public string Name;
            public string Status;
            public string Strategy;
            public string ImpactAnalysis;
            public bool MigrationRequired;
            public string MigrationPlan;
            public string[] ExistingCandidates;
            public string FeatureGroup;
        }

        public int Validate(ValidationReport report)
        {
            string queuePath = Path.Combine(Application.dataPath, "..", TASK_QUEUE_RELATIVE);
            if (!File.Exists(queuePath)) return 0;

            string[] lines = File.ReadAllLines(queuePath);
            int scanned = 0;

            TaskReuseInfo current = new TaskReuseInfo();
            bool inEntry = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match m = REGEX_NAME.Match(lines[i]);
                if (m.Success)
                {
                    if (inEntry) { scanned++; ValidateReuseIntegrity(current, report); }
                    current = new TaskReuseInfo { Name = m.Groups[1].Value };
                    inEntry = true;
                    continue;
                }
                if (!inEntry) continue;

                m = REGEX_STATUS.Match(lines[i]);
                if (m.Success) { current.Status = m.Groups[1].Value; continue; }

                m = REGEX_STRATEGY.Match(lines[i]);
                if (m.Success) { current.Strategy = m.Groups[1].Value; continue; }

                m = REGEX_IMPACT.Match(lines[i]);
                if (m.Success) { current.ImpactAnalysis = m.Groups[1].Value; continue; }

                m = REGEX_MIGRATION.Match(lines[i]);
                if (m.Success) { current.MigrationRequired = m.Groups[1].Value == "true"; continue; }

                m = REGEX_MIGRATION_PLAN.Match(lines[i]);
                if (m.Success) { current.MigrationPlan = m.Groups[1].Value; continue; }

                m = REGEX_CANDIDATES.Match(lines[i]);
                if (m.Success)
                {
                    string raw = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        string[] parts = raw.Split(',');
                        current.ExistingCandidates = new string[parts.Length];
                        for (int p = 0; p < parts.Length; p++)
                            current.ExistingCandidates[p] = parts[p].Trim();
                    }
                    continue;
                }

                m = REGEX_FEATURE_GROUP.Match(lines[i]);
                if (m.Success) { current.FeatureGroup = m.Groups[1].Value; continue; }
            }
            if (inEntry) { scanned++; ValidateReuseIntegrity(current, report); }

            scanned += CheckResponsibilityDuplication(report);

            return scanned;
        }

        void ValidateReuseIntegrity(TaskReuseInfo task, ValidationReport report)
        {
            if (task.Status == "done" && string.IsNullOrEmpty(task.Strategy)) return;
            if (task.Status == "pending") return;

            if (task.Strategy == "replace")
            {
                if (task.ImpactAnalysis != "completed")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Reuse integrity violation: Task '" + task.Name
                        + "' has integration_strategy=replace but impact_analysis='"
                        + (task.ImpactAnalysis ?? "missing")
                        + "'. Replace strategy requires completed impact analysis. "
                        + "Safer action: complete impact_analysis before proceeding with replace.",
                        TASK_QUEUE_RELATIVE);
                }

                if (!task.MigrationRequired)
                {
                    report.AddError(VALIDATOR_NAME,
                        "Reuse integrity violation: Task '" + task.Name
                        + "' has integration_strategy=replace but migration_required=false. "
                        + "Replace strategy always requires migration. "
                        + "Safer action: set migration_required=true and create migration plan.",
                        TASK_QUEUE_RELATIVE);
                }

                if (string.IsNullOrEmpty(task.MigrationPlan) || task.MigrationPlan == "none" || task.MigrationPlan == "null")
                {
                    report.AddError(VALIDATOR_NAME,
                        "Reuse integrity violation: Task '" + task.Name
                        + "' has integration_strategy=replace but migration_plan is missing/none. "
                        + "Replace without migration plan risks breaking dependent modules. "
                        + "Safer action: create docs/ai/migration_plans/" + task.Name + "_MIGRATION.md",
                        TASK_QUEUE_RELATIVE);
                }
            }

            if (task.Strategy == "create_new"
                && task.ExistingCandidates != null
                && task.ExistingCandidates.Length > 0)
            {
                bool allEmpty = true;
                for (int i = 0; i < task.ExistingCandidates.Length; i++)
                {
                    if (!string.IsNullOrEmpty(task.ExistingCandidates[i]))
                    {
                        allEmpty = false;
                        break;
                    }
                }

                if (!allEmpty)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < task.ExistingCandidates.Length; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(task.ExistingCandidates[i]);
                    }

                    report.AddWarning(VALIDATOR_NAME,
                        "Reuse concern: Task '" + task.Name
                        + "' chose integration_strategy=create_new but existing candidates were found: ["
                        + sb.ToString() + "]. "
                        + "Verify that none of these candidates can be reused or extended. "
                        + "Safer action: consider 'extend' or 'adapt' if overlap exists.",
                        TASK_QUEUE_RELATIVE);
                }
            }

            if ((task.Strategy == "extend" || task.Strategy == "adapt")
                && task.ExistingCandidates != null
                && task.ExistingCandidates.Length > 0)
            {
                for (int i = 0; i < task.ExistingCandidates.Length; i++)
                {
                    string candidate = task.ExistingCandidates[i];
                    if (string.IsNullOrEmpty(candidate)) continue;

                    string candidatePath = Path.Combine(Application.dataPath, MODULES_RELATIVE, candidate);
                    if (!Directory.Exists(candidatePath))
                    {
                        report.AddError(VALIDATOR_NAME,
                            "Reuse integrity violation: Task '" + task.Name
                            + "' claims " + task.Strategy + " on candidate '"
                            + candidate + "' but module directory does not exist at "
                            + "Assets/" + MODULES_RELATIVE + "/" + candidate + ". "
                            + "Safer action: verify candidate module exists before claiming reuse.",
                            TASK_QUEUE_RELATIVE);
                    }
                }
            }

            if (!string.IsNullOrEmpty(task.Strategy) && task.Strategy != "create_new")
            {
                if (task.ExistingCandidates == null || task.ExistingCandidates.Length == 0)
                {
                    report.AddWarning(VALIDATOR_NAME,
                        "Reuse metadata gap: Task '" + task.Name
                        + "' has integration_strategy=" + task.Strategy
                        + " but existing_module_candidates is empty. "
                        + "Non-create strategies should reference target modules.",
                        TASK_QUEUE_RELATIVE);
                }
            }
        }

        int CheckResponsibilityDuplication(ValidationReport report)
        {
            string modulesPath = Path.Combine(Application.dataPath, MODULES_RELATIVE);
            if (!Directory.Exists(modulesPath)) return 0;

            string[] moduleDirs = Directory.GetDirectories(modulesPath);

            List<string> moduleNames = new List<string>();
            List<string[]> moduleMethods = new List<string[]>();

            for (int i = 0; i < moduleDirs.Length; i++)
            {
                string dirName = Path.GetFileName(moduleDirs[i]);
                if (dirName == "Template") continue;

                string interfaceFile = Path.Combine(moduleDirs[i], "I" + dirName + ".cs");
                if (!File.Exists(interfaceFile)) continue;

                string content = File.ReadAllText(interfaceFile);
                MatchCollection matches = REGEX_INTERFACE_METHOD.Matches(content);

                if (matches.Count < MIN_INTERFACE_METHODS) continue;

                string[] methods = new string[matches.Count];
                for (int m = 0; m < matches.Count; m++)
                    methods[m] = matches[m].Groups[1].Value.ToLower();

                moduleNames.Add(dirName);
                moduleMethods.Add(methods);
            }

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            int scanned = moduleNames.Count;

            for (int a = 0; a < moduleNames.Count; a++)
            {
                for (int b = a + 1; b < moduleNames.Count; b++)
                {
                    if (AreRelated(moduleNames[a], moduleNames[b], graph)) continue;

                    float overlap = ComputeMethodOverlap(moduleMethods[a], moduleMethods[b]);
                    if (overlap >= RESPONSIBILITY_OVERLAP_THRESHOLD)
                    {
                        report.AddWarning(VALIDATOR_NAME,
                            "Responsibility duplication: " + moduleNames[a] + " and " + moduleNames[b]
                            + " have " + (overlap * 100f).ToString("F0") + "% interface method overlap. "
                            + "This may indicate duplicated responsibility. "
                            + "If one should reuse the other, update integration_strategy accordingly.",
                            "Assets/" + MODULES_RELATIVE + "/" + moduleNames[a] + "/I" + moduleNames[a] + ".cs");
                    }
                }
            }

            return scanned;
        }

        static bool AreRelated(string a, string b, DependencyGraphBuilder.DependencyGraph graph)
        {
            for (int i = 0; i < graph.Modules.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule mod = graph.Modules[i];
                if (mod.Name != a) continue;
                if (mod.Dependencies == null) return false;
                for (int d = 0; d < mod.Dependencies.Length; d++)
                {
                    if (mod.Dependencies[d] == b) return true;
                }
            }
            for (int i = 0; i < graph.Modules.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule mod = graph.Modules[i];
                if (mod.Name != b) continue;
                if (mod.Dependencies == null) return false;
                for (int d = 0; d < mod.Dependencies.Length; d++)
                {
                    if (mod.Dependencies[d] == a) return true;
                }
            }
            return false;
        }

        static float ComputeMethodOverlap(string[] methodsA, string[] methodsB)
        {
            int commonCount = 0;
            for (int a = 0; a < methodsA.Length; a++)
            {
                for (int b = 0; b < methodsB.Length; b++)
                {
                    if (methodsA[a] == methodsB[b])
                    {
                        commonCount++;
                        break;
                    }
                }
            }

            int maxLen = methodsA.Length > methodsB.Length ? methodsA.Length : methodsB.Length;
            if (maxLen == 0) return 0f;
            return (float)commonCount / maxLen;
        }
    }
}
