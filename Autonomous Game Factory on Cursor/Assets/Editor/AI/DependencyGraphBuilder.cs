using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class DependencyGraphBuilder
    {
        const string MODULE_REGISTRY_RELATIVE = "Docs/ai/MODULE_REGISTRY.yaml";
        const string TASK_QUEUE_RELATIVE = "docs/ai/TASK_QUEUE.yaml";

        static readonly Regex REGEX_NAME = new Regex(@"^\s*-?\s*name:\s*(\w+)");
        static readonly Regex REGEX_PATH = new Regex(@"^\s*path:\s*(.+)");
        static readonly Regex REGEX_DEPENDENCIES = new Regex(@"^\s*dependencies:\s*\[([^\]]*)\]");
        static readonly Regex REGEX_DEPENDS_ON = new Regex(@"^\s*depends_on:\s*\[([^\]]*)\]");
        static readonly Regex REGEX_STATUS = new Regex(@"^\s*status:\s*(\w+)");

        public struct RegistryModule
        {
            public string Name;
            public string Path;
            public string[] Dependencies;
        }

        public struct TaskEntry
        {
            public string Name;
            public string Status;
            public string[] DependsOn;
        }

        public struct DependencyGraph
        {
            public RegistryModule[] Modules;
            public Dictionary<string, RegistryModule> ModuleMap;
            public TaskEntry[] Tasks;
            public Dictionary<string, TaskEntry> TaskMap;
        }

        public static string GetRegistryPath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, MODULE_REGISTRY_RELATIVE);
        }

        public static string GetTaskQueuePath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, TASK_QUEUE_RELATIVE);
        }

        public static RegistryModule[] ParseModuleRegistry(string[] lines)
        {
            var list = new List<RegistryModule>();
            string currentName = null;
            string currentPath = null;
            string[] currentDeps = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                Match nameMatch = REGEX_NAME.Match(line);
                if (nameMatch.Success)
                {
                    if (currentName != null && currentPath != null)
                        list.Add(new RegistryModule { Name = currentName, Path = currentPath, Dependencies = currentDeps ?? new string[0] });

                    currentName = nameMatch.Groups[1].Value;
                    currentPath = null;
                    currentDeps = null;
                    continue;
                }

                Match pathMatch = REGEX_PATH.Match(line);
                if (pathMatch.Success && currentName != null)
                {
                    currentPath = pathMatch.Groups[1].Value.Trim();
                    continue;
                }

                Match depMatch = REGEX_DEPENDENCIES.Match(line);
                if (depMatch.Success && currentName != null)
                {
                    currentDeps = ParseBracketList(depMatch.Groups[1].Value);
                    continue;
                }
            }

            if (currentName != null && currentPath != null)
                list.Add(new RegistryModule { Name = currentName, Path = currentPath, Dependencies = currentDeps ?? new string[0] });

            return list.ToArray();
        }

        public static TaskEntry[] ParseTaskQueue(string[] lines)
        {
            var list = new List<TaskEntry>();
            string currentName = null;
            string currentStatus = null;
            string[] currentDeps = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                Match nameMatch = REGEX_NAME.Match(line);
                if (nameMatch.Success)
                {
                    if (currentName != null)
                        list.Add(new TaskEntry { Name = currentName, Status = currentStatus ?? "pending", DependsOn = currentDeps ?? new string[0] });

                    currentName = nameMatch.Groups[1].Value;
                    currentStatus = null;
                    currentDeps = null;
                    continue;
                }

                Match statusMatch = REGEX_STATUS.Match(line);
                if (statusMatch.Success && currentName != null)
                {
                    currentStatus = statusMatch.Groups[1].Value;
                    continue;
                }

                Match depMatch = REGEX_DEPENDS_ON.Match(line);
                if (depMatch.Success && currentName != null)
                {
                    currentDeps = ParseBracketList(depMatch.Groups[1].Value);
                    continue;
                }
            }

            if (currentName != null)
                list.Add(new TaskEntry { Name = currentName, Status = currentStatus ?? "pending", DependsOn = currentDeps ?? new string[0] });

            return list.ToArray();
        }

        public static DependencyGraph BuildGraph()
        {
            DependencyGraph graph = new DependencyGraph();

            string registryPath = GetRegistryPath();
            if (File.Exists(registryPath))
            {
                graph.Modules = ParseModuleRegistry(File.ReadAllLines(registryPath));
            }
            else
            {
                graph.Modules = new RegistryModule[0];
                Debug.LogWarning("[DependencyGraphBuilder] MODULE_REGISTRY.yaml not found: " + registryPath);
            }

            graph.ModuleMap = new Dictionary<string, RegistryModule>(graph.Modules.Length);
            for (int i = 0; i < graph.Modules.Length; i++)
                graph.ModuleMap[graph.Modules[i].Name] = graph.Modules[i];

            string taskPath = GetTaskQueuePath();
            if (File.Exists(taskPath))
            {
                graph.Tasks = ParseTaskQueue(File.ReadAllLines(taskPath));
            }
            else
            {
                graph.Tasks = new TaskEntry[0];
                Debug.LogWarning("[DependencyGraphBuilder] TASK_QUEUE.yaml not found: " + taskPath);
            }

            graph.TaskMap = new Dictionary<string, TaskEntry>(graph.Tasks.Length);
            for (int i = 0; i < graph.Tasks.Length; i++)
                graph.TaskMap[graph.Tasks[i].Name] = graph.Tasks[i];

            return graph;
        }

        public static string[] GetExecutableModules(DependencyGraph graph)
        {
            var executable = new List<string>();
            for (int i = 0; i < graph.Tasks.Length; i++)
            {
                TaskEntry task = graph.Tasks[i];
                if (task.Status != "planned")
                    continue;

                bool ready = true;
                for (int d = 0; d < task.DependsOn.Length; d++)
                {
                    TaskEntry dep;
                    if (!graph.TaskMap.TryGetValue(task.DependsOn[d], out dep) || dep.Status != "done")
                    {
                        ready = false;
                        break;
                    }
                }

                if (ready)
                    executable.Add(task.Name);
            }
            return executable.ToArray();
        }

        public static string[] GetBlockedModules(DependencyGraph graph)
        {
            var blocked = new List<string>();
            for (int i = 0; i < graph.Tasks.Length; i++)
            {
                TaskEntry task = graph.Tasks[i];
                if (task.Status == "done" || task.Status == "in_progress" || task.Status == "review")
                    continue;

                for (int d = 0; d < task.DependsOn.Length; d++)
                {
                    TaskEntry dep;
                    if (!graph.TaskMap.TryGetValue(task.DependsOn[d], out dep) || dep.Status != "done")
                    {
                        blocked.Add(task.Name);
                        break;
                    }
                }
            }
            return blocked.ToArray();
        }

        public static bool DetectCycle(RegistryModule[] modules, out string cycleChain)
        {
            cycleChain = null;
            var moduleMap = new Dictionary<string, string[]>(modules.Length);
            for (int i = 0; i < modules.Length; i++)
                moduleMap[modules[i].Name] = FilterModuleDependencies(modules[i].Dependencies, moduleMap);

            var visited = new Dictionary<string, int>(modules.Length);
            for (int i = 0; i < modules.Length; i++)
                visited[modules[i].Name] = 0;

            var path = new List<string>();

            for (int i = 0; i < modules.Length; i++)
            {
                if (visited[modules[i].Name] == 0)
                {
                    if (DFSDetectCycle(modules[i].Name, moduleMap, visited, path, out cycleChain))
                        return true;
                }
            }

            return false;
        }

        public static string[] TopologicalSort(RegistryModule[] modules)
        {
            var moduleNames = new HashSet<string>(modules.Length);
            var adjList = new Dictionary<string, List<string>>(modules.Length);
            var inDegree = new Dictionary<string, int>(modules.Length);

            for (int i = 0; i < modules.Length; i++)
            {
                string name = modules[i].Name;
                moduleNames.Add(name);
                if (!adjList.ContainsKey(name))
                    adjList[name] = new List<string>();
                if (!inDegree.ContainsKey(name))
                    inDegree[name] = 0;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                string[] deps = modules[i].Dependencies;
                for (int d = 0; d < deps.Length; d++)
                {
                    if (!moduleNames.Contains(deps[d]))
                        continue;

                    if (!adjList.ContainsKey(deps[d]))
                        adjList[deps[d]] = new List<string>();

                    adjList[deps[d]].Add(modules[i].Name);

                    if (inDegree.ContainsKey(modules[i].Name))
                        inDegree[modules[i].Name] = inDegree[modules[i].Name] + 1;
                    else
                        inDegree[modules[i].Name] = 1;
                }
            }

            var queue = new Queue<string>();
            var keys = new List<string>(inDegree.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                if (inDegree[keys[i]] == 0)
                    queue.Enqueue(keys[i]);
            }

            var sorted = new List<string>();
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                sorted.Add(current);

                List<string> neighbors;
                if (adjList.TryGetValue(current, out neighbors))
                {
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        inDegree[neighbors[i]] = inDegree[neighbors[i]] - 1;
                        if (inDegree[neighbors[i]] == 0)
                            queue.Enqueue(neighbors[i]);
                    }
                }
            }

            return sorted.ToArray();
        }

        static bool DFSDetectCycle(string node, Dictionary<string, string[]> adjList, Dictionary<string, int> visited, List<string> path, out string cycleChain)
        {
            cycleChain = null;
            visited[node] = 1;
            path.Add(node);

            string[] deps;
            if (adjList.TryGetValue(node, out deps))
            {
                for (int i = 0; i < deps.Length; i++)
                {
                    string dep = deps[i];
                    int state;
                    if (!visited.TryGetValue(dep, out state))
                        continue;

                    if (state == 1)
                    {
                        int cycleStart = path.IndexOf(dep);
                        var sb = new System.Text.StringBuilder();
                        for (int c = cycleStart; c < path.Count; c++)
                        {
                            sb.Append(path[c]);
                            sb.Append(" → ");
                        }
                        sb.Append(dep);
                        cycleChain = sb.ToString();
                        path.RemoveAt(path.Count - 1);
                        return true;
                    }

                    if (state == 0)
                    {
                        if (DFSDetectCycle(dep, adjList, visited, path, out cycleChain))
                        {
                            path.RemoveAt(path.Count - 1);
                            return true;
                        }
                    }
                }
            }

            visited[node] = 2;
            path.RemoveAt(path.Count - 1);
            return false;
        }

        static string[] FilterModuleDependencies(string[] rawDeps, Dictionary<string, string[]> knownModules)
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

        static string[] ParseBracketList(string bracketContent)
        {
            string trimmed = bracketContent.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return new string[0];

            string[] parts = trimmed.Split(',');
            var result = new List<string>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                string clean = parts[i].Trim().Trim('"', '\'', ' ');
                if (!string.IsNullOrEmpty(clean))
                    result.Add(clean);
            }
            return result.ToArray();
        }
    }
}
