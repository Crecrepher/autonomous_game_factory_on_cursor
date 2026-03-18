using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class TaskQueueGenerator
    {
        const string LOG_PREFIX = "[TaskQueueGenerator] ";
        const string MODULE_PATH_PREFIX = "Assets/Game/Modules/";

        public struct GeneratedTaskEntry
        {
            public string Name;
            public string Status;
            public string Priority;
            public string Owner;
            public string Role;
            public string[] DependsOn;
            public string ModulePath;
            public string FeatureGoal;
            public string[] Requirements;
            public string BasedOn;
            public string[] References;
            public string[] Constraints;
            public string[] Deliverables;
            public string Notes;
            public string FeatureGroup;
        }

        public struct GenerationResult
        {
            public string FeatureName;
            public string FeatureGroup;
            public GeneratedTaskEntry[] Entries;
            public bool Success;
            public string Error;
        }

        public static GenerationResult GenerateFromDecomposition(FeatureDecomposer.DecompositionResult decomposition)
        {
            GenerationResult result = new GenerationResult();
            result.FeatureName = decomposition.FeatureName;
            result.FeatureGroup = decomposition.FeatureGroup;

            if (!decomposition.Success)
            {
                result.Success = false;
                result.Error = "Cannot generate from failed decomposition: " + decomposition.Error;
                return result;
            }

            if (decomposition.Modules == null || decomposition.Modules.Length == 0)
            {
                result.Success = false;
                result.Error = "No modules in decomposition";
                return result;
            }

            DependencyGraphBuilder.DependencyGraph existingGraph = DependencyGraphBuilder.BuildGraph();

            var entries = new List<GeneratedTaskEntry>();
            for (int i = 0; i < decomposition.Modules.Length; i++)
            {
                FeatureDecomposer.DecomposedModule mod = decomposition.Modules[i];

                if (TaskExistsInQueue(existingGraph, mod.Name))
                {
                    Debug.LogWarning(LOG_PREFIX + "Task already exists for: " + mod.Name + " — skipping");
                    continue;
                }

                GeneratedTaskEntry entry = new GeneratedTaskEntry();
                entry.Name = mod.Name;
                entry.Status = "pending";
                entry.Priority = "medium";
                entry.Owner = null;
                entry.Role = null;
                entry.DependsOn = FilterModuleDependencies(mod.Dependencies);
                entry.ModulePath = mod.ModulePath;
                entry.FeatureGoal = mod.Responsibility;
                entry.Requirements = mod.Deliverables;
                entry.BasedOn = "Template";
                entry.References = new string[] { "docs/ai/MODULE_TEMPLATES.md" };
                entry.Constraints = mod.Constraints;
                entry.Deliverables = mod.Deliverables;
                entry.Notes = "Auto-generated from feature: " + decomposition.FeatureName;
                entry.FeatureGroup = decomposition.FeatureGroup;

                entries.Add(entry);
            }

            result.Entries = entries.ToArray();
            result.Success = entries.Count > 0;

            if (result.Success)
                Debug.Log(LOG_PREFIX + "Generated " + entries.Count + " task entries for feature: " + decomposition.FeatureName);
            else
                result.Error = "No new tasks to generate (all modules already exist in queue)";

            return result;
        }

        public static void WriteEntriesToTaskQueue(GeneratedTaskEntry[] entries)
        {
            string path = DependencyGraphBuilder.GetTaskQueuePath();
            if (!File.Exists(path))
            {
                Debug.LogError(LOG_PREFIX + "TASK_QUEUE.yaml not found: " + path);
                return;
            }

            string existing = File.ReadAllText(path);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < entries.Length; i++)
            {
                GeneratedTaskEntry entry = entries[i];
                sb.AppendLine("");
                sb.AppendLine("  - name: " + entry.Name);
                sb.AppendLine("    status: " + entry.Status);
                sb.AppendLine("    priority: " + entry.Priority);
                sb.AppendLine("    owner: null");
                sb.AppendLine("    role: null");
                sb.AppendLine("    depends_on: [" + JoinArray(entry.DependsOn) + "]");
                sb.AppendLine("    module_path: " + entry.ModulePath);
                sb.AppendLine("    feature_group: \"" + entry.FeatureGroup + "\"");
                sb.AppendLine("    feature_goal: \"" + EscapeYaml(entry.FeatureGoal) + "\"");
                sb.AppendLine("    based_on: " + entry.BasedOn);
                sb.AppendLine("    notes: \"" + EscapeYaml(entry.Notes) + "\"");

                if (entry.Deliverables != null && entry.Deliverables.Length > 0)
                {
                    sb.AppendLine("    deliverables:");
                    for (int d = 0; d < entry.Deliverables.Length; d++)
                        sb.AppendLine("      - " + entry.Deliverables[d]);
                }

                if (entry.Constraints != null && entry.Constraints.Length > 0)
                {
                    sb.AppendLine("    constraints:");
                    for (int c = 0; c < entry.Constraints.Length; c++)
                        sb.AppendLine("      - \"" + EscapeYaml(entry.Constraints[c]) + "\"");
                }

                if (entry.References != null && entry.References.Length > 0)
                {
                    sb.AppendLine("    references:");
                    for (int r = 0; r < entry.References.Length; r++)
                        sb.AppendLine("      - \"" + entry.References[r] + "\"");
                }

                sb.AppendLine("    description: \"" + EscapeYaml(entry.FeatureGoal) + "\"");
            }

            File.WriteAllText(path, existing + sb.ToString());
            Debug.Log(LOG_PREFIX + entries.Length + " entries written to TASK_QUEUE.yaml");
        }

        public static void WriteModulesToRegistry(FeatureDecomposer.DecomposedModule[] modules)
        {
            string path = DependencyGraphBuilder.GetRegistryPath();
            if (!File.Exists(path))
            {
                Debug.LogError(LOG_PREFIX + "MODULE_REGISTRY.yaml not found: " + path);
                return;
            }

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            string existing = File.ReadAllText(path);
            StringBuilder sb = new StringBuilder();

            int addedCount = 0;
            for (int i = 0; i < modules.Length; i++)
            {
                FeatureDecomposer.DecomposedModule mod = modules[i];

                if (graph.ModuleMap != null && graph.ModuleMap.ContainsKey(mod.Name))
                {
                    Debug.Log(LOG_PREFIX + "Module already in registry: " + mod.Name + " — skipping");
                    continue;
                }

                sb.AppendLine("");
                sb.AppendLine("  - name: " + mod.Name);
                sb.AppendLine("    path: " + mod.ModulePath);
                sb.AppendLine("    editable: true");
                sb.AppendLine("    risk: low");
                sb.AppendLine("    description: \"" + EscapeYaml(mod.Responsibility) + "\"");
                sb.AppendLine("    dependencies: [" + JoinArray(mod.Dependencies) + "]");

                addedCount++;
            }

            if (addedCount > 0)
            {
                File.WriteAllText(path, existing + sb.ToString());
                Debug.Log(LOG_PREFIX + addedCount + " modules added to MODULE_REGISTRY.yaml");
            }
        }

        static bool TaskExistsInQueue(DependencyGraphBuilder.DependencyGraph graph, string moduleName)
        {
            if (graph.TaskMap == null) return false;
            return graph.TaskMap.ContainsKey(moduleName);
        }

        static string[] FilterModuleDependencies(string[] rawDeps)
        {
            var filtered = new List<string>();
            for (int i = 0; i < rawDeps.Length; i++)
            {
                if (rawDeps[i] == "UnityEngine" || rawDeps[i] == "System")
                    continue;
                filtered.Add(rawDeps[i]);
            }
            return filtered.ToArray();
        }

        static string JoinArray(string[] arr)
        {
            if (arr == null || arr.Length == 0) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(arr[i]);
            }
            return sb.ToString();
        }

        static string EscapeYaml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("\"", "\\\"").Replace("\n", " ");
        }
    }
}
