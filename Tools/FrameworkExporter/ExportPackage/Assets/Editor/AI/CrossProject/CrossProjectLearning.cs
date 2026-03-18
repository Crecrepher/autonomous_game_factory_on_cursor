using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI.CrossProject
{
    public static class CrossProjectLearning
    {
        const string GLOBAL_LEARNING_DIR = "docs/ai/global_learning";
        const string GLOBAL_LOG_FILE = "GLOBAL_LEARNING_LOG.md";
        const string GLOBAL_RULES_FILE = "GLOBAL_RULE_MEMORY.yaml";
        const string GLOBAL_FAILURES_FILE = "GLOBAL_FAILURE_PATTERNS.md";
        const string LOG_PREFIX = "[CrossProjectLearn] ";

        static readonly Regex REGEX_RULE_ID = new Regex(@"^\s*-\s*id:\s*(.+)");
        static readonly Regex REGEX_RULE_NAME = new Regex(@"^\s*name:\s*(.+)");
        static readonly Regex REGEX_RULE_SEVERITY = new Regex(@"^\s*severity:\s*(.+)");
        static readonly Regex REGEX_RULE_CATEGORY = new Regex(@"^\s*category:\s*(.+)");
        static readonly Regex REGEX_REINFORCE = new Regex(@"^\s*reinforcement_count:\s*(\d+)");

        public struct GlobalRule
        {
            public string Id;
            public string Name;
            public string Category;
            public string Severity;
            public int ReinforcementCount;
        }

        public static GlobalRule[] LoadGlobalRules()
        {
            string path = Path.Combine(Application.dataPath, "..", GLOBAL_LEARNING_DIR, GLOBAL_RULES_FILE);
            if (!File.Exists(path)) return new GlobalRule[0];

            string[] lines = File.ReadAllLines(path);
            List<GlobalRule> rules = new List<GlobalRule>();
            GlobalRule current = new GlobalRule();
            bool inEntry = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match m = REGEX_RULE_ID.Match(lines[i]);
                if (m.Success)
                {
                    if (inEntry) rules.Add(current);
                    current = new GlobalRule { Id = m.Groups[1].Value.Trim() };
                    inEntry = true;
                    continue;
                }

                if (!inEntry) continue;

                m = REGEX_RULE_NAME.Match(lines[i]);
                if (m.Success) { current.Name = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_RULE_CATEGORY.Match(lines[i]);
                if (m.Success) { current.Category = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_RULE_SEVERITY.Match(lines[i]);
                if (m.Success) { current.Severity = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_REINFORCE.Match(lines[i]);
                if (m.Success)
                {
                    int val;
                    if (int.TryParse(m.Groups[1].Value, out val))
                        current.ReinforcementCount = val;
                }
            }

            if (inEntry) rules.Add(current);
            return rules.ToArray();
        }

        public static void AppendLearningEvent(
            string projectName, string learningType,
            string lesson, string cause, string fix,
            string patternId, string scope)
        {
            string path = Path.Combine(Application.dataPath, "..", GLOBAL_LEARNING_DIR, GLOBAL_LOG_FILE);
            if (!File.Exists(path)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("### [" + DateTime.UtcNow.ToString("yyyy-MM-dd") + "] ["
                + projectName + "] — " + learningType);
            sb.AppendLine();
            sb.AppendLine("- **교훈**: " + lesson);
            sb.AppendLine("- **원인**: " + cause);
            sb.AppendLine("- **해결**: " + fix);
            sb.AppendLine("- **패턴 ID**: " + patternId);
            sb.AppendLine("- **적용 범위**: " + scope);

            string existing = File.ReadAllText(path);
            string marker = "*파이프라인 완료 후";
            int insertIdx = existing.LastIndexOf(marker);
            if (insertIdx > 0)
            {
                string updated = existing.Substring(0, insertIdx) + sb.ToString() + "\n" + existing.Substring(insertIdx);
                File.WriteAllText(path, updated);
            }
            else
            {
                File.AppendAllText(path, sb.ToString());
            }

            Debug.Log(LOG_PREFIX + "Learning event recorded: " + learningType + " — " + lesson);
        }

        public static string GetGlobalRulesSummary()
        {
            GlobalRule[] rules = LoadGlobalRules();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Global Rules — " + rules.Length + " rules");
            for (int i = 0; i < rules.Length; i++)
            {
                sb.AppendLine("- [" + rules[i].Id + "] " + rules[i].Name
                    + " (" + rules[i].Severity + ", reinforced: " + rules[i].ReinforcementCount + ")");
            }
            return sb.ToString();
        }

        [UnityEditor.MenuItem("Tools/AI/CPIL/Show Global Rules")]
        static void ShowRules()
        {
            Debug.Log(GetGlobalRulesSummary());
        }
    }
}
