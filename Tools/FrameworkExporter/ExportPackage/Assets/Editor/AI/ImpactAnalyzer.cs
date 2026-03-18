using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class ImpactAnalyzer
    {
        public struct AffectedModule
        {
            public string ModuleName;
            public string DependencyType;
            public string RequiredChange;
            public string Risk;
        }

        public struct FeatureGroupEffect
        {
            public string Group;
            public string Effect;
        }

        public struct ImpactReport
        {
            public string TargetModule;
            public string Strategy;
            public AffectedModule[] AffectedModules;
            public bool RegistryChangesRequired;
            public bool QueueChangesRequired;
            public string ValidatorRisk;
            public FeatureGroupEffect[] FeatureGroupImpact;
            public int TotalAffected;
            public int HighRiskCount;
            public string[] BlockingIssues;
        }

        public static ImpactReport Analyze(string targetModuleName, string strategy)
        {
            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();

            List<AffectedModule> affected = new List<AffectedModule>();
            List<FeatureGroupEffect> groupEffects = new List<FeatureGroupEffect>();
            HashSet<string> processedGroups = new HashSet<string>();
            int highRiskCount = 0;
            List<string> blockingIssues = new List<string>();

            for (int i = 0; i < graph.ModuleMap.Count; i++)
            {
                DependencyGraphBuilder.RegistryModule regModule = graph.ModuleMap[i];
                if (regModule.Dependencies == null) continue;

                bool dependsOnTarget = false;
                for (int d = 0; d < regModule.Dependencies.Length; d++)
                {
                    if (regModule.Dependencies[d] == targetModuleName)
                    {
                        dependsOnTarget = true;
                        break;
                    }
                }

                if (!dependsOnTarget) continue;

                string risk = strategy == "replace" ? "medium" : "low";
                string requiredChange = strategy == "replace" ? "update_api_call" : "no_change";

                AffectedModule am = new AffectedModule
                {
                    ModuleName = regModule.Name,
                    DependencyType = "direct",
                    RequiredChange = requiredChange,
                    Risk = risk
                };
                affected.Add(am);

                if (risk == "high")
                    highRiskCount++;
            }

            for (int t = 0; t < graph.TaskMap.Count; t++)
            {
                DependencyGraphBuilder.TaskEntry task = graph.TaskMap[t];
                if (string.IsNullOrEmpty(task.FeatureGroup)) continue;
                if (processedGroups.Contains(task.FeatureGroup)) continue;

                bool groupAffected = false;
                for (int a = 0; a < affected.Count; a++)
                {
                    if (affected[a].ModuleName == task.Name)
                    {
                        groupAffected = true;
                        break;
                    }
                }

                if (groupAffected)
                {
                    FeatureGroupEffect effect = new FeatureGroupEffect
                    {
                        Group = task.FeatureGroup,
                        Effect = "update_required"
                    };
                    groupEffects.Add(effect);
                }
                else
                {
                    FeatureGroupEffect effect = new FeatureGroupEffect
                    {
                        Group = task.FeatureGroup,
                        Effect = "none"
                    };
                    groupEffects.Add(effect);
                }
                processedGroups.Add(task.FeatureGroup);
            }

            bool registryChanges = strategy == "replace" && affected.Count > 0;
            bool queueChanges = strategy == "replace" && affected.Count > 0;
            string validatorRisk = affected.Count > 5 ? "high" : (affected.Count > 2 ? "medium" : "low");

            if (strategy == "replace" && affected.Count > 8)
                blockingIssues.Add("Too many affected modules (" + affected.Count + "), consider phased migration");

            ImpactReport report = new ImpactReport
            {
                TargetModule = targetModuleName,
                Strategy = strategy,
                AffectedModules = affected.ToArray(),
                RegistryChangesRequired = registryChanges,
                QueueChangesRequired = queueChanges,
                ValidatorRisk = validatorRisk,
                FeatureGroupImpact = groupEffects.ToArray(),
                TotalAffected = affected.Count,
                HighRiskCount = highRiskCount,
                BlockingIssues = blockingIssues.ToArray()
            };

            return report;
        }

        public static string FormatReport(ImpactReport report)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== IMPACT ANALYSIS REPORT ===");
            sb.AppendLine("Target Module: " + report.TargetModule);
            sb.AppendLine("Strategy: " + report.Strategy);
            sb.AppendLine("Total Affected: " + report.TotalAffected);
            sb.AppendLine("High Risk Count: " + report.HighRiskCount);
            sb.AppendLine("Registry Changes: " + report.RegistryChangesRequired);
            sb.AppendLine("Queue Changes: " + report.QueueChangesRequired);
            sb.AppendLine("Validator Risk: " + report.ValidatorRisk);
            sb.AppendLine();

            sb.AppendLine("--- Affected Modules ---");
            for (int i = 0; i < report.AffectedModules.Length; i++)
            {
                AffectedModule am = report.AffectedModules[i];
                sb.AppendLine("  " + am.ModuleName + " | " + am.DependencyType
                    + " | " + am.RequiredChange + " | risk=" + am.Risk);
            }

            sb.AppendLine();
            sb.AppendLine("--- Feature Group Impact ---");
            for (int i = 0; i < report.FeatureGroupImpact.Length; i++)
            {
                FeatureGroupEffect fg = report.FeatureGroupImpact[i];
                sb.AppendLine("  " + fg.Group + " | " + fg.Effect);
            }

            if (report.BlockingIssues.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- BLOCKING ISSUES ---");
                for (int i = 0; i < report.BlockingIssues.Length; i++)
                {
                    sb.AppendLine("  [BLOCK] " + report.BlockingIssues[i]);
                }
            }

            return sb.ToString();
        }

        [UnityEditor.MenuItem("Tools/AI/Run Impact Analysis (Test — Economy)")]
        static void RunImpactTest()
        {
            ImpactReport report = Analyze("Economy", "replace");
            Debug.Log(FormatReport(report));
        }
    }
}
