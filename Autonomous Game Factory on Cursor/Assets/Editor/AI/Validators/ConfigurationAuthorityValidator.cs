using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ConfigurationAuthorityValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ConfigurationAuthority";
        const string MODULES_RELATIVE = "Game/Modules";
        const string CONFIG_SUFFIX = "Config.cs";
        const string FACTORY_SUFFIX = "Factory.cs";
        const string BOOTSTRAP_SUFFIX = "Bootstrap.cs";
        const string RUNTIME_SUFFIX = "Runtime.cs";
        const int MIN_CONCEPT_LENGTH = 3;
        const float CONCEPT_MATCH_THRESHOLD = 0.7f;

        static readonly string[] RUNTIME_CONCEPT_KEYWORDS = new string[]
        {
            "capacity", "max", "limit", "size", "count",
            "speed", "duration", "cooldown", "interval", "delay",
            "cost", "price", "rate", "damage", "range",
            "health", "hp", "stack", "slot", "tier", "level"
        };

        static readonly Regex REGEX_SERIALIZED_FIELD =
            new Regex(@"\[SerializeField\]\s*(?:int|float|bool|string|[\w<>\[\]]+)\s+(_\w+)", RegexOptions.Multiline);

        static readonly Regex REGEX_PUBLIC_FIELD =
            new Regex(@"^\s*public\s+(?:int|float|bool|string|[\w<>\[\]]+)\s+(\w+)\s*[;={]", RegexOptions.Multiline);

        static readonly Regex REGEX_CONST_FIELD =
            new Regex(@"^\s*(?:public\s+)?const\s+(?:int|float|bool|string)\s+(\w+)\s*=", RegexOptions.Multiline);

        static readonly Regex REGEX_CONFIG_INJECT =
            new Regex(@"(?:config|Config|_config)\s*\.\s*(\w+)", RegexOptions.Multiline);

        struct ConfigFieldInfo
        {
            public string ModuleName;
            public string FieldName;
            public string NormalizedName;
            public string FilePath;
            public string SourceType;
            public string[] Concepts;
        }

        struct ConflictPair
        {
            public ConfigFieldInfo FieldA;
            public ConfigFieldInfo FieldB;
            public string Reason;
        }

        public int Validate(ValidationReport report)
        {
            string modulesPath = Path.Combine(Application.dataPath, MODULES_RELATIVE);
            if (!Directory.Exists(modulesPath)) return 0;

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            List<ConfigFieldInfo> allFields = new List<ConfigFieldInfo>();
            int scanned = 0;

            string[] moduleDirs = Directory.GetDirectories(modulesPath);
            for (int i = 0; i < moduleDirs.Length; i++)
            {
                string dirName = Path.GetFileName(moduleDirs[i]);
                if (dirName == "Template") continue;

                scanned += ExtractConfigFields(moduleDirs[i], dirName, allFields);
                scanned += DetectFactoryInjectionConflicts(moduleDirs[i], dirName, allFields);
            }

            List<ConflictPair> conflicts = new List<ConflictPair>();
            DetectCrossModuleConflicts(graph, allFields, conflicts);
            DetectConceptualConflicts(allFields, conflicts);

            ReportConflicts(conflicts, report);

            return scanned;
        }

        int ExtractConfigFields(string moduleDir, string moduleName, List<ConfigFieldInfo> output)
        {
            string configFile = Path.Combine(moduleDir, moduleName + CONFIG_SUFFIX);
            if (!File.Exists(configFile)) return 0;

            string content = File.ReadAllText(configFile);
            string relativePath = "Assets/" + MODULES_RELATIVE + "/" + moduleName + "/" + moduleName + CONFIG_SUFFIX;

            MatchCollection serialized = REGEX_SERIALIZED_FIELD.Matches(content);
            for (int i = 0; i < serialized.Count; i++)
            {
                string fieldName = serialized[i].Groups[1].Value;
                output.Add(CreateFieldInfo(moduleName, fieldName, relativePath, "Config_Serialized"));
            }

            MatchCollection publicFields = REGEX_PUBLIC_FIELD.Matches(content);
            for (int i = 0; i < publicFields.Count; i++)
            {
                string fieldName = publicFields[i].Groups[1].Value;
                bool exists = false;
                for (int j = 0; j < output.Count; j++)
                {
                    if (output[j].ModuleName == moduleName && output[j].FieldName == fieldName)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    output.Add(CreateFieldInfo(moduleName, fieldName, relativePath, "Config_Public"));
            }

            return 1;
        }

        int DetectFactoryInjectionConflicts(string moduleDir, string moduleName, List<ConfigFieldInfo> output)
        {
            int scanned = 0;

            string factoryFile = Path.Combine(moduleDir, moduleName + FACTORY_SUFFIX);
            if (File.Exists(factoryFile))
            {
                scanned++;
                string content = File.ReadAllText(factoryFile);
                string relativePath = "Assets/" + MODULES_RELATIVE + "/" + moduleName + "/" + moduleName + FACTORY_SUFFIX;
                MatchCollection injections = REGEX_CONFIG_INJECT.Matches(content);
                for (int i = 0; i < injections.Count; i++)
                {
                    string fieldName = injections[i].Groups[1].Value;
                    bool exists = false;
                    for (int j = 0; j < output.Count; j++)
                    {
                        if (output[j].ModuleName == moduleName && output[j].FieldName == fieldName)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                        output.Add(CreateFieldInfo(moduleName, fieldName, relativePath, "Factory_Inject"));
                }
            }

            string bootstrapFile = Path.Combine(moduleDir, moduleName + BOOTSTRAP_SUFFIX);
            if (File.Exists(bootstrapFile))
            {
                scanned++;
                string content = File.ReadAllText(bootstrapFile);
                string relativePath = "Assets/" + MODULES_RELATIVE + "/" + moduleName + "/" + moduleName + BOOTSTRAP_SUFFIX;
                MatchCollection injections = REGEX_CONFIG_INJECT.Matches(content);
                for (int i = 0; i < injections.Count; i++)
                {
                    string fieldName = injections[i].Groups[1].Value;
                    bool exists = false;
                    for (int j = 0; j < output.Count; j++)
                    {
                        if (output[j].ModuleName == moduleName && output[j].FieldName == fieldName)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                        output.Add(CreateFieldInfo(moduleName, fieldName, relativePath, "Bootstrap_Inject"));
                }
            }

            return scanned;
        }

        ConfigFieldInfo CreateFieldInfo(string moduleName, string fieldName, string filePath, string sourceType)
        {
            string normalized = NormalizeName(fieldName);
            return new ConfigFieldInfo
            {
                ModuleName = moduleName,
                FieldName = fieldName,
                NormalizedName = normalized,
                FilePath = filePath,
                SourceType = sourceType,
                Concepts = ExtractConcepts(normalized)
            };
        }

        void DetectCrossModuleConflicts(
            DependencyGraphBuilder.DependencyGraph graph,
            List<ConfigFieldInfo> allFields,
            List<ConflictPair> conflicts)
        {
            for (int i = 0; i < graph.ModuleMap.Count; i++)
            {
                DependencyGraphBuilder.RegistryModule module = graph.ModuleMap[i];
                if (module.Dependencies == null) continue;

                for (int d = 0; d < module.Dependencies.Length; d++)
                {
                    string dep = module.Dependencies[d];
                    if (dep == "UnityEngine" || dep == "System") continue;

                    for (int a = 0; a < allFields.Count; a++)
                    {
                        if (allFields[a].ModuleName != module.Name) continue;
                        if (allFields[a].SourceType == "Factory_Inject" || allFields[a].SourceType == "Bootstrap_Inject")
                            continue;

                        for (int b = 0; b < allFields.Count; b++)
                        {
                            if (allFields[b].ModuleName != dep) continue;
                            if (allFields[b].SourceType == "Factory_Inject" || allFields[b].SourceType == "Bootstrap_Inject")
                                continue;

                            if (allFields[a].NormalizedName == allFields[b].NormalizedName)
                            {
                                conflicts.Add(new ConflictPair
                                {
                                    FieldA = allFields[a],
                                    FieldB = allFields[b],
                                    Reason = "Exact duplicate config field name across dependent modules. "
                                        + "Only one module should be source-of-truth for '" + allFields[a].NormalizedName + "'."
                                });
                            }
                            else if (SharesConcepts(allFields[a].Concepts, allFields[b].Concepts))
                            {
                                conflicts.Add(new ConflictPair
                                {
                                    FieldA = allFields[a],
                                    FieldB = allFields[b],
                                    Reason = "Config fields controlling similar concept across dependent modules. "
                                        + "Risk: ambiguous source-of-truth for same runtime behavior."
                                });
                            }
                        }
                    }
                }
            }
        }

        void DetectConceptualConflicts(List<ConfigFieldInfo> allFields, List<ConflictPair> conflicts)
        {
            for (int a = 0; a < allFields.Count; a++)
            {
                if (allFields[a].SourceType == "Factory_Inject" || allFields[a].SourceType == "Bootstrap_Inject")
                    continue;

                for (int b = a + 1; b < allFields.Count; b++)
                {
                    if (allFields[b].SourceType == "Factory_Inject" || allFields[b].SourceType == "Bootstrap_Inject")
                        continue;
                    if (allFields[a].ModuleName == allFields[b].ModuleName) continue;

                    bool alreadyReported = false;
                    for (int c = 0; c < conflicts.Count; c++)
                    {
                        if ((conflicts[c].FieldA.ModuleName == allFields[a].ModuleName
                             && conflicts[c].FieldB.ModuleName == allFields[b].ModuleName
                             && conflicts[c].FieldA.FieldName == allFields[a].FieldName
                             && conflicts[c].FieldB.FieldName == allFields[b].FieldName)
                            || (conflicts[c].FieldA.ModuleName == allFields[b].ModuleName
                                && conflicts[c].FieldB.ModuleName == allFields[a].ModuleName
                                && conflicts[c].FieldA.FieldName == allFields[b].FieldName
                                && conflicts[c].FieldB.FieldName == allFields[a].FieldName))
                        {
                            alreadyReported = true;
                            break;
                        }
                    }
                    if (alreadyReported) continue;

                    if (allFields[a].NormalizedName == allFields[b].NormalizedName)
                    {
                        conflicts.Add(new ConflictPair
                        {
                            FieldA = allFields[a],
                            FieldB = allFields[b],
                            Reason = "Identical config field name in unrelated modules. "
                                + "Risk: runtime confusion over which config governs the behavior."
                        });
                    }
                }
            }
        }

        void ReportConflicts(List<ConflictPair> conflicts, ValidationReport report)
        {
            for (int i = 0; i < conflicts.Count; i++)
            {
                ConflictPair c = conflicts[i];
                string preferred = DeterminePreferredAuthority(c.FieldA, c.FieldB);
                bool isBlocking = IsRuntimeSafetyRisk(c);

                string message = "Configuration authority conflict: "
                    + c.FieldA.ModuleName + "." + c.FieldA.FieldName
                    + " (" + c.FieldA.SourceType + ") vs "
                    + c.FieldB.ModuleName + "." + c.FieldB.FieldName
                    + " (" + c.FieldB.SourceType + "). "
                    + c.Reason
                    + " Recommended source-of-truth: " + preferred + "Config. "
                    + "Architectural fix: consolidate this config field into " + preferred + "Config "
                    + "and reference it from the dependent module.";

                if (isBlocking)
                    report.AddError(VALIDATOR_NAME, message, c.FieldA.FilePath);
                else
                    report.AddWarning(VALIDATOR_NAME, message, c.FieldA.FilePath);
            }
        }

        static string DeterminePreferredAuthority(ConfigFieldInfo a, ConfigFieldInfo b)
        {
            if (a.SourceType.StartsWith("Config") && !b.SourceType.StartsWith("Config"))
                return a.ModuleName;
            if (b.SourceType.StartsWith("Config") && !a.SourceType.StartsWith("Config"))
                return b.ModuleName;

            return a.ModuleName;
        }

        static bool IsRuntimeSafetyRisk(ConflictPair conflict)
        {
            if (conflict.FieldA.SourceType == "Factory_Inject" || conflict.FieldB.SourceType == "Factory_Inject")
                return true;
            if (conflict.FieldA.SourceType == "Bootstrap_Inject" || conflict.FieldB.SourceType == "Bootstrap_Inject")
                return true;
            return false;
        }

        static string NormalizeName(string name)
        {
            if (name.Length > 0 && name[0] == '_')
                name = name.Substring(1);

            System.Text.StringBuilder sb = new System.Text.StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                    sb.Append('_');
                sb.Append(char.ToLower(name[i]));
            }
            return sb.ToString();
        }

        static string[] ExtractConcepts(string normalizedName)
        {
            string[] parts = normalizedName.Split('_');
            List<string> concepts = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length < MIN_CONCEPT_LENGTH) continue;

                for (int k = 0; k < RUNTIME_CONCEPT_KEYWORDS.Length; k++)
                {
                    if (parts[i] == RUNTIME_CONCEPT_KEYWORDS[k] || parts[i].Contains(RUNTIME_CONCEPT_KEYWORDS[k]))
                    {
                        concepts.Add(RUNTIME_CONCEPT_KEYWORDS[k]);
                        break;
                    }
                }
            }
            return concepts.ToArray();
        }

        static bool SharesConcepts(string[] conceptsA, string[] conceptsB)
        {
            if (conceptsA.Length == 0 || conceptsB.Length == 0) return false;

            int shared = 0;
            for (int a = 0; a < conceptsA.Length; a++)
            {
                for (int b = 0; b < conceptsB.Length; b++)
                {
                    if (conceptsA[a] == conceptsB[b])
                    {
                        shared++;
                        break;
                    }
                }
            }

            int minLen = conceptsA.Length < conceptsB.Length ? conceptsA.Length : conceptsB.Length;
            if (minLen == 0) return false;

            return (float)shared / minLen >= CONCEPT_MATCH_THRESHOLD;
        }
    }
}
