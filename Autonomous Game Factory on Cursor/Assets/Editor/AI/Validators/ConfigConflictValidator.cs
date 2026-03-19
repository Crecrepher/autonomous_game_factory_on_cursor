using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ConfigConflictValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ConfigConflict";
        const string MODULES_RELATIVE = "Game/Modules";
        const string CONFIG_SUFFIX = "Config.cs";
        const int MIN_FIELD_NAME_LENGTH = 3;
        const float SIMILARITY_THRESHOLD = 0.6f;

        static readonly Regex REGEX_SERIALIZED_FIELD =
            new Regex(@"\[SerializeField\]\s*(?:int|float|bool|string)\s+(_\w+)");
        static readonly Regex REGEX_PRIVATE_FIELD =
            new Regex(@"^\s*(?:int|float|bool|string)\s+(_\w+)\s*=", RegexOptions.Multiline);
        static readonly Regex REGEX_CONST_FIELD =
            new Regex(@"^\s*const\s+(?:int|float|bool|string)\s+(\w+)\s*=", RegexOptions.Multiline);

        struct ConfigField
        {
            public string ModuleName;
            public string FieldName;
            public string FilePath;
        }

        public int Validate(ValidationReport report)
        {
            string modulesPath = Path.Combine(Application.dataPath, MODULES_RELATIVE);
            if (!Directory.Exists(modulesPath))
                return 0;

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            List<ConfigField> allFields = new List<ConfigField>();
            int scannedCount = 0;

            string[] moduleDirs = Directory.GetDirectories(modulesPath);
            for (int i = 0; i < moduleDirs.Length; i++)
            {
                string dirName = Path.GetFileName(moduleDirs[i]);
                if (dirName == "Template")
                    continue;

                string configFile = Path.Combine(moduleDirs[i], dirName + CONFIG_SUFFIX);
                if (!File.Exists(configFile))
                    continue;

                scannedCount++;
                string content = File.ReadAllText(configFile);
                string relativePath = "Assets/" + MODULES_RELATIVE + "/" + dirName + "/" + dirName + CONFIG_SUFFIX;

                ExtractFields(content, dirName, relativePath, allFields);
            }

            CheckDependencyPairConflicts(graph, allFields, report);

            return scannedCount;
        }

        void ExtractFields(string content, string moduleName, string filePath, List<ConfigField> output)
        {
            MatchCollection serializedMatches = REGEX_SERIALIZED_FIELD.Matches(content);
            for (int i = 0; i < serializedMatches.Count; i++)
            {
                output.Add(new ConfigField
                {
                    ModuleName = moduleName,
                    FieldName = serializedMatches[i].Groups[1].Value,
                    FilePath = filePath
                });
            }

            MatchCollection privateMatches = REGEX_PRIVATE_FIELD.Matches(content);
            for (int i = 0; i < privateMatches.Count; i++)
            {
                string fieldName = privateMatches[i].Groups[1].Value;
                bool alreadyAdded = false;
                for (int j = 0; j < output.Count; j++)
                {
                    if (output[j].ModuleName == moduleName && output[j].FieldName == fieldName)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                if (!alreadyAdded)
                {
                    output.Add(new ConfigField
                    {
                        ModuleName = moduleName,
                        FieldName = fieldName,
                        FilePath = privateMatches[i].Groups[1].Value
                    });
                }
            }
        }

        void CheckDependencyPairConflicts(
            DependencyGraphBuilder.DependencyGraph graph,
            List<ConfigField> allFields,
            ValidationReport report)
        {
            for (int i = 0; i < graph.Modules.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule module = graph.Modules[i];
                if (module.Dependencies == null) continue;

                for (int d = 0; d < module.Dependencies.Length; d++)
                {
                    string dep = module.Dependencies[d];
                    if (dep == "UnityEngine" || dep == "System")
                        continue;

                    CheckFieldConflicts(module.Name, dep, allFields, report);
                }
            }
        }

        void CheckFieldConflicts(
            string moduleA, string moduleB,
            List<ConfigField> allFields,
            ValidationReport report)
        {
            for (int a = 0; a < allFields.Count; a++)
            {
                if (allFields[a].ModuleName != moduleA) continue;

                for (int b = 0; b < allFields.Count; b++)
                {
                    if (allFields[b].ModuleName != moduleB) continue;

                    float similarity = ComputeFieldSimilarity(
                        allFields[a].FieldName,
                        allFields[b].FieldName);

                    if (similarity >= SIMILARITY_THRESHOLD)
                    {
                        string preferred = moduleB;
                        report.AddWarning(VALIDATOR_NAME,
                            "Config conflict: duplicate source-of-truth detected. "
                            + moduleA + "Config." + allFields[a].FieldName
                            + " vs " + moduleB + "Config." + allFields[b].FieldName
                            + " (similarity: " + similarity.ToString("F2") + "). "
                            + "Preferred source: " + preferred + "Config",
                            allFields[a].FilePath);
                    }
                }
            }
        }

        static float ComputeFieldSimilarity(string fieldA, string fieldB)
        {
            string normalizedA = NormalizeFieldName(fieldA);
            string normalizedB = NormalizeFieldName(fieldB);

            if (normalizedA == normalizedB)
                return 1.0f;

            if (normalizedA.Contains(normalizedB) || normalizedB.Contains(normalizedA))
                return 0.8f;

            string[] wordsA = SplitCamelCase(normalizedA);
            string[] wordsB = SplitCamelCase(normalizedB);

            int commonCount = 0;
            for (int i = 0; i < wordsA.Length; i++)
            {
                if (wordsA[i].Length < MIN_FIELD_NAME_LENGTH) continue;
                for (int j = 0; j < wordsB.Length; j++)
                {
                    if (wordsA[i] == wordsB[j])
                    {
                        commonCount++;
                        break;
                    }
                }
            }

            int maxWords = wordsA.Length > wordsB.Length ? wordsA.Length : wordsB.Length;
            if (maxWords == 0) return 0f;

            return (float)commonCount / maxWords;
        }

        static string NormalizeFieldName(string fieldName)
        {
            if (fieldName.StartsWith("_"))
                fieldName = fieldName.Substring(1);
            return fieldName.ToLower();
        }

        static string[] SplitCamelCase(string input)
        {
            List<string> words = new List<string>();
            int start = 0;
            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    words.Add(input.Substring(start, i - start).ToLower());
                    start = i;
                }
            }
            words.Add(input.Substring(start).ToLower());
            return words.ToArray();
        }
    }
}
