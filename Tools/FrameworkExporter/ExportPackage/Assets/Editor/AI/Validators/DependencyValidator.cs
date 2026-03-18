using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class DependencyValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "Dependency";
        const string MODULES_RELATIVE = "Game/Modules";
        const string CS_EXTENSION = "*.cs";
        const string TESTS_FOLDER = "Tests";

        static readonly Regex REGEX_USING_GAME = new Regex(@"^\s*using\s+Game\.(\w+)");

        public int Validate(ValidationReport report)
        {
            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            int scannedCount = 0;

            scannedCount += ValidateRegistryDependencies(graph, report);
            scannedCount += ValidateCodeReferences(graph, report);
            scannedCount += ValidateTaskQueueConsistency(graph, report);

            return scannedCount;
        }

        int ValidateRegistryDependencies(DependencyGraphBuilder.DependencyGraph graph, ValidationReport report)
        {
            int count = 0;

            for (int i = 0; i < graph.Modules.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule module = graph.Modules[i];
                count++;

                for (int d = 0; d < module.Dependencies.Length; d++)
                {
                    string dep = module.Dependencies[d];
                    if (dep == "UnityEngine" || dep == "System")
                        continue;

                    if (!graph.ModuleMap.ContainsKey(dep))
                    {
                        report.AddError(VALIDATOR_NAME,
                            "Module '" + module.Name + "' depends on '" + dep + "' which is not registered in MODULE_REGISTRY.yaml",
                            module.Path);
                    }
                }
            }

            return count;
        }

        int ValidateCodeReferences(DependencyGraphBuilder.DependencyGraph graph, ValidationReport report)
        {
            int scannedCount = 0;
            string modulesPath = Path.Combine(Application.dataPath, MODULES_RELATIVE);
            if (!Directory.Exists(modulesPath))
                return 0;

            var moduleNameSet = new HashSet<string>(graph.Modules.Length);
            for (int i = 0; i < graph.Modules.Length; i++)
                moduleNameSet.Add(graph.Modules[i].Name);

            for (int i = 0; i < graph.Modules.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule module = graph.Modules[i];
                string moduleFullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), module.Path);
                if (!Directory.Exists(moduleFullPath))
                    continue;

                var allowedModuleDeps = new HashSet<string>();
                for (int d = 0; d < module.Dependencies.Length; d++)
                {
                    if (module.Dependencies[d] != "UnityEngine" && module.Dependencies[d] != "System")
                        allowedModuleDeps.Add(module.Dependencies[d]);
                }

                string[] csFiles = Directory.GetFiles(moduleFullPath, CS_EXTENSION, SearchOption.AllDirectories);
                for (int f = 0; f < csFiles.Length; f++)
                {
                    string relativePath = csFiles[f].Replace(Path.GetDirectoryName(Application.dataPath) + Path.DirectorySeparatorChar, "");
                    if (IsTestFile(relativePath))
                        continue;

                    scannedCount++;
                    string[] fileLines = File.ReadAllLines(csFiles[f]);

                    for (int line = 0; line < fileLines.Length; line++)
                    {
                        Match usingMatch = REGEX_USING_GAME.Match(fileLines[line]);
                        if (!usingMatch.Success)
                            continue;

                        string referencedModule = usingMatch.Groups[1].Value;

                        if (referencedModule == module.Name)
                            continue;

                        if (!moduleNameSet.Contains(referencedModule))
                            continue;

                        if (!allowedModuleDeps.Contains(referencedModule))
                        {
                            report.AddError(VALIDATOR_NAME,
                                "Module '" + module.Name + "' references '" + referencedModule + "' via using statement, but this dependency is not declared in MODULE_REGISTRY.yaml",
                                relativePath + " (line " + (line + 1) + ")");
                        }
                    }
                }
            }

            return scannedCount;
        }

        int ValidateTaskQueueConsistency(DependencyGraphBuilder.DependencyGraph graph, ValidationReport report)
        {
            int count = 0;

            for (int i = 0; i < graph.Tasks.Length; i++)
            {
                DependencyGraphBuilder.TaskEntry task = graph.Tasks[i];
                count++;

                if (!graph.ModuleMap.ContainsKey(task.Name))
                {
                    report.AddError(VALIDATOR_NAME,
                        "Task '" + task.Name + "' in TASK_QUEUE.yaml has no matching entry in MODULE_REGISTRY.yaml",
                        "TASK_QUEUE.yaml");
                    continue;
                }

                DependencyGraphBuilder.RegistryModule registryModule = graph.ModuleMap[task.Name];

                var registryModuleDeps = new HashSet<string>();
                for (int d = 0; d < registryModule.Dependencies.Length; d++)
                {
                    string dep = registryModule.Dependencies[d];
                    if (dep != "UnityEngine" && dep != "System")
                        registryModuleDeps.Add(dep);
                }

                for (int d = 0; d < task.DependsOn.Length; d++)
                {
                    string taskDep = task.DependsOn[d];

                    if (!graph.ModuleMap.ContainsKey(taskDep))
                    {
                        report.AddError(VALIDATOR_NAME,
                            "Task '" + task.Name + "' depends on '" + taskDep + "' which is not registered in MODULE_REGISTRY.yaml",
                            "TASK_QUEUE.yaml");
                    }

                    if (!registryModuleDeps.Contains(taskDep))
                    {
                        report.AddWarning(VALIDATOR_NAME,
                            "Task '" + task.Name + "' has depends_on '" + taskDep + "' in TASK_QUEUE but this is not listed in MODULE_REGISTRY.yaml dependencies",
                            "TASK_QUEUE.yaml");
                    }
                }
            }

            return count;
        }

        static bool IsTestFile(string relativePath)
        {
            string normalized = relativePath.Replace('\\', '/');
            return normalized.Contains("/" + TESTS_FOLDER + "/");
        }
    }
}
