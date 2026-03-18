using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class ModuleDiscovery
    {
        const float DISCOVERY_THRESHOLD = 0.4f;
        const float HIGH_SIMILARITY = 0.7f;
        const float EXACT_MATCH = 0.9f;
        const float NAME_WEIGHT = 0.25f;
        const float INTERFACE_WEIGHT = 0.30f;
        const float RESPONSIBILITY_WEIGHT = 0.30f;
        const float DEPENDENCY_WEIGHT = 0.15f;
        const string MODULES_ROOT = "Assets/Game/Modules";

        static readonly Regex REGEX_METHOD = new Regex(@"^\s*(?:void|bool|int|float|string|E\w+)\s+(\w+)\s*\(");
        static readonly Regex REGEX_PROPERTY = new Regex(@"^\s*(?:bool|int|float|string)\s+(\w+)\s*\{");
        static readonly Regex REGEX_EVENT = new Regex(@"^\s*event\s+\w+(?:<[^>]+>)?\s+(\w+)");

        public struct CandidateModule
        {
            public string ModuleName;
            public float SimilarityScore;
            public string ReasonForSimilarity;
            public string PotentialReuseLevel;
            public float NameScore;
            public float InterfaceScore;
            public float ResponsibilityScore;
            public float DependencyScore;
        }

        public struct DiscoveryResult
        {
            public string Query;
            public int CandidateCount;
            public CandidateModule[] Candidates;
        }

        public struct InterfaceSignature
        {
            public string ModuleName;
            public string[] Methods;
            public string[] Properties;
            public string[] Events;
        }

        public static DiscoveryResult RunDiscovery(string featureQuery, string[] featureKeywords)
        {
            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();
            List<CandidateModule> candidates = new List<CandidateModule>();
            string queryLower = featureQuery.ToLower();

            for (int i = 0; i < graph.ModuleMap.Count; i++)
            {
                string moduleName = graph.ModuleMap[i].Name;
                if (moduleName == "Template")
                    continue;

                float nameScore = ComputeNameSimilarity(featureKeywords, moduleName);
                float interfaceScore = ComputeInterfaceSimilarity(featureKeywords, moduleName);
                float responsibilityScore = ComputeResponsibilitySimilarity(queryLower, graph.ModuleMap[i]);
                float dependencyScore = ComputeDependencySimilarity(featureKeywords, graph.ModuleMap[i]);

                float totalScore = nameScore * NAME_WEIGHT
                    + interfaceScore * INTERFACE_WEIGHT
                    + responsibilityScore * RESPONSIBILITY_WEIGHT
                    + dependencyScore * DEPENDENCY_WEIGHT;

                if (totalScore < DISCOVERY_THRESHOLD)
                    continue;

                string reuseLevel = DetermineReuseLevel(totalScore);
                string reason = BuildReason(moduleName, nameScore, interfaceScore, responsibilityScore);

                CandidateModule candidate = new CandidateModule
                {
                    ModuleName = moduleName,
                    SimilarityScore = totalScore,
                    ReasonForSimilarity = reason,
                    PotentialReuseLevel = reuseLevel,
                    NameScore = nameScore,
                    InterfaceScore = interfaceScore,
                    ResponsibilityScore = responsibilityScore,
                    DependencyScore = dependencyScore
                };
                candidates.Add(candidate);
            }

            candidates.Sort(CompareBySimilarityDescending);

            DiscoveryResult result = new DiscoveryResult
            {
                Query = featureQuery,
                CandidateCount = candidates.Count,
                Candidates = candidates.ToArray()
            };

            return result;
        }

        static int CompareBySimilarityDescending(CandidateModule a, CandidateModule b)
        {
            if (a.SimilarityScore > b.SimilarityScore) return -1;
            if (a.SimilarityScore < b.SimilarityScore) return 1;
            return 0;
        }

        static float ComputeNameSimilarity(string[] keywords, string moduleName)
        {
            string moduleNameLower = moduleName.ToLower();
            int matchCount = 0;
            for (int i = 0; i < keywords.Length; i++)
            {
                if (moduleNameLower.Contains(keywords[i].ToLower()))
                    matchCount++;
            }
            if (matchCount == 0) return 0f;
            return Mathf.Min(1f, (float)matchCount / keywords.Length);
        }

        static float ComputeInterfaceSimilarity(string[] keywords, string moduleName)
        {
            InterfaceSignature sig = ScanInterface(moduleName);
            if (sig.Methods == null) return 0f;

            int totalMembers = sig.Methods.Length + sig.Properties.Length + sig.Events.Length;
            if (totalMembers == 0) return 0f;

            int matchCount = 0;
            for (int k = 0; k < keywords.Length; k++)
            {
                string kw = keywords[k].ToLower();
                for (int m = 0; m < sig.Methods.Length; m++)
                {
                    if (sig.Methods[m].ToLower().Contains(kw))
                    {
                        matchCount++;
                        break;
                    }
                }
                for (int p = 0; p < sig.Properties.Length; p++)
                {
                    if (sig.Properties[p].ToLower().Contains(kw))
                    {
                        matchCount++;
                        break;
                    }
                }
            }
            return Mathf.Min(1f, (float)matchCount / Mathf.Max(1f, keywords.Length));
        }

        static float ComputeResponsibilitySimilarity(string queryLower, DependencyGraphBuilder.RegistryModule module)
        {
            string descPath = Path.Combine(Application.dataPath, "..", "docs", "ai", "MODULE_REGISTRY.yaml");
            if (!File.Exists(descPath)) return 0f;

            string registryContent = File.ReadAllText(descPath);
            string moduleSection = ExtractModuleDescription(registryContent, module.Name);
            if (string.IsNullOrEmpty(moduleSection)) return 0f;

            string descLower = moduleSection.ToLower();
            string[] queryWords = queryLower.Split(' ', ',', '.', '—', ':', '/', '(', ')');
            int matchCount = 0;
            int validWordCount = 0;
            for (int i = 0; i < queryWords.Length; i++)
            {
                if (queryWords[i].Length < 2) continue;
                validWordCount++;
                if (descLower.Contains(queryWords[i]))
                    matchCount++;
            }
            if (validWordCount == 0) return 0f;
            return Mathf.Min(1f, (float)matchCount / validWordCount);
        }

        static float ComputeDependencySimilarity(string[] keywords, DependencyGraphBuilder.RegistryModule module)
        {
            if (module.Dependencies == null || module.Dependencies.Length == 0)
                return 0f;

            int matchCount = 0;
            for (int i = 0; i < module.Dependencies.Length; i++)
            {
                string dep = module.Dependencies[i].ToLower();
                if (dep == "unityengine" || dep == "system") continue;
                for (int k = 0; k < keywords.Length; k++)
                {
                    if (dep.Contains(keywords[k].ToLower()))
                    {
                        matchCount++;
                        break;
                    }
                }
            }
            return Mathf.Min(1f, (float)matchCount / Mathf.Max(1f, keywords.Length));
        }

        static string DetermineReuseLevel(float score)
        {
            if (score >= EXACT_MATCH) return "full_reuse";
            if (score >= HIGH_SIMILARITY) return "partial_reuse";
            if (score >= 0.55f) return "extend";
            return "reference_only";
        }

        static string BuildReason(string moduleName, float nameScore, float ifaceScore, float respScore)
        {
            string bestMatch = "responsibility";
            float bestScore = respScore;
            if (nameScore > bestScore) { bestMatch = "name"; bestScore = nameScore; }
            if (ifaceScore > bestScore) { bestMatch = "interface"; }

            return moduleName + ": highest match on " + bestMatch;
        }

        public static InterfaceSignature ScanInterface(string moduleName)
        {
            string interfacePath = Path.Combine(MODULES_ROOT, moduleName, "I" + moduleName + ".cs");
            string fullPath = Path.Combine(Application.dataPath, "..", interfacePath);

            InterfaceSignature sig = new InterfaceSignature { ModuleName = moduleName };

            if (!File.Exists(fullPath))
                return sig;

            string[] lines = File.ReadAllLines(fullPath);
            List<string> methods = new List<string>();
            List<string> properties = new List<string>();
            List<string> events = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                Match m = REGEX_METHOD.Match(lines[i]);
                if (m.Success)
                {
                    methods.Add(m.Groups[1].Value);
                    continue;
                }
                Match p = REGEX_PROPERTY.Match(lines[i]);
                if (p.Success)
                {
                    properties.Add(p.Groups[1].Value);
                    continue;
                }
                Match e = REGEX_EVENT.Match(lines[i]);
                if (e.Success)
                {
                    events.Add(e.Groups[1].Value);
                }
            }

            sig.Methods = methods.ToArray();
            sig.Properties = properties.ToArray();
            sig.Events = events.ToArray();
            return sig;
        }

        static string ExtractModuleDescription(string registryContent, string moduleName)
        {
            int nameIdx = registryContent.IndexOf("name: " + moduleName);
            if (nameIdx < 0) return null;

            int descIdx = registryContent.IndexOf("description:", nameIdx);
            if (descIdx < 0) return null;

            int endIdx = registryContent.IndexOf("\n  - name:", descIdx);
            if (endIdx < 0) endIdx = registryContent.Length;

            return registryContent.Substring(descIdx, endIdx - descIdx);
        }

        public static bool IsAboveThreshold(float score)
        {
            return score >= DISCOVERY_THRESHOLD;
        }

        public static bool IsHighSimilarity(float score)
        {
            return score >= HIGH_SIMILARITY;
        }

        public static bool IsExactMatch(float score)
        {
            return score >= EXACT_MATCH;
        }

        [UnityEditor.MenuItem("Tools/AI/Run Module Discovery (Test)")]
        static void RunDiscoveryTest()
        {
            string[] testKeywords = { "inventory", "item", "slot", "stack" };
            DiscoveryResult result = RunDiscovery("아이템 인벤토리 관리 시스템", testKeywords);

            Debug.Log("[Module Discovery] Query: " + result.Query);
            Debug.Log("[Module Discovery] Candidates found: " + result.CandidateCount);

            for (int i = 0; i < result.CandidateCount; i++)
            {
                CandidateModule c = result.Candidates[i];
                Debug.Log("[Module Discovery]   " + c.ModuleName
                    + " | score=" + c.SimilarityScore.ToString("F2")
                    + " | reuse=" + c.PotentialReuseLevel
                    + " | name=" + c.NameScore.ToString("F2")
                    + " | iface=" + c.InterfaceScore.ToString("F2")
                    + " | resp=" + c.ResponsibilityScore.ToString("F2")
                    + " | dep=" + c.DependencyScore.ToString("F2"));
            }
        }
    }
}
