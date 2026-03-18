using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class LearningRecorderWriter
    {
        const string LOG_PREFIX = "[LearningRecorder] ";
        const string LEARNING_RELATIVE = "docs/ai/learning";
        const string LEARNING_LOG_FILE = "LEARNING_LOG.md";
        const string HUMAN_FIX_FILE = "HUMAN_FIX_EXAMPLES.md";
        const string VALIDATOR_PATTERNS_FILE = "VALIDATOR_FAILURE_PATTERNS.md";
        const string RULE_MEMORY_FILE = "RULE_MEMORY.yaml";
        const string RECURRING_MISTAKES_FILE = "RECURRING_MISTAKES.md";

        public struct HumanFix
        {
            public string File;
            public string Change;
            public string Rationale;
            public string Timestamp;
        }

        public struct ValidatorFailure
        {
            public string ValidatorName;
            public string Message;
            public string FilePath;
        }

        public struct LearningEntry
        {
            public string Module;
            public string FeatureGroup;
            public HumanFix[] HumanFixes;
            public ValidatorFailure[] ValidatorFailures;
            public int RetryCount;
        }

        public struct RecordResult
        {
            public int LogEntriesAdded;
            public int HumanFixExamplesAdded;
            public int ValidatorPatternsAdded;
            public bool Success;
            public string Error;
        }

        public static RecordResult RecordLearningData(LearningEntry entry)
        {
            RecordResult result = new RecordResult();

            string learningDir = GetLearningDirectory();
            if (!Directory.Exists(learningDir))
            {
                Directory.CreateDirectory(learningDir);
                Debug.Log(LOG_PREFIX + "Created learning directory: " + learningDir);
            }

            int nextLogId = GetNextLogId(learningDir);

            if (entry.ValidatorFailures != null)
            {
                for (int i = 0; i < entry.ValidatorFailures.Length; i++)
                {
                    string logId = "LL-" + nextLogId.ToString("D4");
                    nextLogId++;
                    AppendLearningLogEntry(learningDir, logId, entry.Module, entry.FeatureGroup,
                        "validator_failure", entry.ValidatorFailures[i].ValidatorName,
                        entry.ValidatorFailures[i].Message, entry.ValidatorFailures[i].FilePath);
                    result.LogEntriesAdded++;

                    AppendValidatorPattern(learningDir, entry.ValidatorFailures[i]);
                    result.ValidatorPatternsAdded++;
                }
            }

            if (entry.HumanFixes != null)
            {
                for (int i = 0; i < entry.HumanFixes.Length; i++)
                {
                    string logId = "LL-" + nextLogId.ToString("D4");
                    nextLogId++;
                    AppendLearningLogEntry(learningDir, logId, entry.Module, entry.FeatureGroup,
                        "human_fix", "human", entry.HumanFixes[i].Change,
                        entry.HumanFixes[i].File);
                    result.LogEntriesAdded++;

                    AppendHumanFixExample(learningDir, entry.Module, entry.HumanFixes[i]);
                    result.HumanFixExamplesAdded++;
                }
            }

            result.Success = true;
            Debug.Log(LOG_PREFIX + "Recorded learning for " + entry.Module
                + ": logs=" + result.LogEntriesAdded
                + " fixes=" + result.HumanFixExamplesAdded
                + " patterns=" + result.ValidatorPatternsAdded);

            return result;
        }

        public static ValidatorFailure[] ExtractFailuresFromReport(string reportPath)
        {
            if (!File.Exists(reportPath))
            {
                Debug.LogWarning(LOG_PREFIX + "Report not found: " + reportPath);
                return new ValidatorFailure[0];
            }

            string json = File.ReadAllText(reportPath);
            var failures = new List<ValidatorFailure>();

            int searchFrom = 0;
            while (true)
            {
                int vIdx = json.IndexOf("\"ValidatorName\"", searchFrom);
                if (vIdx < 0) break;

                int sevIdx = json.IndexOf("\"Severity\"", vIdx);
                if (sevIdx < 0) break;

                string sevBlock = json.Substring(sevIdx, Mathf.Min(60, json.Length - sevIdx));
                if (!sevBlock.Contains("Error"))
                {
                    searchFrom = sevIdx + 10;
                    continue;
                }

                ValidatorFailure f = new ValidatorFailure();
                f.ValidatorName = ExtractJsonStringValue(json, "ValidatorName", vIdx);
                f.Message = ExtractJsonStringValue(json, "Message", vIdx);
                f.FilePath = ExtractJsonStringValue(json, "Path", vIdx);
                failures.Add(f);

                searchFrom = sevIdx + 10;
            }

            return failures.ToArray();
        }

        public static HumanFix[] ParseHumanFixesFromYaml(string[] lines, int startLine)
        {
            var fixes = new List<HumanFix>();
            HumanFix current = new HumanFix();
            bool inFixes = false;
            bool hasData = false;

            for (int i = startLine; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("human_fixes:"))
                {
                    inFixes = true;
                    continue;
                }

                if (!inFixes) continue;

                if (line.Length > 0 && !line.StartsWith("-") && !line.StartsWith("file:")
                    && !line.StartsWith("change:") && !line.StartsWith("rationale:")
                    && !line.StartsWith("timestamp:"))
                {
                    if (hasData) fixes.Add(current);
                    break;
                }

                if (line.StartsWith("- file:"))
                {
                    if (hasData) fixes.Add(current);
                    current = new HumanFix();
                    hasData = true;
                    current.File = ExtractYamlValue(line, "file");
                }
                else if (line.StartsWith("file:"))
                {
                    current.File = ExtractYamlValue(line, "file");
                }
                else if (line.StartsWith("change:"))
                {
                    current.Change = ExtractYamlValue(line, "change");
                }
                else if (line.StartsWith("rationale:"))
                {
                    current.Rationale = ExtractYamlValue(line, "rationale");
                }
                else if (line.StartsWith("timestamp:"))
                {
                    current.Timestamp = ExtractYamlValue(line, "timestamp");
                }
            }

            if (hasData) fixes.Add(current);
            return fixes.ToArray();
        }

        static void AppendLearningLogEntry(string dir, string id, string module,
            string featureGroup, string eventType, string source, string description, string filePath)
        {
            string path = Path.Combine(dir, LEARNING_LOG_FILE);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("- id: " + id);
            sb.AppendLine("  date: \"" + DateTime.UtcNow.ToString("yyyy-MM-dd") + "\"");
            sb.AppendLine("  module: " + module);
            sb.AppendLine("  feature_group: " + featureGroup);
            sb.AppendLine("  event_type: " + eventType);
            sb.AppendLine("  source: " + source);
            sb.AppendLine("  description: \"" + EscapeYaml(description) + "\"");
            if (!string.IsNullOrEmpty(filePath))
                sb.AppendLine("  files_affected:\n    - " + filePath);

            File.AppendAllText(path, sb.ToString());
            Debug.Log(LOG_PREFIX + "LEARNING_LOG: " + id + " (" + eventType + ") for " + module);
        }

        static void AppendHumanFixExample(string dir, string module, HumanFix fix)
        {
            string path = Path.Combine(dir, HUMAN_FIX_FILE);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("### " + module + " — " + fix.File);
            sb.AppendLine();
            sb.AppendLine("**Change:** " + fix.Change);
            sb.AppendLine();
            sb.AppendLine("**Rationale:** " + fix.Rationale);
            sb.AppendLine();
            sb.AppendLine("**Timestamp:** " + (string.IsNullOrEmpty(fix.Timestamp)
                ? DateTime.UtcNow.ToString("o") : fix.Timestamp));

            File.AppendAllText(path, sb.ToString());
            Debug.Log(LOG_PREFIX + "HUMAN_FIX_EXAMPLES: " + module + "/" + fix.File);
        }

        static void AppendValidatorPattern(string dir, ValidatorFailure failure)
        {
            string path = Path.Combine(dir, VALIDATOR_PATTERNS_FILE);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("| ").Append(failure.ValidatorName);
            sb.Append(" | ").Append(EscapeTable(failure.Message));
            sb.Append(" | ").Append(string.IsNullOrEmpty(failure.FilePath) ? "-" : failure.FilePath);
            sb.Append(" | ").Append(DateTime.UtcNow.ToString("yyyy-MM-dd"));
            sb.AppendLine(" |");

            File.AppendAllText(path, sb.ToString());
            Debug.Log(LOG_PREFIX + "VALIDATOR_PATTERNS: " + failure.ValidatorName + " — " + failure.Message);
        }

        static int GetNextLogId(string dir)
        {
            string path = Path.Combine(dir, LEARNING_LOG_FILE);
            if (!File.Exists(path)) return 1;

            string content = File.ReadAllText(path);
            int maxId = 0;
            int searchFrom = 0;

            while (true)
            {
                int idx = content.IndexOf("LL-", searchFrom);
                if (idx < 0) break;

                int numStart = idx + 3;
                int numEnd = numStart;
                while (numEnd < content.Length && numEnd < numStart + 4
                    && content[numEnd] >= '0' && content[numEnd] <= '9')
                    numEnd++;

                if (numEnd > numStart)
                {
                    int val;
                    if (int.TryParse(content.Substring(numStart, numEnd - numStart), out val))
                    {
                        if (val > maxId) maxId = val;
                    }
                }

                searchFrom = numEnd;
            }

            return maxId + 1;
        }

        static string GetLearningDirectory()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, LEARNING_RELATIVE);
        }

        static string EscapeYaml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("\"", "\\\"").Replace("\n", " ");
        }

        static string EscapeTable(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("|", "\\|").Replace("\n", " ");
        }

        static string ExtractJsonStringValue(string json, string key, int searchFrom)
        {
            string needle = "\"" + key + "\"";
            int keyIdx = json.IndexOf(needle, searchFrom);
            if (keyIdx < 0) return "";

            int colonIdx = json.IndexOf(':', keyIdx + needle.Length);
            if (colonIdx < 0) return "";

            int quoteStart = json.IndexOf('"', colonIdx + 1);
            if (quoteStart < 0) return "";

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) return "";

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        static string ExtractYamlValue(string line, string key)
        {
            int colonIdx = line.IndexOf(':');
            if (colonIdx < 0) return "";
            return line.Substring(colonIdx + 1).Trim().Trim('"', '\'');
        }
    }
}
