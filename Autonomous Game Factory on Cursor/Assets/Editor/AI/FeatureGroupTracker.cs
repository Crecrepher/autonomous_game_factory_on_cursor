using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class FeatureGroupTracker
    {
        const string LOG_PREFIX = "[FeatureGroupTracker] ";

        public struct FeatureGroupStatus
        {
            public string FeatureGroup;
            public string[] ModuleNames;
            public string[] ModuleStatuses;
            public bool AllDone;
            public bool HasBlocked;
            public int DoneCount;
            public int TotalCount;

            public string[] HumanStates;
            public string[] CommitStates;
            public string[] LearningStates;
            public bool AllCommitReady;
            public bool AllHumanValidated;
            public bool HasHumanFixes;
            public bool LearningComplete;
            public bool HasEscalated;
            public int CommitReadyCount;
        }

        public static FeatureGroupStatus[] GetFeatureGroupStatuses(
            TaskQueueGenerator.GeneratedTaskEntry[] entries,
            DependencyGraphBuilder.DependencyGraph graph)
        {
            var groupMap = new Dictionary<string, List<string>>();

            for (int i = 0; i < entries.Length; i++)
            {
                string group = entries[i].FeatureGroup;
                if (string.IsNullOrEmpty(group)) continue;

                List<string> modules;
                if (!groupMap.TryGetValue(group, out modules))
                {
                    modules = new List<string>();
                    groupMap[group] = modules;
                }
                modules.Add(entries[i].Name);
            }

            var results = new List<FeatureGroupStatus>();
            var groupKeys = new List<string>(groupMap.Keys);

            for (int g = 0; g < groupKeys.Count; g++)
            {
                string groupName = groupKeys[g];
                List<string> moduleNames = groupMap[groupName];
                results.Add(BuildGroupStatus(groupName, moduleNames, graph));
            }

            return results.ToArray();
        }

        public static FeatureGroupStatus[] ScanTaskQueueForFeatureGroups(DependencyGraphBuilder.DependencyGraph graph)
        {
            var groupModules = new Dictionary<string, List<string>>();

            for (int i = 0; i < graph.Tasks.Length; i++)
            {
                string fg = graph.Tasks[i].FeatureGroup;
                if (string.IsNullOrEmpty(fg)) continue;

                List<string> list;
                if (!groupModules.TryGetValue(fg, out list))
                {
                    list = new List<string>();
                    groupModules[fg] = list;
                }
                list.Add(graph.Tasks[i].Name);
            }

            var results = new List<FeatureGroupStatus>();
            var groupKeys = new List<string>(groupModules.Keys);
            for (int g = 0; g < groupKeys.Count; g++)
                results.Add(BuildGroupStatus(groupKeys[g], groupModules[groupKeys[g]], graph));

            return results.ToArray();
        }

        public static string[] GetCommitReadyGroups(DependencyGraphBuilder.DependencyGraph graph)
        {
            FeatureGroupStatus[] groups = ScanTaskQueueForFeatureGroups(graph);
            var ready = new List<string>();

            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].AllCommitReady && !groups[i].HasBlocked && !groups[i].HasEscalated
                    && groups[i].AllHumanValidated && groups[i].TotalCount > 0)
                    ready.Add(groups[i].FeatureGroup);
            }

            return ready.ToArray();
        }

        static FeatureGroupStatus BuildGroupStatus(string groupName, List<string> moduleNames,
            DependencyGraphBuilder.DependencyGraph graph)
        {
            int count = moduleNames.Count;
            FeatureGroupStatus s = new FeatureGroupStatus();
            s.FeatureGroup = groupName;
            s.ModuleNames = moduleNames.ToArray();
            s.ModuleStatuses = new string[count];
            s.HumanStates = new string[count];
            s.CommitStates = new string[count];
            s.LearningStates = new string[count];
            s.TotalCount = count;
            s.DoneCount = 0;
            s.CommitReadyCount = 0;
            s.HasBlocked = false;
            s.HasEscalated = false;
            s.AllDone = true;
            s.AllCommitReady = true;
            s.AllHumanValidated = true;
            s.HasHumanFixes = false;
            s.LearningComplete = true;

            for (int m = 0; m < count; m++)
            {
                DependencyGraphBuilder.TaskEntry task;
                if (!graph.TaskMap.TryGetValue(moduleNames[m], out task))
                {
                    s.ModuleStatuses[m] = "unknown";
                    s.HumanStates[m] = "none";
                    s.CommitStates[m] = "none";
                    s.LearningStates[m] = "none";
                    s.AllDone = false;
                    s.AllCommitReady = false;
                    s.AllHumanValidated = false;
                    continue;
                }

                s.ModuleStatuses[m] = task.Status;
                s.HumanStates[m] = task.HumanState;
                s.CommitStates[m] = task.CommitState;
                s.LearningStates[m] = task.LearningState;

                if (task.Status == "done")
                    s.DoneCount++;
                else
                    s.AllDone = false;

                if (task.Status == "blocked")
                    s.HasBlocked = true;

                if (task.Status == "escalated")
                    s.HasEscalated = true;

                if (task.CommitState == "ready")
                    s.CommitReadyCount++;
                else
                    s.AllCommitReady = false;

                if (task.HumanState != "validated")
                    s.AllHumanValidated = false;

                if (task.LearningState != "none" && task.LearningState != "recorded")
                    s.LearningComplete = false;
            }

            return s;
        }

        public static string FormatGroupStatusReport(FeatureGroupStatus[] groups)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Feature Group Status Report");
            sb.AppendLine("");

            for (int i = 0; i < groups.Length; i++)
            {
                FeatureGroupStatus g = groups[i];
                string tag;
                if (g.AllCommitReady && g.AllHumanValidated && !g.HasBlocked && !g.HasEscalated)
                    tag = "COMMIT-READY";
                else if (g.HasEscalated)
                    tag = "ESCALATED";
                else if (g.HasBlocked)
                    tag = "BLOCKED";
                else
                    tag = "IN-PROGRESS";

                sb.AppendLine("## " + g.FeatureGroup + " [" + tag + "]");
                sb.AppendLine("Progress: " + g.DoneCount + "/" + g.TotalCount
                    + " done | " + g.CommitReadyCount + "/" + g.TotalCount + " commit-ready");
                sb.AppendLine("Human: " + (g.AllHumanValidated ? "ALL-VALIDATED" : "PENDING")
                    + " | Learning: " + (g.LearningComplete ? "OK" : "PENDING"));
                sb.AppendLine("");
                for (int m = 0; m < g.ModuleNames.Length; m++)
                {
                    sb.Append("- ").Append(g.ModuleNames[m]);
                    sb.Append(": status=").Append(g.ModuleStatuses[m]);
                    sb.Append(" human=").Append(g.HumanStates[m]);
                    sb.Append(" commit=").Append(g.CommitStates[m]);
                    sb.Append(" learning=").Append(g.LearningStates[m]);
                    sb.AppendLine();
                }
                sb.AppendLine("");
            }

            return sb.ToString();
        }
    }
}
