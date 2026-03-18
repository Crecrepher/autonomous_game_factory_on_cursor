using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class IntelligentDecomposer
    {
        const string LOG_PREFIX = "[SmartDecomposer] ";
        const string MODULE_PATH_PREFIX = "Assets/Game/Modules/";
        const float KEYWORD_MATCH_THRESHOLD = 0.3f;

        struct SystemTemplate
        {
            public string SystemName;
            public string[] Keywords;
            public string[] SubModules;
            public string[][] SubModuleDeps;
            public string[] Descriptions;
        }

        static readonly SystemTemplate[] KNOWN_SYSTEMS = new SystemTemplate[]
        {
            new SystemTemplate
            {
                SystemName = "Economy",
                Keywords = new[] { "economy", "currency", "gold", "gem", "coin", "money", "wallet", "shop", "store", "purchase", "spend", "earn", "재화", "골드", "경제", "상점" },
                SubModules = new[] { "CurrencyWallet", "ShopSystem", "PriceCalculator" },
                SubModuleDeps = new[] { new string[0], new[] { "CurrencyWallet" }, new[] { "CurrencyWallet" } },
                Descriptions = new[] { "재화 추가/소비/조회", "아이템 구매/판매", "가격 계산 및 할인" }
            },
            new SystemTemplate
            {
                SystemName = "Combat",
                Keywords = new[] { "combat", "attack", "damage", "health", "fight", "battle", "weapon", "전투", "공격", "데미지", "체력", "무기" },
                SubModules = new[] { "HealthSystem", "DamageCalculator", "CombatCore" },
                SubModuleDeps = new[] { new string[0], new[] { "HealthSystem" }, new[] { "HealthSystem", "DamageCalculator" } },
                Descriptions = new[] { "체력 관리/회복/감소", "데미지 계산/저항/배율", "전투 시퀀스/타겟팅" }
            },
            new SystemTemplate
            {
                SystemName = "Inventory",
                Keywords = new[] { "inventory", "item", "slot", "stack", "equip", "인벤토리", "아이템", "슬롯", "장비" },
                SubModules = new[] { "InventorySystem", "ItemStacking", "EquipmentSlot" },
                SubModuleDeps = new[] { new string[0], new string[0], new[] { "InventorySystem" } },
                Descriptions = new[] { "슬롯 기반 인벤토리", "아이템 스택 관리", "장비 장착/해제" }
            },
            new SystemTemplate
            {
                SystemName = "Progression",
                Keywords = new[] { "level", "experience", "xp", "upgrade", "progression", "rank", "레벨", "경험치", "승급", "업그레이드" },
                SubModules = new[] { "LevelSystem", "ExperienceTracker", "UpgradeManager" },
                SubModuleDeps = new[] { new string[0], new[] { "LevelSystem" }, new[] { "LevelSystem" } },
                Descriptions = new[] { "레벨/랭크 관리", "경험치 추적/계산", "업그레이드 적용" }
            },
            new SystemTemplate
            {
                SystemName = "Quest",
                Keywords = new[] { "quest", "mission", "objective", "task", "reward", "퀘스트", "미션", "보상", "목표" },
                SubModules = new[] { "QuestTracker", "ObjectiveEvaluator", "RewardDistributor" },
                SubModuleDeps = new[] { new string[0], new[] { "QuestTracker" }, new[] { "QuestTracker" } },
                Descriptions = new[] { "퀘스트 진행/완료 추적", "목표 조건 평가", "보상 분배" }
            },
            new SystemTemplate
            {
                SystemName = "UI",
                Keywords = new[] { "ui", "hud", "menu", "panel", "button", "screen", "popup", "dialog", "화면", "메뉴", "팝업" },
                SubModules = new[] { "UIManager", "ScreenNavigation", "PopupSystem" },
                SubModuleDeps = new[] { new string[0], new[] { "UIManager" }, new[] { "UIManager" } },
                Descriptions = new[] { "UI 요소 관리", "화면 전환/네비게이션", "팝업 생성/표시" }
            },
            new SystemTemplate
            {
                SystemName = "Audio",
                Keywords = new[] { "audio", "sound", "music", "sfx", "bgm", "사운드", "음악", "효과음" },
                SubModules = new[] { "AudioCore", "SFXPlayer", "MusicPlayer" },
                SubModuleDeps = new[] { new string[0], new[] { "AudioCore" }, new[] { "AudioCore" } },
                Descriptions = new[] { "오디오 시스템 코어", "효과음 재생/풀링", "배경음악 재생/전환" }
            },
            new SystemTemplate
            {
                SystemName = "Save",
                Keywords = new[] { "save", "load", "persist", "data", "storage", "저장", "불러오기", "세이브" },
                SubModules = new[] { "SaveSystem", "DataSerializer" },
                SubModuleDeps = new[] { new string[0], new[] { "SaveSystem" } },
                Descriptions = new[] { "저장/불러오기 관리", "데이터 직렬화/역직렬화" }
            }
        };

        public struct DecompositionPlan
        {
            public string OriginalDescription;
            public string InferredFeatureName;
            public string InferredFeatureGroup;
            public SystemMatch[] MatchedSystems;
            public ProposedModule[] ProposedModules;
            public string[] InferredFeatureGroups;
        }

        public struct SystemMatch
        {
            public string SystemName;
            public float MatchScore;
            public string[] MatchedKeywords;
        }

        public struct ProposedModule
        {
            public string Name;
            public string Path;
            public string Description;
            public string ParentSystem;
            public string[] Dependencies;
            public string FeatureGroup;
            public bool AlreadyExists;
            public string ExistingAction;
        }

        public static DecompositionPlan AnalyzeDesignDescription(string description)
        {
            string descLower = description.ToLower();
            string[] descWords = descLower.Split(' ', ',', '.', '/', '(', ')', '\n', '\r', ':', ';', '-', '→');

            List<SystemMatch> matches = new List<SystemMatch>();
            List<ProposedModule> proposedModules = new List<ProposedModule>();

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();

            for (int s = 0; s < KNOWN_SYSTEMS.Length; s++)
            {
                SystemTemplate sys = KNOWN_SYSTEMS[s];
                List<string> matchedWords = new List<string>();
                int hitCount = 0;

                for (int k = 0; k < sys.Keywords.Length; k++)
                {
                    for (int w = 0; w < descWords.Length; w++)
                    {
                        if (descWords[w].Length < 2) continue;
                        if (descWords[w] == sys.Keywords[k] || descWords[w].Contains(sys.Keywords[k]))
                        {
                            hitCount++;
                            if (!matchedWords.Contains(sys.Keywords[k]))
                                matchedWords.Add(sys.Keywords[k]);
                            break;
                        }
                    }
                }

                float score = sys.Keywords.Length > 0 ? (float)hitCount / sys.Keywords.Length : 0f;
                if (score < KEYWORD_MATCH_THRESHOLD) continue;

                matches.Add(new SystemMatch
                {
                    SystemName = sys.SystemName,
                    MatchScore = score,
                    MatchedKeywords = matchedWords.ToArray()
                });

                string featureGroup = sys.SystemName.ToLower();

                for (int m = 0; m < sys.SubModules.Length; m++)
                {
                    string moduleName = sys.SubModules[m];
                    bool exists = graph.ModuleMap.ContainsKey(moduleName);

                    string[] deps = sys.SubModuleDeps[m];
                    string[] fullDeps = new string[deps.Length];
                    for (int d = 0; d < deps.Length; d++)
                        fullDeps[d] = deps[d];

                    proposedModules.Add(new ProposedModule
                    {
                        Name = moduleName,
                        Path = MODULE_PATH_PREFIX + moduleName,
                        Description = sys.Descriptions[m],
                        ParentSystem = sys.SystemName,
                        Dependencies = fullDeps,
                        FeatureGroup = featureGroup,
                        AlreadyExists = exists,
                        ExistingAction = exists ? "reuse_or_extend" : "create_new"
                    });
                }
            }

            string mainSystem = matches.Count > 0 ? matches[0].SystemName : "UnknownSystem";
            float bestScore = 0f;
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].MatchScore > bestScore)
                {
                    bestScore = matches[i].MatchScore;
                    mainSystem = matches[i].SystemName;
                }
            }

            List<string> groups = new List<string>();
            for (int i = 0; i < matches.Count; i++)
            {
                string g = matches[i].SystemName.ToLower();
                if (!groups.Contains(g)) groups.Add(g);
            }

            return new DecompositionPlan
            {
                OriginalDescription = description,
                InferredFeatureName = mainSystem + "Feature",
                InferredFeatureGroup = mainSystem.ToLower(),
                MatchedSystems = matches.ToArray(),
                ProposedModules = proposedModules.ToArray(),
                InferredFeatureGroups = groups.ToArray()
            };
        }

        public static string FormatPlan(DecompositionPlan plan)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Intelligent Feature Decomposition");
            sb.AppendLine();
            sb.AppendLine("## Input");
            sb.AppendLine("```");
            sb.AppendLine(plan.OriginalDescription);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("## Inferred");
            sb.AppendLine("- Feature Name: " + plan.InferredFeatureName);
            sb.AppendLine("- Feature Group: " + plan.InferredFeatureGroup);
            sb.AppendLine("- Matched Systems: " + plan.MatchedSystems.Length);
            sb.AppendLine();

            sb.AppendLine("## System Matches");
            for (int i = 0; i < plan.MatchedSystems.Length; i++)
            {
                SystemMatch m = plan.MatchedSystems[i];
                sb.AppendLine("- **" + m.SystemName + "** (score: " + m.MatchScore.ToString("F2") + ")");
                sb.Append("  Keywords: ");
                for (int k = 0; k < m.MatchedKeywords.Length; k++)
                {
                    if (k > 0) sb.Append(", ");
                    sb.Append(m.MatchedKeywords[k]);
                }
                sb.AppendLine();
            }
            sb.AppendLine();

            sb.AppendLine("## Proposed Module Hierarchy");
            string currentParent = "";
            for (int i = 0; i < plan.ProposedModules.Length; i++)
            {
                ProposedModule mod = plan.ProposedModules[i];
                if (mod.ParentSystem != currentParent)
                {
                    currentParent = mod.ParentSystem;
                    sb.AppendLine();
                    sb.AppendLine("### " + currentParent);
                }

                string status = mod.AlreadyExists ? "[EXISTS — " + mod.ExistingAction + "]" : "[NEW]";
                sb.AppendLine("- " + status + " " + mod.Name);
                sb.AppendLine("  - Description: " + mod.Description);
                if (mod.Dependencies.Length > 0)
                {
                    sb.Append("  - Dependencies: ");
                    for (int d = 0; d < mod.Dependencies.Length; d++)
                    {
                        if (d > 0) sb.Append(", ");
                        sb.Append(mod.Dependencies[d]);
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("  - Feature Group: " + mod.FeatureGroup);
            }

            return sb.ToString();
        }

        [UnityEditor.MenuItem("Tools/AI/Intelligent Decompose (Test — RPG Economy)")]
        static void TestDecomposeEconomy()
        {
            string description =
                "RPG 게임의 경제 시스템. 골드와 젬 두 종류의 재화가 있고, "
                + "상점에서 아이템을 구매/판매할 수 있다. "
                + "인벤토리에 아이템이 저장되고, 장비를 장착할 수 있다.";

            DecompositionPlan plan = AnalyzeDesignDescription(description);
            Debug.Log(FormatPlan(plan));
        }

        [UnityEditor.MenuItem("Tools/AI/Intelligent Decompose (Test — Combat + Progression)")]
        static void TestDecomposeCombat()
        {
            string description =
                "전투 시스템: 공격, 데미지 계산, 체력 관리. "
                + "레벨업 시 스탯 증가. 경험치 획득. "
                + "퀘스트 완료 시 보상 지급.";

            DecompositionPlan plan = AnalyzeDesignDescription(description);
            Debug.Log(FormatPlan(plan));
        }
    }
}
