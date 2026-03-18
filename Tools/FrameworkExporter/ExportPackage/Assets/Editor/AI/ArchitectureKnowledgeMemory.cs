using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class ArchitectureKnowledgeMemory
    {
        const string MEMORY_DIR = "docs/ai/architecture_memory";
        const string PATTERNS_FILE = "ARCHITECTURE_PATTERNS.yaml";
        const string ANTI_PATTERNS_FILE = "ANTI_PATTERNS.yaml";
        const string EVOLUTION_LOG = "MODULE_EVOLUTION_LOG.md";
        const string LOG_PREFIX = "[ArchKnowledge] ";

        static readonly Regex REGEX_PATTERN_ID = new Regex(@"^\s*-\s*id:\s*(.+)");
        static readonly Regex REGEX_PATTERN_NAME = new Regex(@"^\s*name:\s*(.+)");
        static readonly Regex REGEX_PATTERN_CATEGORY = new Regex(@"^\s*category:\s*(.+)");
        static readonly Regex REGEX_DETECTION_REGEX = new Regex(@"^\s*detection_regex:\s*""(.+)""");
        static readonly Regex REGEX_AUTO_BLOCK = new Regex(@"^\s*auto_block:\s*(true|false)");
        static readonly Regex REGEX_SEVERITY = new Regex(@"^\s*severity:\s*(.+)");
        static readonly Regex REGEX_REINFORCE = new Regex(@"^\s*reinforcement_count:\s*(\d+)");

        public struct KnownPattern
        {
            public string Id;
            public string Name;
            public string Category;
            public string ValidatorReference;
        }

        public struct AntiPattern
        {
            public string Id;
            public string Name;
            public string Category;
            public string Severity;
            public string DetectionRegex;
            public bool AutoBlock;
            public int OccurrenceCount;
        }

        public static KnownPattern[] LoadPatterns()
        {
            string path = Path.Combine(Application.dataPath, "..", MEMORY_DIR, PATTERNS_FILE);
            if (!File.Exists(path)) return new KnownPattern[0];

            string[] lines = File.ReadAllLines(path);
            List<KnownPattern> result = new List<KnownPattern>();
            KnownPattern current = new KnownPattern();
            bool inEntry = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match m = REGEX_PATTERN_ID.Match(lines[i]);
                if (m.Success)
                {
                    if (inEntry) result.Add(current);
                    current = new KnownPattern { Id = m.Groups[1].Value.Trim() };
                    inEntry = true;
                    continue;
                }

                if (!inEntry) continue;

                m = REGEX_PATTERN_NAME.Match(lines[i]);
                if (m.Success) { current.Name = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_PATTERN_CATEGORY.Match(lines[i]);
                if (m.Success) { current.Category = m.Groups[1].Value.Trim(); continue; }

                if (lines[i].TrimStart().StartsWith("validator_reference:"))
                {
                    current.ValidatorReference = lines[i].Substring(lines[i].IndexOf(':') + 1).Trim();
                }
            }

            if (inEntry) result.Add(current);
            return result.ToArray();
        }

        public static AntiPattern[] LoadAntiPatterns()
        {
            string path = Path.Combine(Application.dataPath, "..", MEMORY_DIR, ANTI_PATTERNS_FILE);
            if (!File.Exists(path)) return new AntiPattern[0];

            string[] lines = File.ReadAllLines(path);
            List<AntiPattern> result = new List<AntiPattern>();
            AntiPattern current = new AntiPattern();
            bool inEntry = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match m = REGEX_PATTERN_ID.Match(lines[i]);
                if (m.Success)
                {
                    if (inEntry) result.Add(current);
                    current = new AntiPattern { Id = m.Groups[1].Value.Trim() };
                    inEntry = true;
                    continue;
                }

                if (!inEntry) continue;

                m = REGEX_PATTERN_NAME.Match(lines[i]);
                if (m.Success) { current.Name = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_PATTERN_CATEGORY.Match(lines[i]);
                if (m.Success) { current.Category = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_SEVERITY.Match(lines[i]);
                if (m.Success) { current.Severity = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_DETECTION_REGEX.Match(lines[i]);
                if (m.Success) { current.DetectionRegex = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_AUTO_BLOCK.Match(lines[i]);
                if (m.Success) { current.AutoBlock = m.Groups[1].Value == "true"; continue; }
            }

            if (inEntry) result.Add(current);
            return result.ToArray();
        }

        public static int ScanCodeForAntiPatterns(string filePath, AntiPattern[] antiPatterns, List<string> violations)
        {
            if (!File.Exists(filePath)) return 0;

            string content = File.ReadAllText(filePath);
            int violationCount = 0;

            for (int i = 0; i < antiPatterns.Length; i++)
            {
                if (string.IsNullOrEmpty(antiPatterns[i].DetectionRegex)) continue;

                Regex rx = new Regex(antiPatterns[i].DetectionRegex);
                if (rx.IsMatch(content))
                {
                    violations.Add("[" + antiPatterns[i].Severity.ToUpper() + "] "
                        + antiPatterns[i].Name + " detected in " + filePath
                        + " — " + antiPatterns[i].Id);
                    violationCount++;
                }
            }

            return violationCount;
        }

        public static void AppendEvolutionEntry(
            string moduleName, string changeType, string change,
            string reason, string affected, string risk, string decider)
        {
            string path = Path.Combine(Application.dataPath, "..", MEMORY_DIR, EVOLUTION_LOG);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("### [" + DateTime.UtcNow.ToString("yyyy-MM-dd") + "] "
                + moduleName + " — " + changeType);
            sb.AppendLine();
            sb.AppendLine("- **변경**: " + change);
            sb.AppendLine("- **사유**: " + reason);
            sb.AppendLine("- **영향**: " + affected);
            sb.AppendLine("- **위험**: " + risk);
            sb.AppendLine("- **결정자**: " + decider);

            string marker = "*이 로그는 Committer가";
            string existing = File.ReadAllText(path);
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

            Debug.Log(LOG_PREFIX + "Evolution entry added: " + moduleName + " — " + changeType);
        }

        [UnityEditor.MenuItem("Tools/AI/Show Architecture Knowledge Summary")]
        static void ShowSummary()
        {
            KnownPattern[] patterns = LoadPatterns();
            AntiPattern[] antiPatterns = LoadAntiPatterns();

            Debug.Log(LOG_PREFIX + "Known Patterns: " + patterns.Length);
            for (int i = 0; i < patterns.Length; i++)
                Debug.Log("  [" + patterns[i].Id + "] " + patterns[i].Name + " (" + patterns[i].Category + ")");

            Debug.Log(LOG_PREFIX + "Anti-Patterns: " + antiPatterns.Length);
            for (int i = 0; i < antiPatterns.Length; i++)
                Debug.Log("  [" + antiPatterns[i].Id + "] " + antiPatterns[i].Name
                    + " (" + antiPatterns[i].Severity + ", autoBlock=" + antiPatterns[i].AutoBlock + ")");
        }
    }
}
