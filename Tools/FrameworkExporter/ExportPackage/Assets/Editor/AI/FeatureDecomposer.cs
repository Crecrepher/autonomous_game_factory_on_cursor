using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class FeatureDecomposer
    {
        const string LOG_PREFIX = "[FeatureDecomposer] ";
        const int MAX_MODULES_PER_FEATURE = 10;
        const string MODULE_PATH_PREFIX = "Assets/Game/Modules/";

        public struct DecomposedModule
        {
            public string Name;
            public string ModulePath;
            public string Responsibility;
            public string[] Dependencies;
            public string[] Deliverables;
            public string[] Constraints;
            public string FeatureGroup;
        }

        public struct DecompositionResult
        {
            public string FeatureName;
            public string FeatureGroup;
            public DecomposedModule[] Modules;
            public bool Success;
            public string Error;
        }

        public static DecompositionResult Decompose(FeatureIntake.FeatureEntry feature, DependencyGraphBuilder.DependencyGraph existingGraph)
        {
            DecompositionResult result = new DecompositionResult();
            result.FeatureName = feature.Name;
            result.FeatureGroup = feature.FeatureGroup;

            if (string.IsNullOrEmpty(feature.Name))
            {
                result.Success = false;
                result.Error = "Feature name is empty";
                return result;
            }

            if (string.IsNullOrEmpty(feature.FeatureGroup))
            {
                result.Success = false;
                result.Error = "Feature group is empty";
                return result;
            }

            if (feature.Modules != null && feature.Modules.Length > 0)
            {
                result.Modules = BuildModulesFromExplicitList(feature, existingGraph);
            }
            else
            {
                result.Modules = new DecomposedModule[0];
                Debug.LogWarning(LOG_PREFIX + "No modules specified for feature: " + feature.Name +
                    ". Modules should be provided by the AI agent after analyzing the feature description.");
            }

            if (result.Modules.Length > MAX_MODULES_PER_FEATURE)
            {
                result.Success = false;
                result.Error = "Too many modules (" + result.Modules.Length + "). Max: " + MAX_MODULES_PER_FEATURE + ". Consider splitting into sub-features.";
                return result;
            }

            for (int i = 0; i < result.Modules.Length; i++)
            {
                if (!ValidateModuleName(result.Modules[i].Name))
                {
                    result.Success = false;
                    result.Error = "Invalid module name: " + result.Modules[i].Name;
                    return result;
                }

                if (IsGodModule(result.Modules[i].Name))
                {
                    result.Success = false;
                    result.Error = "God-module detected: " + result.Modules[i].Name + ". Split into smaller modules.";
                    return result;
                }
            }

            if (!ValidateNoCyclicDependencies(result.Modules))
            {
                result.Success = false;
                result.Error = "Cyclic dependency detected among decomposed modules";
                return result;
            }

            result.Success = true;
            Debug.Log(LOG_PREFIX + "Decomposed '" + feature.Name + "' into " + result.Modules.Length + " modules");
            return result;
        }

        static DecomposedModule[] BuildModulesFromExplicitList(FeatureIntake.FeatureEntry feature, DependencyGraphBuilder.DependencyGraph existingGraph)
        {
            var modules = new List<DecomposedModule>();

            for (int i = 0; i < feature.Modules.Length; i++)
            {
                string moduleName = feature.Modules[i].Trim();
                if (string.IsNullOrEmpty(moduleName))
                    continue;

                bool existsInRegistry = existingGraph.ModuleMap != null &&
                    existingGraph.ModuleMap.ContainsKey(moduleName);

                if (existsInRegistry)
                {
                    Debug.Log(LOG_PREFIX + "Module '" + moduleName + "' already exists in registry. Skipping creation (extension only if needed).");
                    continue;
                }

                DecomposedModule mod = new DecomposedModule();
                mod.Name = moduleName;
                mod.ModulePath = MODULE_PATH_PREFIX + moduleName;
                mod.Responsibility = "Part of feature: " + feature.Name;
                mod.FeatureGroup = feature.FeatureGroup;
                mod.Dependencies = new string[] { "UnityEngine", "System" };
                mod.Deliverables = GetStandardDeliverables(moduleName);
                mod.Constraints = feature.Constraints != null ? feature.Constraints : new string[0];

                modules.Add(mod);
            }

            return modules.ToArray();
        }

        static string[] GetStandardDeliverables(string moduleName)
        {
            return new string[]
            {
                "I" + moduleName + ".cs",
                moduleName + "Runtime.cs",
                moduleName + "Config.cs",
                moduleName + "Factory.cs",
                moduleName + "Bootstrap.cs",
                "Tests/Editor/" + moduleName + "Tests.cs"
            };
        }

        static bool ValidateModuleName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.Length < 3)
                return false;

            if (!char.IsUpper(name[0]))
                return false;

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (!char.IsLetterOrDigit(c))
                    return false;
            }

            return true;
        }

        static readonly string[] GOD_MODULE_PATTERNS = new string[]
        {
            "GameplaySystem",
            "CombatManagerEverything",
            "UniversalManager",
            "EverythingController",
            "MasterSystem",
            "GlobalManager",
            "AllInOneSystem"
        };

        static bool IsGodModule(string name)
        {
            for (int i = 0; i < GOD_MODULE_PATTERNS.Length; i++)
            {
                if (name == GOD_MODULE_PATTERNS[i])
                    return true;
            }
            return false;
        }

        static bool ValidateNoCyclicDependencies(DecomposedModule[] modules)
        {
            var nameSet = new HashSet<string>();
            for (int i = 0; i < modules.Length; i++)
                nameSet.Add(modules[i].Name);

            var visited = new Dictionary<string, int>();
            var adjList = new Dictionary<string, string[]>();
            for (int i = 0; i < modules.Length; i++)
            {
                visited[modules[i].Name] = 0;
                var modDeps = new List<string>();
                for (int d = 0; d < modules[i].Dependencies.Length; d++)
                {
                    if (nameSet.Contains(modules[i].Dependencies[d]))
                        modDeps.Add(modules[i].Dependencies[d]);
                }
                adjList[modules[i].Name] = modDeps.ToArray();
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (visited[modules[i].Name] == 0)
                {
                    if (DFSCheck(modules[i].Name, adjList, visited))
                        return false;
                }
            }

            return true;
        }

        static bool DFSCheck(string node, Dictionary<string, string[]> adjList, Dictionary<string, int> visited)
        {
            visited[node] = 1;
            string[] deps;
            if (adjList.TryGetValue(node, out deps))
            {
                for (int i = 0; i < deps.Length; i++)
                {
                    int state;
                    if (!visited.TryGetValue(deps[i], out state))
                        continue;
                    if (state == 1) return true;
                    if (state == 0 && DFSCheck(deps[i], adjList, visited))
                        return true;
                }
            }
            visited[node] = 2;
            return false;
        }

        public static string FormatDecompositionReport(DecompositionResult result)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Feature Decomposition: " + result.FeatureName);
            sb.AppendLine("");
            sb.AppendLine("Feature Group: " + result.FeatureGroup);
            sb.AppendLine("Status: " + (result.Success ? "SUCCESS" : "FAILED — " + result.Error));
            sb.AppendLine("Module Count: " + (result.Modules != null ? result.Modules.Length : 0));
            sb.AppendLine("");

            if (result.Modules != null)
            {
                sb.AppendLine("## Modules");
                sb.AppendLine("");
                for (int i = 0; i < result.Modules.Length; i++)
                {
                    DecomposedModule mod = result.Modules[i];
                    sb.AppendLine("### " + (i + 1) + ". " + mod.Name);
                    sb.AppendLine("- Path: " + mod.ModulePath);
                    sb.AppendLine("- Responsibility: " + mod.Responsibility);
                    sb.AppendLine("- Dependencies: [" + JoinArray(mod.Dependencies) + "]");
                    sb.AppendLine("- Deliverables:");
                    for (int d = 0; d < mod.Deliverables.Length; d++)
                        sb.AppendLine("  - " + mod.Deliverables[d]);
                    sb.AppendLine("");
                }
            }

            return sb.ToString();
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
    }
}
