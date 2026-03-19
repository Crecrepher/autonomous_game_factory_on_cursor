using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class ArchitectureDiffAnalyzer
    {
        const string MODULES_ROOT = "Assets/Game/Modules";
        const string DIFF_REPORTS_DIR = "docs/ai/diff_reports";
        const string RISK_LOW = "low";
        const string RISK_MEDIUM = "medium";
        const string RISK_HIGH = "high";
        const string RISK_CRITICAL = "critical";
        const string RISK_NOT_ANALYZED = "not_analyzed";

        static readonly Regex REGEX_MONO_INHERIT =
            new Regex(@"class\s+\w+Runtime\s*:\s*.*MonoBehaviour");
        static readonly Regex REGEX_FOREACH =
            new Regex(@"\bforeach\s*\(");
        static readonly Regex REGEX_LINQ =
            new Regex(@"\busing\s+System\.Linq\b");
        static readonly Regex REGEX_COROUTINE =
            new Regex(@"\bIEnumerator\b|\bStartCoroutine\b");
        static readonly Regex REGEX_LAMBDA =
            new Regex(@"=>\s*\{|=>\s*[^;{]+;");

        public struct ArchDiff
        {
            public string Type;
            public string Target;
            public string Change;
            public string Risk;
            public string Reason;
            public string Mitigation;
        }

        public struct DependencyGraphDiff
        {
            public string[] AddedEdges;
            public string[] RemovedEdges;
            public bool CycleDetected;
            public int MaxDepthChange;
        }

        public struct DiffSummary
        {
            public int TotalDiffs;
            public int CriticalCount;
            public int HighCount;
            public int MediumCount;
            public int LowCount;
            public string[] BlockingReasons;
        }

        public struct DiffReport
        {
            public string ModuleName;
            public string Strategy;
            public string OverallRisk;
            public bool Blocked;
            public ArchDiff[] Diffs;
            public DependencyGraphDiff GraphDiff;
            public DiffSummary Summary;
        }

        public static DiffReport Analyze(
            string moduleName,
            string strategy,
            string[] proposedDependencies,
            string[] proposedInterfaceMethods,
            string proposedDescription)
        {
            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();

            List<ArchDiff> diffs = new List<ArchDiff>();
            List<string> addedEdges = new List<string>();
            List<string> removedEdges = new List<string>();
            List<string> blockingReasons = new List<string>();
            bool cycleDetected = false;

            bool moduleExists = false;
            for (int i = 0; i < graph.Modules.Length; i++)
            {
                if (graph.Modules[i].Name == moduleName)
                {
                    moduleExists = true;
                    break;
                }
            }

            if (!moduleExists && strategy == "create_new")
            {
                diffs.Add(new ArchDiff
                {
                    Type = "new_module",
                    Target = moduleName,
                    Change = "New module creation: " + moduleName,
                    Risk = RISK_LOW,
                    Reason = "Standard new module, no existing code affected",
                    Mitigation = null
                });

                CheckResponsibilityOverlap(moduleName, proposedDescription, graph, diffs);
            }

            if (strategy == "replace")
            {
                AnalyzeReplacement(moduleName, graph, diffs, blockingReasons);
            }

            if (strategy == "extend" && moduleExists)
            {
                AnalyzeExtension(moduleName, proposedInterfaceMethods, diffs, blockingReasons);
            }

            AnalyzeDependencyChanges(
                moduleName, proposedDependencies, graph,
                diffs, addedEdges, removedEdges, blockingReasons, ref cycleDetected);

            if (moduleExists)
            {
                AnalyzeCodeSafety(moduleName, diffs, blockingReasons);
            }

            int criticalCount = 0;
            int highCount = 0;
            int mediumCount = 0;
            int lowCount = 0;

            for (int i = 0; i < diffs.Count; i++)
            {
                if (diffs[i].Risk == RISK_CRITICAL) criticalCount++;
                else if (diffs[i].Risk == RISK_HIGH) highCount++;
                else if (diffs[i].Risk == RISK_MEDIUM) mediumCount++;
                else lowCount++;
            }

            string overallRisk = RISK_LOW;
            if (criticalCount > 0) overallRisk = RISK_CRITICAL;
            else if (highCount > 0) overallRisk = RISK_HIGH;
            else if (mediumCount > 0) overallRisk = RISK_MEDIUM;

            bool blocked = criticalCount > 0;

            DiffReport report = new DiffReport
            {
                ModuleName = moduleName,
                Strategy = strategy,
                OverallRisk = overallRisk,
                Blocked = blocked,
                Diffs = diffs.ToArray(),
                GraphDiff = new DependencyGraphDiff
                {
                    AddedEdges = addedEdges.ToArray(),
                    RemovedEdges = removedEdges.ToArray(),
                    CycleDetected = cycleDetected,
                    MaxDepthChange = addedEdges.Count
                },
                Summary = new DiffSummary
                {
                    TotalDiffs = diffs.Count,
                    CriticalCount = criticalCount,
                    HighCount = highCount,
                    MediumCount = mediumCount,
                    LowCount = lowCount,
                    BlockingReasons = blockingReasons.ToArray()
                }
            };

            return report;
        }

        static void CheckResponsibilityOverlap(
            string moduleName,
            string proposedDescription,
            DependencyGraphBuilder.DependencyGraph graph,
            List<ArchDiff> diffs)
        {
            if (string.IsNullOrEmpty(proposedDescription)) return;

            string descLower = proposedDescription.ToLower();

            for (int i = 0; i < graph.Modules.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule existing = graph.Modules[i];
                if (existing.Name == "Template" || existing.Name == moduleName)
                    continue;

                string registryPath = Path.Combine(
                    Application.dataPath, "..", "docs", "ai", "MODULE_REGISTRY.yaml");
                if (!File.Exists(registryPath)) continue;

                string registryContent = File.ReadAllText(registryPath);
                int nameIdx = registryContent.IndexOf("name: " + existing.Name);
                if (nameIdx < 0) continue;

                int descIdx = registryContent.IndexOf("description:", nameIdx);
                if (descIdx < 0) continue;

                int endIdx = registryContent.IndexOf("\n  - name:", descIdx);
                if (endIdx < 0) endIdx = registryContent.Length;

                string existingDesc = registryContent.Substring(descIdx, endIdx - descIdx).ToLower();

                string[] keywords = descLower.Split(' ', ',', '.', '/', '(', ')');
                int matchCount = 0;
                int validCount = 0;
                for (int k = 0; k < keywords.Length; k++)
                {
                    if (keywords[k].Length < 3) continue;
                    validCount++;
                    if (existingDesc.Contains(keywords[k]))
                        matchCount++;
                }

                if (validCount > 0 && (float)matchCount / validCount > 0.5f)
                {
                    diffs.Add(new ArchDiff
                    {
                        Type = "responsibility_drift",
                        Target = existing.Name,
                        Change = "Proposed " + moduleName + " overlaps with " + existing.Name + " responsibilities",
                        Risk = RISK_MEDIUM,
                        Reason = "High keyword overlap with existing module description",
                        Mitigation = "Consider reusing " + existing.Name + " or clearly separating responsibilities"
                    });
                }
            }
        }

        static void AnalyzeReplacement(
            string moduleName,
            DependencyGraphBuilder.DependencyGraph graph,
            List<ArchDiff> diffs,
            List<string> blockingReasons)
        {
            int dependentCount = 0;
            for (int i = 0; i < graph.Modules.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule m = graph.Modules[i];
                if (m.Dependencies == null) continue;
                for (int d = 0; d < m.Dependencies.Length; d++)
                {
                    if (m.Dependencies[d] == moduleName)
                    {
                        dependentCount++;
                        break;
                    }
                }
            }

            string risk = dependentCount > 5 ? RISK_HIGH : (dependentCount > 0 ? RISK_MEDIUM : RISK_LOW);

            diffs.Add(new ArchDiff
            {
                Type = "module_replacement",
                Target = moduleName,
                Change = "Replace existing module " + moduleName + " (" + dependentCount + " dependents)",
                Risk = risk,
                Reason = dependentCount + " modules depend on " + moduleName,
                Mitigation = "Use adapter pattern for gradual migration"
            });
        }

        static void AnalyzeExtension(
            string moduleName,
            string[] proposedMethods,
            List<ArchDiff> diffs,
            List<string> blockingReasons)
        {
            if (proposedMethods == null || proposedMethods.Length == 0) return;

            ModuleDiscovery.InterfaceSignature existing =
                ModuleDiscovery.ScanInterface(moduleName);

            if (existing.Methods == null) return;

            for (int p = 0; p < proposedMethods.Length; p++)
            {
                bool existsInCurrent = false;
                for (int e = 0; e < existing.Methods.Length; e++)
                {
                    if (existing.Methods[e] == proposedMethods[p])
                    {
                        existsInCurrent = true;
                        break;
                    }
                }

                if (!existsInCurrent)
                {
                    diffs.Add(new ArchDiff
                    {
                        Type = "interface_change",
                        Target = "I" + moduleName,
                        Change = "Add method: " + proposedMethods[p],
                        Risk = RISK_MEDIUM,
                        Reason = "New method added to existing interface",
                        Mitigation = "Ensure backward compatibility — no existing method signatures changed"
                    });
                }
            }

            for (int e = 0; e < existing.Methods.Length; e++)
            {
                bool stillExists = false;
                for (int p = 0; p < proposedMethods.Length; p++)
                {
                    if (proposedMethods[p] == existing.Methods[e])
                    {
                        stillExists = true;
                        break;
                    }
                }

                if (!stillExists)
                {
                    string reason = "Existing method " + existing.Methods[e]
                        + " removed from I" + moduleName + " — backward compatibility broken";
                    diffs.Add(new ArchDiff
                    {
                        Type = "interface_change",
                        Target = "I" + moduleName,
                        Change = "Remove method: " + existing.Methods[e],
                        Risk = RISK_CRITICAL,
                        Reason = reason,
                        Mitigation = "Keep existing method, add new method instead"
                    });
                    blockingReasons.Add(reason);
                }
            }
        }

        static void AnalyzeDependencyChanges(
            string moduleName,
            string[] proposedDeps,
            DependencyGraphBuilder.DependencyGraph graph,
            List<ArchDiff> diffs,
            List<string> addedEdges,
            List<string> removedEdges,
            List<string> blockingReasons,
            ref bool cycleDetected)
        {
            if (proposedDeps == null) return;

            string[] existingDeps = null;
            for (int i = 0; i < graph.Modules.Length; i++)
            {
                if (graph.Modules[i].Name == moduleName)
                {
                    existingDeps = graph.Modules[i].Dependencies;
                    break;
                }
            }

            for (int p = 0; p < proposedDeps.Length; p++)
            {
                string dep = proposedDeps[p];
                if (dep == "UnityEngine" || dep == "System") continue;

                bool isNew = true;
                if (existingDeps != null)
                {
                    for (int e = 0; e < existingDeps.Length; e++)
                    {
                        if (existingDeps[e] == dep)
                        {
                            isNew = false;
                            break;
                        }
                    }
                }

                if (isNew)
                {
                    string edge = moduleName + " → " + dep;
                    addedEdges.Add(edge);
                    diffs.Add(new ArchDiff
                    {
                        Type = "dependency_addition",
                        Target = edge,
                        Change = "Add dependency: " + edge,
                        Risk = RISK_LOW,
                        Reason = "New dependency edge",
                        Mitigation = null
                    });

                    bool reverseExists = false;
                    for (int m = 0; m < graph.Modules.Length; m++)
                    {
                        if (graph.Modules[m].Name != dep) continue;
                        if (graph.Modules[m].Dependencies == null) continue;
                        for (int d = 0; d < graph.Modules[m].Dependencies.Length; d++)
                        {
                            if (graph.Modules[m].Dependencies[d] == moduleName)
                            {
                                reverseExists = true;
                                break;
                            }
                        }
                        break;
                    }

                    if (reverseExists)
                    {
                        cycleDetected = true;
                        string reason = "Circular dependency detected: " + moduleName
                            + " → " + dep + " → " + moduleName;
                        diffs.Add(new ArchDiff
                        {
                            Type = "architecture_violation",
                            Target = edge,
                            Change = "Circular dependency introduced",
                            Risk = RISK_CRITICAL,
                            Reason = reason,
                            Mitigation = "Use Shared interface to break the cycle"
                        });
                        blockingReasons.Add(reason);
                    }
                }
            }

            if (existingDeps != null)
            {
                for (int e = 0; e < existingDeps.Length; e++)
                {
                    string dep = existingDeps[e];
                    if (dep == "UnityEngine" || dep == "System") continue;

                    bool stillExists = false;
                    for (int p = 0; p < proposedDeps.Length; p++)
                    {
                        if (proposedDeps[p] == dep)
                        {
                            stillExists = true;
                            break;
                        }
                    }

                    if (!stillExists)
                    {
                        string edge = moduleName + " → " + dep;
                        removedEdges.Add(edge);
                        diffs.Add(new ArchDiff
                        {
                            Type = "dependency_removal",
                            Target = edge,
                            Change = "Remove dependency: " + edge,
                            Risk = RISK_MEDIUM,
                            Reason = "Existing dependency removed — verify no runtime references remain",
                            Mitigation = "Ensure all using/references to " + dep + " are removed"
                        });
                    }
                }
            }
        }

        static void AnalyzeCodeSafety(
            string moduleName,
            List<ArchDiff> diffs,
            List<string> blockingReasons)
        {
            string runtimePath = Path.Combine(
                Application.dataPath, "..",
                MODULES_ROOT, moduleName, moduleName + "Runtime.cs");

            if (!File.Exists(runtimePath)) return;

            string content = File.ReadAllText(runtimePath);

            if (REGEX_MONO_INHERIT.IsMatch(content))
            {
                string reason = moduleName + "Runtime inherits MonoBehaviour — architecture layer violation";
                diffs.Add(new ArchDiff
                {
                    Type = "architecture_violation",
                    Target = moduleName + "Runtime.cs",
                    Change = "Runtime inherits MonoBehaviour",
                    Risk = RISK_CRITICAL,
                    Reason = reason,
                    Mitigation = "Runtime must be pure C# — move MonoBehaviour to Bootstrap only"
                });
                blockingReasons.Add(reason);
            }

            if (REGEX_FOREACH.IsMatch(content))
            {
                string reason = moduleName + "Runtime uses foreach — GC allocation per frame risk";
                diffs.Add(new ArchDiff
                {
                    Type = "architecture_violation",
                    Target = moduleName + "Runtime.cs",
                    Change = "foreach usage detected",
                    Risk = RISK_CRITICAL,
                    Reason = reason,
                    Mitigation = "Replace foreach with for loop"
                });
                blockingReasons.Add(reason);
            }

            if (REGEX_LINQ.IsMatch(content))
            {
                string reason = moduleName + "Runtime uses LINQ — GC allocation risk";
                diffs.Add(new ArchDiff
                {
                    Type = "architecture_violation",
                    Target = moduleName + "Runtime.cs",
                    Change = "LINQ usage detected",
                    Risk = RISK_CRITICAL,
                    Reason = reason,
                    Mitigation = "Remove LINQ, use manual loops"
                });
                blockingReasons.Add(reason);
            }

            if (REGEX_COROUTINE.IsMatch(content))
            {
                string reason = moduleName + "Runtime uses coroutines — GC allocation risk";
                diffs.Add(new ArchDiff
                {
                    Type = "architecture_violation",
                    Target = moduleName + "Runtime.cs",
                    Change = "Coroutine usage detected in Runtime",
                    Risk = RISK_CRITICAL,
                    Reason = reason,
                    Mitigation = "Use update loop with delta time accumulation"
                });
                blockingReasons.Add(reason);
            }
        }

        public static string FormatReport(DiffReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Architecture Diff Report — " + report.ModuleName);
            sb.AppendLine();
            sb.AppendLine("- Strategy: " + report.Strategy);
            sb.AppendLine("- Overall Risk: **" + report.OverallRisk.ToUpper() + "**");
            sb.AppendLine("- Blocked: " + report.Blocked);
            sb.AppendLine("- Total Diffs: " + report.Summary.TotalDiffs);
            sb.AppendLine();

            sb.AppendLine("## Risk Summary");
            sb.AppendLine();
            sb.AppendLine("| Level | Count |");
            sb.AppendLine("|-------|-------|");
            sb.AppendLine("| critical | " + report.Summary.CriticalCount + " |");
            sb.AppendLine("| high | " + report.Summary.HighCount + " |");
            sb.AppendLine("| medium | " + report.Summary.MediumCount + " |");
            sb.AppendLine("| low | " + report.Summary.LowCount + " |");
            sb.AppendLine();

            if (report.Summary.BlockingReasons.Length > 0)
            {
                sb.AppendLine("## Blocking Reasons");
                sb.AppendLine();
                for (int i = 0; i < report.Summary.BlockingReasons.Length; i++)
                {
                    sb.AppendLine("- **[BLOCK]** " + report.Summary.BlockingReasons[i]);
                }
                sb.AppendLine();
            }

            sb.AppendLine("## Diffs");
            sb.AppendLine();
            for (int i = 0; i < report.Diffs.Length; i++)
            {
                ArchDiff d = report.Diffs[i];
                sb.AppendLine("### " + (i + 1) + ". " + d.Type + " [" + d.Risk + "]");
                sb.AppendLine();
                sb.AppendLine("- Target: " + d.Target);
                sb.AppendLine("- Change: " + d.Change);
                sb.AppendLine("- Reason: " + d.Reason);
                if (d.Mitigation != null)
                    sb.AppendLine("- Mitigation: " + d.Mitigation);
                sb.AppendLine();
            }

            sb.AppendLine("## Dependency Graph Diff");
            sb.AppendLine();
            if (report.GraphDiff.AddedEdges.Length > 0)
            {
                sb.AppendLine("Added edges:");
                for (int i = 0; i < report.GraphDiff.AddedEdges.Length; i++)
                    sb.AppendLine("  + " + report.GraphDiff.AddedEdges[i]);
            }
            if (report.GraphDiff.RemovedEdges.Length > 0)
            {
                sb.AppendLine("Removed edges:");
                for (int i = 0; i < report.GraphDiff.RemovedEdges.Length; i++)
                    sb.AppendLine("  - " + report.GraphDiff.RemovedEdges[i]);
            }
            sb.AppendLine("Cycle detected: " + report.GraphDiff.CycleDetected);

            return sb.ToString();
        }

        public static void WriteReport(DiffReport report)
        {
            string dirPath = Path.Combine(Application.dataPath, "..", DIFF_REPORTS_DIR);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string filePath = Path.Combine(dirPath, report.ModuleName + "_DIFF.md");
            File.WriteAllText(filePath, FormatReport(report));
            Debug.Log("[ArchDiffAnalyzer] Report written: " + filePath);
        }

        [UnityEditor.MenuItem("Tools/AI/Run Architecture Diff (Test — InventorySystem)")]
        static void RunTestInventory()
        {
            string[] deps = { "UnityEngine", "System", "ItemStacking" };
            string[] methods = { "Init", "Add", "Remove", "GetCount", "Has" };
            DiffReport report = Analyze(
                "InventorySystem",
                "create_new",
                deps,
                methods,
                "슬롯 기반 인벤토리. 아이템 추가/제거/조회.");

            Debug.Log(FormatReport(report));
        }

        [UnityEditor.MenuItem("Tools/AI/Run Architecture Diff (Test — Economy Replace)")]
        static void RunTestEconomyReplace()
        {
            string[] deps = { "UnityEngine", "System" };
            string[] methods = { "AddCurrency", "SpendCurrency", "GetBalance" };
            DiffReport report = Analyze(
                "Economy",
                "replace",
                deps,
                methods,
                "다중 재화 경제 시스템");

            Debug.Log(FormatReport(report));
            WriteReport(report);
        }
    }
}
