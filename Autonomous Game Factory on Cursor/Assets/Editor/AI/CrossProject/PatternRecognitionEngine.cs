using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Editor.AI.CrossProject
{
    public static class PatternRecognitionEngine
    {
        const string LOG_PREFIX = "[PatternRecog] ";
        const float MATCH_THRESHOLD = 0.25f;

        struct ArchitectureTemplate
        {
            public string PatternName;
            public string Category;
            public string[] Keywords;
            public string[] TypicalModules;
            public string Description;
            public string Complexity;
        }

        static readonly ArchitectureTemplate[] KNOWN_TEMPLATES = new ArchitectureTemplate[]
        {
            new ArchitectureTemplate
            {
                PatternName = "InventoryPattern",
                Category = "Inventory",
                Keywords = new[] { "inventory", "item", "slot", "stack", "equip", "bag", "인벤토리", "아이템", "장비" },
                TypicalModules = new[] { "InventorySystem", "ItemStacking", "EquipmentSlot", "ItemDatabase" },
                Description = "슬롯 기반 인벤토리 + 아이템 스택 + 장비 시스템",
                Complexity = "medium"
            },
            new ArchitectureTemplate
            {
                PatternName = "CurrencyPattern",
                Category = "Economy",
                Keywords = new[] { "currency", "gold", "gem", "coin", "wallet", "economy", "재화", "골드" },
                TypicalModules = new[] { "CurrencyWallet", "ShopSystem", "PriceCalculator" },
                Description = "다중 재화 관리 + 상점 + 가격 계산",
                Complexity = "low"
            },
            new ArchitectureTemplate
            {
                PatternName = "BuffSystemPattern",
                Category = "Combat",
                Keywords = new[] { "buff", "debuff", "status", "effect", "duration", "modifier", "버프" },
                TypicalModules = new[] { "StatusEffect", "BuffSystem", "BuffIconUI" },
                Description = "시간 기반 버프/디버프 + 스탯 수정 + UI 표시",
                Complexity = "medium"
            },
            new ArchitectureTemplate
            {
                PatternName = "CropGrowthPattern",
                Category = "Farming",
                Keywords = new[] { "crop", "farm", "grow", "harvest", "seed", "plant", "water", "작물", "농사", "수확" },
                TypicalModules = new[] { "CropGrowth", "FarmPlot", "HarvestReward", "SeedInventory" },
                Description = "작물 성장 시뮬레이션 + 밭 관리 + 수확 보상",
                Complexity = "medium"
            },
            new ArchitectureTemplate
            {
                PatternName = "SkillTreePattern",
                Category = "Progression",
                Keywords = new[] { "skill", "tree", "unlock", "talent", "node", "branch", "스킬", "트리" },
                TypicalModules = new[] { "SkillTree", "SkillNode", "SkillUnlocker", "SkillEffect" },
                Description = "트리 구조 스킬 해금 + 노드 의존성 + 효과 적용",
                Complexity = "high"
            },
            new ArchitectureTemplate
            {
                PatternName = "CombatCorePattern",
                Category = "Combat",
                Keywords = new[] { "combat", "attack", "damage", "health", "defense", "전투", "공격", "데미지" },
                TypicalModules = new[] { "HealthSystem", "DamageCalculator", "CombatCore", "AttackSequencer" },
                Description = "체력 관리 + 데미지 계산 + 전투 시퀀스",
                Complexity = "high"
            },
            new ArchitectureTemplate
            {
                PatternName = "QuestPattern",
                Category = "Quest",
                Keywords = new[] { "quest", "mission", "objective", "reward", "complete", "퀘스트", "미션", "보상" },
                TypicalModules = new[] { "QuestTracker", "ObjectiveEvaluator", "RewardDistributor" },
                Description = "퀘스트 추적 + 목표 평가 + 보상 분배",
                Complexity = "medium"
            },
            new ArchitectureTemplate
            {
                PatternName = "SaveLoadPattern",
                Category = "Save",
                Keywords = new[] { "save", "load", "persist", "serialize", "data", "저장", "세이브" },
                TypicalModules = new[] { "SaveSystem", "DataSerializer", "CloudSync" },
                Description = "로컬 저장 + 데이터 직렬화 + 클라우드 동기화",
                Complexity = "medium"
            }
        };

        public struct PatternMatch
        {
            public string PatternName;
            public string Category;
            public float Score;
            public string[] SuggestedModules;
            public string Description;
            public string Complexity;
            public string[] MatchedKeywords;
        }

        public struct RecognitionResult
        {
            public string Query;
            public PatternMatch[] Matches;
            public int TotalPatterns;
        }

        public static RecognitionResult Recognize(string description)
        {
            string descLower = description.ToLower();
            string[] descWords = descLower.Split(' ', ',', '.', '/', '(', ')', '\n', '\r', ':', ';', '-', '→');

            List<PatternMatch> matches = new List<PatternMatch>();

            for (int t = 0; t < KNOWN_TEMPLATES.Length; t++)
            {
                ArchitectureTemplate tpl = KNOWN_TEMPLATES[t];
                List<string> matched = new List<string>();
                int hitCount = 0;

                for (int k = 0; k < tpl.Keywords.Length; k++)
                {
                    for (int w = 0; w < descWords.Length; w++)
                    {
                        if (descWords[w].Length < 2) continue;
                        if (descWords[w] == tpl.Keywords[k] || descWords[w].Contains(tpl.Keywords[k]))
                        {
                            hitCount++;
                            if (!matched.Contains(tpl.Keywords[k]))
                                matched.Add(tpl.Keywords[k]);
                            break;
                        }
                    }
                }

                float score = tpl.Keywords.Length > 0 ? (float)hitCount / tpl.Keywords.Length : 0f;
                if (score < MATCH_THRESHOLD) continue;

                GlobalModuleLibrary.GlobalModule[] catalog = GlobalModuleLibrary.LoadCatalog();
                List<string> suggestedModules = new List<string>();
                for (int m = 0; m < tpl.TypicalModules.Length; m++)
                {
                    bool inCatalog = false;
                    for (int c = 0; c < catalog.Length; c++)
                    {
                        if (catalog[c].Name == tpl.TypicalModules[m])
                        {
                            inCatalog = true;
                            break;
                        }
                    }
                    suggestedModules.Add(tpl.TypicalModules[m] + (inCatalog ? " [GLOBAL]" : " [NEW]"));
                }

                matches.Add(new PatternMatch
                {
                    PatternName = tpl.PatternName,
                    Category = tpl.Category,
                    Score = score,
                    SuggestedModules = suggestedModules.ToArray(),
                    Description = tpl.Description,
                    Complexity = tpl.Complexity,
                    MatchedKeywords = matched.ToArray()
                });
            }

            for (int i = 0; i < matches.Count - 1; i++)
            {
                for (int j = i + 1; j < matches.Count; j++)
                {
                    if (matches[j].Score > matches[i].Score)
                    {
                        PatternMatch tmp = matches[i];
                        matches[i] = matches[j];
                        matches[j] = tmp;
                    }
                }
            }

            return new RecognitionResult
            {
                Query = description,
                Matches = matches.ToArray(),
                TotalPatterns = KNOWN_TEMPLATES.Length
            };
        }

        public static string FormatResult(RecognitionResult result)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Pattern Recognition Results");
            sb.AppendLine();
            sb.AppendLine("Query: " + result.Query);
            sb.AppendLine("Matched: " + result.Matches.Length + " / " + result.TotalPatterns + " patterns");
            sb.AppendLine();

            for (int i = 0; i < result.Matches.Length; i++)
            {
                PatternMatch m = result.Matches[i];
                sb.AppendLine("## " + (i + 1) + ". " + m.PatternName + " [" + m.Category + "]");
                sb.AppendLine("- Score: " + m.Score.ToString("F2"));
                sb.AppendLine("- Complexity: " + m.Complexity);
                sb.AppendLine("- Description: " + m.Description);
                sb.Append("- Keywords: ");
                for (int k = 0; k < m.MatchedKeywords.Length; k++)
                {
                    if (k > 0) sb.Append(", ");
                    sb.Append(m.MatchedKeywords[k]);
                }
                sb.AppendLine();
                sb.AppendLine("- Suggested Modules:");
                for (int s = 0; s < m.SuggestedModules.Length; s++)
                    sb.AppendLine("  - " + m.SuggestedModules[s]);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        [UnityEditor.MenuItem("Tools/AI/CPIL/Analyze Cross-Project Patterns (Test — Farm Game)")]
        static void TestFarmGame()
        {
            string desc = "농사 시뮬레이션 게임. 작물을 심고 키우고 수확. "
                + "재화로 씨앗 구매. 인벤토리에 수확물 저장. 레벨업으로 새 작물 해금.";
            RecognitionResult result = Recognize(desc);
            Debug.Log(FormatResult(result));
        }

        [UnityEditor.MenuItem("Tools/AI/CPIL/Analyze Cross-Project Patterns (Test — RPG)")]
        static void TestRPG()
        {
            string desc = "RPG 전투 게임. 스킬 트리로 능력 해금. "
                + "적과 전투하여 데미지 계산. 퀘스트 완료 시 보상.";
            RecognitionResult result = Recognize(desc);
            Debug.Log(FormatResult(result));
        }
    }
}
