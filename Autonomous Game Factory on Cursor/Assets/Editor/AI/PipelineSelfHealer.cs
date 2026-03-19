using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class PipelineSelfHealer
    {
        const string REGISTRY_PATH = "docs/ai/MODULE_REGISTRY.yaml";
        const string TASK_QUEUE_PATH = "docs/ai/TASK_QUEUE.yaml";
        const string MODULES_ROOT = "Assets/Game/Modules";
        const string LOG_PREFIX = "[SelfHealer] ";

        static readonly Regex REGEX_NAME = new Regex(@"^\s*-?\s*name:\s*(\w+)");
        static readonly Regex REGEX_STATUS = new Regex(@"^\s*status:\s*(\w+)");

        public struct HealAction
        {
            public string Type;
            public string Target;
            public string Description;
            public bool Applied;
        }

        public struct HealReport
        {
            public int DetectedIssues;
            public int FixedIssues;
            public int SkippedIssues;
            public HealAction[] Actions;
        }

        public static HealReport RunDiagnostics(bool dryRun)
        {
            List<HealAction> actions = new List<HealAction>();

            CheckMissingRegistryEntries(actions, dryRun);
            CheckQueueStatusMismatch(actions, dryRun);
            CheckDependencyOrdering(actions, dryRun);

            int fixedCount = 0;
            int skippedCount = 0;
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].Applied) fixedCount++;
                else skippedCount++;
            }

            return new HealReport
            {
                DetectedIssues = actions.Count,
                FixedIssues = fixedCount,
                SkippedIssues = skippedCount,
                Actions = actions.ToArray()
            };
        }

        static void CheckMissingRegistryEntries(List<HealAction> actions, bool dryRun)
        {
            string registryFull = Path.Combine(Application.dataPath, "..", REGISTRY_PATH);
            if (!File.Exists(registryFull)) return;

            string registryContent = File.ReadAllText(registryFull);
            string modulesDir = Path.Combine(Application.dataPath, "..", MODULES_ROOT);
            if (!Directory.Exists(modulesDir)) return;

            string[] moduleDirs = Directory.GetDirectories(modulesDir);
            List<string> missingModules = new List<string>();

            for (int i = 0; i < moduleDirs.Length; i++)
            {
                string dirName = Path.GetFileName(moduleDirs[i]);
                if (dirName == "Template") continue;

                string runtimeFile = Path.Combine(moduleDirs[i], dirName + "Runtime.cs");
                if (!File.Exists(runtimeFile)) continue;

                if (!registryContent.Contains("name: " + dirName))
                {
                    missingModules.Add(dirName);

                    HealAction action = new HealAction
                    {
                        Type = "missing_registry_entry",
                        Target = dirName,
                        Description = "Module '" + dirName + "' has code but no MODULE_REGISTRY entry"
                    };

                    if (!dryRun)
                    {
                        string entry = "\n  - name: " + dirName
                            + "\n    path: " + MODULES_ROOT + "/" + dirName
                            + "\n    editable: true"
                            + "\n    risk: medium"
                            + "\n    description: \"Auto-registered by SelfHealer\""
                            + "\n    dependencies: []";

                        File.AppendAllText(registryFull, entry);
                        action.Applied = true;
                        Debug.Log(LOG_PREFIX + "Added missing registry entry: " + dirName);
                    }

                    actions.Add(action);
                }
            }
        }

        static void CheckQueueStatusMismatch(List<HealAction> actions, bool dryRun)
        {
            string queueFull = Path.Combine(Application.dataPath, "..", TASK_QUEUE_PATH);
            if (!File.Exists(queueFull)) return;

            string[] lines = File.ReadAllLines(queueFull);
            string currentName = null;
            string currentStatus = null;
            bool needsRewrite = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match nameMatch = REGEX_NAME.Match(lines[i]);
                if (nameMatch.Success)
                {
                    if (currentName != null)
                    {
                        needsRewrite = CheckAndFixStatus(
                            currentName, currentStatus, lines, actions, dryRun) || needsRewrite;
                    }
                    currentName = nameMatch.Groups[1].Value;
                    currentStatus = null;
                    continue;
                }

                Match statusMatch = REGEX_STATUS.Match(lines[i]);
                if (statusMatch.Success && currentName != null)
                {
                    currentStatus = statusMatch.Groups[1].Value;
                }
            }

            if (currentName != null)
            {
                needsRewrite = CheckAndFixStatus(
                    currentName, currentStatus, lines, actions, dryRun) || needsRewrite;
            }

            if (needsRewrite && !dryRun)
            {
                File.WriteAllLines(queueFull, lines);
            }
        }

        static bool CheckAndFixStatus(
            string name, string status, string[] lines,
            List<HealAction> actions, bool dryRun)
        {
            if (string.IsNullOrEmpty(status))
            {
                actions.Add(new HealAction
                {
                    Type = "missing_status",
                    Target = name,
                    Description = "Task '" + name + "' has no status field",
                    Applied = false
                });
                return false;
            }

            string[] validStatuses = { "pending", "planned", "in_progress", "review", "done", "blocked", "escalated" };
            bool isValid = false;
            for (int i = 0; i < validStatuses.Length; i++)
            {
                if (validStatuses[i] == status)
                {
                    isValid = true;
                    break;
                }
            }

            if (!isValid)
            {
                HealAction action = new HealAction
                {
                    Type = "invalid_status",
                    Target = name,
                    Description = "Task '" + name + "' has invalid status '" + status + "'. Resetting to pending."
                };

                if (!dryRun)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("name: " + name))
                        {
                            for (int j = i + 1; j < lines.Length && j < i + 20; j++)
                            {
                                if (REGEX_STATUS.IsMatch(lines[j]))
                                {
                                    lines[j] = lines[j].Substring(0, lines[j].IndexOf("status:")) + "status: pending";
                                    action.Applied = true;
                                    Debug.Log(LOG_PREFIX + "Fixed invalid status for " + name + ": " + status + " → pending");
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }

                actions.Add(action);
                return action.Applied;
            }

            return false;
        }

        static void CheckDependencyOrdering(List<HealAction> actions, bool dryRun)
        {
            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();

            string cycleChain;
            if (DependencyGraphBuilder.DetectCycle(graph.Modules, out cycleChain))
            {
                actions.Add(new HealAction
                {
                    Type = "circular_dependency",
                    Target = cycleChain,
                    Description = "Circular dependency detected: " + cycleChain + ". Cannot auto-fix.",
                    Applied = false
                });
                return;
            }

            string[] sorted = DependencyGraphBuilder.TopologicalSort(graph.Modules);
            if (sorted == null || sorted.Length == 0) return;

            int orderIssues = 0;
            for (int i = 0; i < graph.Tasks.Length; i++)
            {
                DependencyGraphBuilder.TaskEntry task = graph.Tasks[i];
                if (task.Status == "done") continue;
                if (task.DependsOn == null || task.DependsOn.Length == 0) continue;

                for (int d = 0; d < task.DependsOn.Length; d++)
                {
                    string dep = task.DependsOn[d];
                    for (int t = 0; t < graph.Tasks.Length; t++)
                    {
                        if (graph.Tasks[t].Name == dep && graph.Tasks[t].Status == "done")
                        {
                            break;
                        }
                    }

                    bool depInRegistry = false;
                    for (int r = 0; r < graph.Modules.Length; r++)
                    {
                        if (graph.Modules[r].Name == dep)
                        {
                            depInRegistry = true;
                            break;
                        }
                    }

                    if (!depInRegistry)
                    {
                        orderIssues++;
                        actions.Add(new HealAction
                        {
                            Type = "dependency_not_registered",
                            Target = task.Name + " → " + dep,
                            Description = "Task '" + task.Name + "' depends on '" + dep
                                + "' which is not in MODULE_REGISTRY. Consider adding it.",
                            Applied = false
                        });
                    }
                }
            }
        }

        public static string FormatReport(HealReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Pipeline Self-Healing Report");
            sb.AppendLine();
            sb.AppendLine("- Detected: " + report.DetectedIssues);
            sb.AppendLine("- Fixed: " + report.FixedIssues);
            sb.AppendLine("- Skipped: " + report.SkippedIssues);
            sb.AppendLine();

            for (int i = 0; i < report.Actions.Length; i++)
            {
                HealAction a = report.Actions[i];
                string status = a.Applied ? "FIXED" : "SKIPPED";
                sb.AppendLine((i + 1) + ". [" + status + "] " + a.Type + " — " + a.Target);
                sb.AppendLine("   " + a.Description);
            }

            return sb.ToString();
        }

        [UnityEditor.MenuItem("Tools/AI/Run Self-Healing (Dry Run)")]
        static void RunDryFromMenu()
        {
            HealReport report = RunDiagnostics(true);
            Debug.Log(FormatReport(report));
            Debug.Log(LOG_PREFIX + "Dry run complete. No changes applied.");
        }

        [UnityEditor.MenuItem("Tools/AI/Run Self-Healing (Apply Fixes)")]
        static void RunApplyFromMenu()
        {
            HealReport report = RunDiagnostics(false);
            Debug.Log(FormatReport(report));
            Debug.Log(LOG_PREFIX + "Applied " + report.FixedIssues + " fixes. Skipped " + report.SkippedIssues + ".");
        }
    }
}
