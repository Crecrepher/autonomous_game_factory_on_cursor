using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class RegressionGuardian
    {
        const string MODULES_ROOT = "Assets/Game/Modules";
        const string LOG_PREFIX = "[RegressionGuard] ";
        const int MAX_DEPENDENCY_DEPTH = 8;

        static readonly Regex REGEX_INTERFACE_METHOD =
            new Regex(@"^\s*([\w<>\[\],\s]+)\s+(\w+)\s*\(([^)]*)\)\s*;");

        public struct RegressionIssue
        {
            public string Type;
            public string Module;
            public string Detail;
            public string Severity;
            public string[] AffectedModules;
        }

        public struct RegressionReport
        {
            public int TotalIssues;
            public int CriticalCount;
            public int HighCount;
            public bool ShouldBlock;
            public RegressionIssue[] Issues;
        }

        public static RegressionReport RunFullScan()
        {
            List<RegressionIssue> issues = new List<RegressionIssue>();
            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();

            CheckDependencyBreakage(graph, issues);
            CheckInterfaceBreakingChanges(graph, issues);
            CheckModuleFunctionalityRemoval(graph, issues);

            int criticalCount = 0;
            int highCount = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == "critical") criticalCount++;
                else if (issues[i].Severity == "high") highCount++;
            }

            RegressionReport report = new RegressionReport
            {
                TotalIssues = issues.Count,
                CriticalCount = criticalCount,
                HighCount = highCount,
                ShouldBlock = criticalCount > 0,
                Issues = issues.ToArray()
            };

            return report;
        }

        static void CheckDependencyBreakage(
            DependencyGraphBuilder.DependencyGraph graph,
            List<RegressionIssue> issues)
        {
            for (int i = 0; i < graph.ModuleMap.Count; i++)
            {
                DependencyGraphBuilder.RegistryModule module = graph.ModuleMap[i];
                if (module.Dependencies == null) continue;
                if (module.Name == "Template") continue;

                for (int d = 0; d < module.Dependencies.Length; d++)
                {
                    string dep = module.Dependencies[d];
                    if (dep == "UnityEngine" || dep == "System") continue;

                    bool depExists = false;
                    for (int j = 0; j < graph.ModuleMap.Count; j++)
                    {
                        if (graph.ModuleMap[j].Name == dep)
                        {
                            depExists = true;
                            break;
                        }
                    }

                    if (!depExists)
                    {
                        issues.Add(new RegressionIssue
                        {
                            Type = "dependency_breakage",
                            Module = module.Name,
                            Detail = "Depends on '" + dep + "' which does not exist in MODULE_REGISTRY",
                            Severity = "critical",
                            AffectedModules = new[] { module.Name }
                        });
                    }

                    string depInterfacePath = Path.Combine(
                        Application.dataPath, "..", MODULES_ROOT, dep, "I" + dep + ".cs");
                    if (depExists && !File.Exists(depInterfacePath))
                    {
                        string depModulePath = Path.Combine(
                            Application.dataPath, "..", MODULES_ROOT, dep);
                        if (Directory.Exists(depModulePath))
                        {
                            issues.Add(new RegressionIssue
                            {
                                Type = "interface_missing",
                                Module = dep,
                                Detail = "Module '" + dep + "' exists but I" + dep + ".cs is missing. "
                                    + module.Name + " depends on it.",
                                Severity = "high",
                                AffectedModules = new[] { module.Name, dep }
                            });
                        }
                    }
                }
            }
        }

        static void CheckInterfaceBreakingChanges(
            DependencyGraphBuilder.DependencyGraph graph,
            List<RegressionIssue> issues)
        {
            for (int i = 0; i < graph.ModuleMap.Count; i++)
            {
                DependencyGraphBuilder.RegistryModule module = graph.ModuleMap[i];
                if (module.Name == "Template") continue;

                string interfacePath = Path.Combine(
                    Application.dataPath, "..", MODULES_ROOT, module.Name, "I" + module.Name + ".cs");
                if (!File.Exists(interfacePath)) continue;

                string content = File.ReadAllText(interfacePath);
                MatchCollection methods = REGEX_INTERFACE_METHOD.Matches(content);

                if (methods.Count == 0)
                {
                    List<string> dependents = new List<string>();
                    for (int j = 0; j < graph.ModuleMap.Count; j++)
                    {
                        if (graph.ModuleMap[j].Dependencies == null) continue;
                        for (int d = 0; d < graph.ModuleMap[j].Dependencies.Length; d++)
                        {
                            if (graph.ModuleMap[j].Dependencies[d] == module.Name)
                            {
                                dependents.Add(graph.ModuleMap[j].Name);
                                break;
                            }
                        }
                    }

                    if (dependents.Count > 0)
                    {
                        issues.Add(new RegressionIssue
                        {
                            Type = "empty_interface",
                            Module = module.Name,
                            Detail = "I" + module.Name + " has no methods but " + dependents.Count + " modules depend on it",
                            Severity = "high",
                            AffectedModules = dependents.ToArray()
                        });
                    }
                }
            }
        }

        static void CheckModuleFunctionalityRemoval(
            DependencyGraphBuilder.DependencyGraph graph,
            List<RegressionIssue> issues)
        {
            for (int i = 0; i < graph.ModuleMap.Count; i++)
            {
                DependencyGraphBuilder.RegistryModule module = graph.ModuleMap[i];
                if (module.Name == "Template") continue;

                string modulePath = Path.Combine(Application.dataPath, "..", module.Path);
                if (!Directory.Exists(modulePath)) continue;

                string runtimePath = Path.Combine(modulePath, module.Name + "Runtime.cs");
                string factoryPath = Path.Combine(modulePath, module.Name + "Factory.cs");
                string bootstrapPath = Path.Combine(modulePath, module.Name + "Bootstrap.cs");

                if (!File.Exists(runtimePath))
                {
                    issues.Add(new RegressionIssue
                    {
                        Type = "missing_runtime",
                        Module = module.Name,
                        Detail = module.Name + "Runtime.cs is missing from registered module",
                        Severity = "high",
                        AffectedModules = new[] { module.Name }
                    });
                }

                if (!File.Exists(factoryPath))
                {
                    issues.Add(new RegressionIssue
                    {
                        Type = "missing_factory",
                        Module = module.Name,
                        Detail = module.Name + "Factory.cs is missing from registered module",
                        Severity = "medium",
                        AffectedModules = new[] { module.Name }
                    });
                }
            }
        }

        public static string FormatReport(RegressionReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Regression Guardian Report");
            sb.AppendLine();
            sb.AppendLine("- Total Issues: " + report.TotalIssues);
            sb.AppendLine("- Critical: " + report.CriticalCount);
            sb.AppendLine("- High: " + report.HighCount);
            sb.AppendLine("- Should Block Commit: " + report.ShouldBlock);
            sb.AppendLine();

            for (int i = 0; i < report.Issues.Length; i++)
            {
                RegressionIssue issue = report.Issues[i];
                sb.AppendLine("### " + (i + 1) + ". [" + issue.Severity.ToUpper() + "] " + issue.Type);
                sb.AppendLine("- Module: " + issue.Module);
                sb.AppendLine("- Detail: " + issue.Detail);
                if (issue.AffectedModules != null && issue.AffectedModules.Length > 0)
                {
                    sb.Append("- Affected: ");
                    for (int a = 0; a < issue.AffectedModules.Length; a++)
                    {
                        if (a > 0) sb.Append(", ");
                        sb.Append(issue.AffectedModules[a]);
                    }
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        [UnityEditor.MenuItem("Tools/AI/Run Regression Guardian")]
        static void RunFromMenu()
        {
            RegressionReport report = RunFullScan();
            Debug.Log(FormatReport(report));

            if (report.ShouldBlock)
                Debug.LogError(LOG_PREFIX + "REGRESSION DETECTED — Commit should be BLOCKED. Critical: " + report.CriticalCount);
            else if (report.TotalIssues > 0)
                Debug.LogWarning(LOG_PREFIX + "Issues found but no critical regression. Review recommended.");
            else
                Debug.Log(LOG_PREFIX + "No regressions detected. All clear.");
        }
    }
}
