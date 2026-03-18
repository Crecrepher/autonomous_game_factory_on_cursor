using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class FeatureIntake
    {
        const string FEATURE_QUEUE_RELATIVE = "docs/ai/FEATURE_QUEUE.yaml";
        const string LOG_PREFIX = "[FeatureIntake] ";

        static readonly Regex REGEX_FEATURE_NAME = new Regex(@"^\s*-?\s*name:\s*""?([^""]+)""?");
        static readonly Regex REGEX_FEATURE_DESC = new Regex(@"^\s*description:\s*\|?\s*(.*)");
        static readonly Regex REGEX_FEATURE_PRIORITY = new Regex(@"^\s*priority:\s*(\w+)");
        static readonly Regex REGEX_FEATURE_STATUS = new Regex(@"^\s*status:\s*(\w+)");
        static readonly Regex REGEX_FEATURE_GROUP = new Regex(@"^\s*feature_group:\s*""?([^""]+)""?");

        public struct FeatureEntry
        {
            public string Name;
            public string Description;
            public string Priority;
            public string Status;
            public string FeatureGroup;
            public string[] Modules;
            public string[] Constraints;
            public string[] References;
            public string RequestedBy;
            public string CreatedAt;
        }

        public static string GetFeatureQueuePath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, FEATURE_QUEUE_RELATIVE);
        }

        public static FeatureEntry[] ParseFeatureQueue(string[] lines)
        {
            var features = new List<FeatureEntry>();
            FeatureEntry current = new FeatureEntry();
            bool hasFeature = false;
            bool inDescription = false;
            StringBuilder descBuilder = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.TrimStart().StartsWith("- name:"))
                {
                    if (hasFeature)
                    {
                        if (descBuilder.Length > 0)
                            current.Description = descBuilder.ToString().Trim();
                        features.Add(current);
                    }
                    current = new FeatureEntry();
                    current.Modules = new string[0];
                    current.Constraints = new string[0];
                    current.References = new string[0];
                    hasFeature = true;
                    inDescription = false;
                    descBuilder.Length = 0;

                    Match nameMatch = REGEX_FEATURE_NAME.Match(line);
                    if (nameMatch.Success)
                        current.Name = nameMatch.Groups[1].Value.Trim();
                    continue;
                }

                if (!hasFeature)
                    continue;

                if (inDescription)
                {
                    if (line.TrimStart().StartsWith("priority:") ||
                        line.TrimStart().StartsWith("status:") ||
                        line.TrimStart().StartsWith("feature_group:") ||
                        line.TrimStart().StartsWith("modules:") ||
                        line.TrimStart().StartsWith("constraints:") ||
                        line.TrimStart().StartsWith("requested_by:") ||
                        line.TrimStart().StartsWith("created_at:") ||
                        line.TrimStart().StartsWith("references:") ||
                        line.TrimStart().StartsWith("- name:"))
                    {
                        inDescription = false;
                        current.Description = descBuilder.ToString().Trim();
                    }
                    else
                    {
                        descBuilder.AppendLine(line.Trim());
                        continue;
                    }
                }

                Match descMatch = REGEX_FEATURE_DESC.Match(line);
                if (descMatch.Success && !inDescription)
                {
                    string descVal = descMatch.Groups[1].Value.Trim();
                    if (line.Contains("|"))
                    {
                        inDescription = true;
                        descBuilder.Length = 0;
                    }
                    else if (!string.IsNullOrEmpty(descVal))
                    {
                        current.Description = descVal;
                    }
                    continue;
                }

                Match priorityMatch = REGEX_FEATURE_PRIORITY.Match(line);
                if (priorityMatch.Success)
                {
                    current.Priority = priorityMatch.Groups[1].Value;
                    continue;
                }

                Match statusMatch = REGEX_FEATURE_STATUS.Match(line);
                if (statusMatch.Success)
                {
                    current.Status = statusMatch.Groups[1].Value;
                    continue;
                }

                Match groupMatch = REGEX_FEATURE_GROUP.Match(line);
                if (groupMatch.Success)
                {
                    current.FeatureGroup = groupMatch.Groups[1].Value.Trim();
                    continue;
                }
            }

            if (hasFeature)
            {
                if (descBuilder.Length > 0 && string.IsNullOrEmpty(current.Description))
                    current.Description = descBuilder.ToString().Trim();
                features.Add(current);
            }

            return features.ToArray();
        }

        public static FeatureEntry[] LoadFeatureQueue()
        {
            string path = GetFeatureQueuePath();
            if (!File.Exists(path))
            {
                Debug.LogWarning(LOG_PREFIX + "FEATURE_QUEUE.yaml not found: " + path);
                return new FeatureEntry[0];
            }
            return ParseFeatureQueue(File.ReadAllLines(path));
        }

        public static FeatureEntry CreateFeatureEntry(
            string name,
            string description,
            string priority,
            string featureGroup,
            string[] constraints,
            string[] references)
        {
            FeatureEntry entry = new FeatureEntry();
            entry.Name = name;
            entry.Description = description;
            entry.Priority = string.IsNullOrEmpty(priority) ? "medium" : priority;
            entry.Status = "intake";
            entry.FeatureGroup = featureGroup;
            entry.Modules = new string[0];
            entry.Constraints = constraints != null ? constraints : new string[0];
            entry.References = references != null ? references : new string[0];
            entry.RequestedBy = "system";
            entry.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd");
            return entry;
        }

        public static void AppendFeatureToQueue(FeatureEntry entry)
        {
            string path = GetFeatureQueuePath();
            StringBuilder sb = new StringBuilder();

            string existing = "";
            if (File.Exists(path))
                existing = File.ReadAllText(path);

            if (string.IsNullOrEmpty(existing) || !existing.Contains("features:"))
            {
                sb.AppendLine("version: 1");
                sb.AppendLine("");
                sb.AppendLine("features:");
            }

            sb.AppendLine("");
            sb.AppendLine("  - name: \"" + entry.Name + "\"");

            if (!string.IsNullOrEmpty(entry.Description))
            {
                sb.AppendLine("    description: |");
                string[] descLines = entry.Description.Split('\n');
                for (int i = 0; i < descLines.Length; i++)
                    sb.AppendLine("      " + descLines[i].Trim());
            }

            sb.AppendLine("    priority: " + entry.Priority);
            sb.AppendLine("    status: " + entry.Status);
            sb.AppendLine("    feature_group: \"" + entry.FeatureGroup + "\"");
            sb.AppendLine("    requested_by: \"" + entry.RequestedBy + "\"");
            sb.AppendLine("    created_at: \"" + entry.CreatedAt + "\"");

            if (entry.Modules != null && entry.Modules.Length > 0)
            {
                sb.AppendLine("    modules:");
                for (int i = 0; i < entry.Modules.Length; i++)
                    sb.AppendLine("      - " + entry.Modules[i]);
            }
            else
            {
                sb.AppendLine("    modules: []");
            }

            if (entry.Constraints != null && entry.Constraints.Length > 0)
            {
                sb.AppendLine("    constraints:");
                for (int i = 0; i < entry.Constraints.Length; i++)
                    sb.AppendLine("      - \"" + entry.Constraints[i] + "\"");
            }

            if (entry.References != null && entry.References.Length > 0)
            {
                sb.AppendLine("    references:");
                for (int i = 0; i < entry.References.Length; i++)
                    sb.AppendLine("      - \"" + entry.References[i] + "\"");
            }

            if (existing.Contains("features:"))
            {
                File.WriteAllText(path, existing + sb.ToString());
            }
            else
            {
                File.WriteAllText(path, sb.ToString());
            }

            Debug.Log(LOG_PREFIX + "Feature appended: " + entry.Name + " (group: " + entry.FeatureGroup + ")");
        }

        public static void UpdateFeatureStatus(string featureName, string newStatus)
        {
            string path = GetFeatureQueuePath();
            if (!File.Exists(path))
                return;

            string content = File.ReadAllText(path);
            string[] lines = content.Split('\n');
            bool found = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("name:") && lines[i].Contains(featureName))
                {
                    found = true;
                    continue;
                }

                if (found && lines[i].TrimStart().StartsWith("status:"))
                {
                    int colonIdx = lines[i].IndexOf(':');
                    lines[i] = lines[i].Substring(0, colonIdx + 1) + " " + newStatus;
                    break;
                }

                if (found && lines[i].TrimStart().StartsWith("- name:"))
                    break;
            }

            if (found)
            {
                File.WriteAllText(path, string.Join("\n", lines));
                Debug.Log(LOG_PREFIX + "Feature status updated: " + featureName + " → " + newStatus);
            }
        }
    }
}
