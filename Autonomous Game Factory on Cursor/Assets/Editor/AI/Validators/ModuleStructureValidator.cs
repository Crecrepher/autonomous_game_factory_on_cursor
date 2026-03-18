using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ModuleStructureValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ModuleStructure";
        const string MODULE_REGISTRY_PATH = "docs/ai/MODULE_REGISTRY.yaml";

        public int Validate(ValidationReport report)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string registryFullPath = Path.Combine(projectRoot, MODULE_REGISTRY_PATH);
            if (!File.Exists(registryFullPath))
            {
                report.AddWarning(VALIDATOR_NAME, "MODULE_REGISTRY.yaml not found at " + MODULE_REGISTRY_PATH, null);
                return 0;
            }

            List<ModuleEntry> modules = ParseModuleRegistry(File.ReadAllLines(registryFullPath));
            Debug.Log("[ModuleStructureValidator] Parsed module count: " + modules.Count);
            int scannedCount = 0;

            for (int i = 0; i < modules.Count; i++)
            {
                ModuleEntry entry = modules[i];
                if (entry.Path.StartsWith("Assets/Game/Modules/") == false)
                    continue;

                scannedCount++;

                string moduleFullPath = Path.Combine(projectRoot, entry.Path);
                if (!Directory.Exists(moduleFullPath))
                {
                    report.AddWarning(VALIDATOR_NAME, "Module folder missing: " + entry.Path, entry.Path);
                    continue;
                }

                string moduleName = entry.Name;
                string interfaceFile = moduleName + ".cs";
                string interfacePath = Path.Combine(moduleFullPath, "I" + interfaceFile);
                if (!File.Exists(interfacePath))
                    report.AddError(VALIDATOR_NAME, "Missing interface: I" + interfaceFile, entry.Path + "/I" + interfaceFile);

                string configFile = moduleName + "Config.cs";
                string configPath = Path.Combine(moduleFullPath, configFile);
                if (!File.Exists(configPath))
                    report.AddError(VALIDATOR_NAME, "Missing config: " + configFile, entry.Path + "/" + configFile);

                string runtimeFile = moduleName + "Runtime.cs";
                string runtimePath = Path.Combine(moduleFullPath, runtimeFile);
                if (!File.Exists(runtimePath))
                    report.AddError(VALIDATOR_NAME, "Missing runtime: " + runtimeFile, entry.Path + "/" + runtimeFile);

                string factoryFile = moduleName + "Factory.cs";
                string factoryPath = Path.Combine(moduleFullPath, factoryFile);
                if (!File.Exists(factoryPath))
                    report.AddError(VALIDATOR_NAME, "Missing factory: " + factoryFile, entry.Path + "/" + factoryFile);

                string testsFolder = Path.Combine(moduleFullPath, "Tests");
                if (!Directory.Exists(testsFolder))
                {
                    report.AddError(VALIDATOR_NAME, "Missing Tests folder in module: " + entry.Name, entry.Path + "/Tests");
                }
                else
                {
                    string[] testFiles = Directory.GetFiles(testsFolder, "*Tests.cs", SearchOption.AllDirectories);
                    if (testFiles == null || testFiles.Length == 0)
                        report.AddError(VALIDATOR_NAME, "No *Tests.cs in Tests folder: " + entry.Name, entry.Path + "/Tests");
                }
            }

            return scannedCount;
        }

        struct ModuleEntry
        {
            public string Name;
            public string Path;
        }

        static List<ModuleEntry> ParseModuleRegistry(string[] lines)
        {
            var list = new List<ModuleEntry>();
            string currentName = null;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Match nameMatch = Regex.Match(line, @"^\s*-?\s*name:\s*(\w+)");
                if (nameMatch.Success)
                {
                    currentName = nameMatch.Groups[1].Value;
                    continue;
                }
                Match pathMatch = Regex.Match(line, @"^\s*path:\s*(.+)");
                if (pathMatch.Success && currentName != null)
                {
                    list.Add(new ModuleEntry { Name = currentName, Path = pathMatch.Groups[1].Value.Trim() });
                    currentName = null;
                }
            }
            return list;
        }
    }
}
