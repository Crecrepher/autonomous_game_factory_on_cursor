using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class TaskExecutionEngine
    {
        const string LOG_PREFIX = "[ExecEngine] ";
        const int MAX_CONCURRENT_SLOTS = 4;
        const int MAX_EXECUTION_ROUNDS = 30;
        const int DEFAULT_TIMEOUT_MINUTES = 30;
        const string TASK_QUEUE_RELATIVE = "docs/ai/TASK_QUEUE.yaml";
        const string MODULE_REGISTRY_RELATIVE = "docs/ai/MODULE_REGISTRY.yaml";
        const string EXECUTION_LOG_RELATIVE = "docs/ai/runs/PARALLEL_EXECUTION_LOG.md";

        [MenuItem("Tools/AI/Parallel Execution Engine (Dry Run)")]
        public static void RunDryExecution()
        {
            Debug.Log(LOG_PREFIX + "=== Parallel Execution Engine — Dry Run ===");

            ExecutionContext ctx = BuildExecutionContext();
            if (ctx.Aborted)
            {
                Debug.LogError(LOG_PREFIX + "ABORT: " + ctx.AbortReason);
                return;
            }

            DependencyReadyQueue readyQueue = BuildReadyQueue(ctx);
            Debug.Log(LOG_PREFIX + "Ready queue: " + readyQueue.Entries.Count + " tasks ready");

            SimulateParallelExecution(ctx, readyQueue);

            string report = FormatExecutionReport(ctx);
            Debug.Log(report);
            WriteExecutionLog(ctx, report);

            EditorUtility.DisplayDialog(
                "Parallel Execution Engine — Dry Run",
                "Rounds: " + ctx.TotalRounds
                + "\nTasks Completed: " + ctx.CompletedCount
                + "\nMax Concurrent: " + ctx.MaxConcurrentAchieved
                + "\nSee Console for details.",
                "OK");
        }

        static ExecutionContext BuildExecutionContext()
        {
            ExecutionContext ctx = new ExecutionContext();
            ctx.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            DependencyGraphBuilder.DependencyGraph graph = DependencyGraphBuilder.BuildGraph();

            string cycleChain;
            if (DependencyGraphBuilder.DetectCycle(graph.Modules, out cycleChain))
            {
                ctx.Aborted = true;
                ctx.AbortReason = "Circular dependency: " + cycleChain;
                return ctx;
            }

            ctx.TopologicalOrder = DependencyGraphBuilder.TopologicalSort(graph.Modules);
            ctx.Graph = graph;

            for (int i = 0; i < graph.Tasks.Length; i++)
            {
                DependencyGraphBuilder.TaskEntry task = graph.Tasks[i];
                TaskExecutionUnit unit = new TaskExecutionUnit();
                unit.TaskName = task.Name;
                unit.UnitId = "exec_" + task.Name + "_" + DateTimeOffset.Now.ToUnixTimeSeconds();
                unit.OriginalStatus = task.Status;
                unit.DependsOn = task.DependsOn;
                unit.FeatureGroup = task.FeatureGroup;
                unit.MergeTarget = "ai_test";
                unit.TimeoutMinutes = DEFAULT_TIMEOUT_MINUTES;

                string modulePath = FindModulePath(graph.Modules, task.Name);
                unit.ModulePath = modulePath;
                unit.IsolatedWorkspace = new WorkspaceRef();
                unit.IsolatedWorkspace.Type = EWorkspaceType.FolderIsolation;
                unit.IsolatedWorkspace.Ref = modulePath;

                if (task.Status == "done")
                {
                    unit.ExecutionStatus = EExecutionStatus.Done;
                    unit.DependencyStatus = EDependencyStatus.Satisfied;
                }
                else
                {
                    unit.ExecutionStatus = EExecutionStatus.Pending;
                    unit.DependencyStatus = EDependencyStatus.Waiting;
                }

                ctx.Units.Add(unit);
            }

            UpdateDependencyStatuses(ctx);
            return ctx;
        }

        static void UpdateDependencyStatuses(ExecutionContext ctx)
        {
            for (int i = 0; i < ctx.Units.Count; i++)
            {
                TaskExecutionUnit unit = ctx.Units[i];
                if (unit.ExecutionStatus == EExecutionStatus.Done)
                    continue;

                if (unit.DependsOn == null || unit.DependsOn.Length == 0)
                {
                    unit.DependencyStatus = EDependencyStatus.Satisfied;
                    continue;
                }

                bool allSatisfied = true;
                bool anyBlocked = false;

                for (int d = 0; d < unit.DependsOn.Length; d++)
                {
                    TaskExecutionUnit dep = FindUnit(ctx.Units, unit.DependsOn[d]);
                    if (dep == null)
                    {
                        allSatisfied = false;
                        continue;
                    }

                    if (dep.ExecutionStatus == EExecutionStatus.Blocked)
                    {
                        anyBlocked = true;
                        break;
                    }

                    if (dep.ExecutionStatus != EExecutionStatus.Done)
                        allSatisfied = false;
                }

                if (anyBlocked)
                    unit.DependencyStatus = EDependencyStatus.Blocked;
                else if (allSatisfied)
                    unit.DependencyStatus = EDependencyStatus.Satisfied;
                else
                    unit.DependencyStatus = EDependencyStatus.Waiting;
            }
        }

        static DependencyReadyQueue BuildReadyQueue(ExecutionContext ctx)
        {
            DependencyReadyQueue queue = new DependencyReadyQueue();
            queue.Timestamp = ctx.Timestamp;
            queue.MaxConcurrent = MAX_CONCURRENT_SLOTS;

            for (int i = 0; i < ctx.Units.Count; i++)
            {
                TaskExecutionUnit unit = ctx.Units[i];
                if (unit.DependencyStatus == EDependencyStatus.Satisfied
                    && unit.ExecutionStatus == EExecutionStatus.Pending
                    && unit.LeaseId == null)
                {
                    ReadyQueueEntry entry = new ReadyQueueEntry();
                    entry.UnitId = unit.UnitId;
                    entry.TaskName = unit.TaskName;
                    entry.FeatureGroup = unit.FeatureGroup;
                    entry.WorkspaceRequirement = unit.IsolatedWorkspace.Type;
                    queue.Entries.Add(entry);
                }
            }

            SortReadyQueue(queue);

            int activeLeases = 0;
            for (int i = 0; i < ctx.Leases.Count; i++)
            {
                if (ctx.Leases[i].Status == ELeaseStatus.Active)
                    activeLeases++;
            }
            queue.ActiveLeases = activeLeases;
            queue.AvailableSlots = MAX_CONCURRENT_SLOTS - activeLeases;

            return queue;
        }

        static void SortReadyQueue(DependencyReadyQueue queue)
        {
            for (int i = 0; i < queue.Entries.Count - 1; i++)
            {
                for (int j = i + 1; j < queue.Entries.Count; j++)
                {
                    if (string.Compare(queue.Entries[i].TaskName, queue.Entries[j].TaskName, StringComparison.Ordinal) > 0)
                    {
                        ReadyQueueEntry temp = queue.Entries[i];
                        queue.Entries[i] = queue.Entries[j];
                        queue.Entries[j] = temp;
                    }
                }
            }
        }

        static void SimulateParallelExecution(ExecutionContext ctx, DependencyReadyQueue initialQueue)
        {
            int round = 0;
            while (round < MAX_EXECUTION_ROUNDS)
            {
                round++;
                ExecutionRound roundResult = new ExecutionRound();
                roundResult.RoundNumber = round;

                UpdateDependencyStatuses(ctx);
                DependencyReadyQueue queue = BuildReadyQueue(ctx);

                if (queue.Entries.Count == 0)
                {
                    bool anyPending = false;
                    for (int i = 0; i < ctx.Units.Count; i++)
                    {
                        if (ctx.Units[i].ExecutionStatus != EExecutionStatus.Done
                            && ctx.Units[i].ExecutionStatus != EExecutionStatus.Blocked)
                        {
                            anyPending = true;
                            break;
                        }
                    }

                    if (!anyPending)
                    {
                        roundResult.AllDone = true;
                        ctx.Rounds.Add(roundResult);
                        break;
                    }

                    roundResult.Deadlock = true;
                    ctx.Rounds.Add(roundResult);
                    Debug.LogWarning(LOG_PREFIX + "DEADLOCK at round " + round);
                    break;
                }

                int slotsToFill = Math.Min(queue.Entries.Count, queue.AvailableSlots);
                if (slotsToFill > MAX_CONCURRENT_SLOTS)
                    slotsToFill = MAX_CONCURRENT_SLOTS;

                roundResult.ConcurrentCount = slotsToFill;
                if (slotsToFill > ctx.MaxConcurrentAchieved)
                    ctx.MaxConcurrentAchieved = slotsToFill;

                Debug.Log(LOG_PREFIX + "── Round " + round + " ── (" + slotsToFill + " concurrent)");

                for (int i = 0; i < slotsToFill; i++)
                {
                    ReadyQueueEntry entry = queue.Entries[i];
                    TaskExecutionUnit unit = FindUnit(ctx.Units, entry.TaskName);

                    AgentLease lease = new AgentLease();
                    lease.LeaseId = "lease_" + entry.TaskName + "_" + round;
                    lease.TaskName = entry.TaskName;
                    lease.UnitId = entry.UnitId;
                    lease.AgentSlot = "agent_slot_" + (i + 1);
                    lease.AgentType = EAgentType.Subagent;
                    lease.StartedAt = DateTime.Now.ToString("HH:mm:ss");
                    lease.Status = ELeaseStatus.Active;
                    lease.Workspace = unit.IsolatedWorkspace;

                    unit.LeaseId = lease.LeaseId;
                    unit.ExecutionOwner = lease.AgentSlot;
                    unit.ExecutionStatus = EExecutionStatus.Executing;

                    ctx.Leases.Add(lease);

                    Debug.Log(LOG_PREFIX + "  LEASE: " + lease.AgentSlot
                        + " → " + entry.TaskName
                        + " (" + unit.ModulePath + ")");

                    roundResult.LeasedTasks.Add(entry.TaskName);
                }

                for (int i = 0; i < slotsToFill; i++)
                {
                    string taskName = roundResult.LeasedTasks[i];
                    TaskExecutionUnit unit = FindUnit(ctx.Units, taskName);
                    AgentLease lease = FindActiveLease(ctx.Leases, taskName);

                    bool editBoundaryOk = ValidateEditBoundary(unit.ModulePath);
                    bool isolationOk = ValidateIsolation(ctx, unit);

                    if (editBoundaryOk && isolationOk)
                    {
                        unit.ExecutionStatus = EExecutionStatus.Done;
                        lease.Status = ELeaseStatus.Completed;
                        unit.LeaseId = null;
                        unit.ExecutionOwner = null;
                        ctx.CompletedCount++;

                        roundResult.CompletedTasks.Add(taskName);
                        Debug.Log(LOG_PREFIX + "  DONE: " + taskName + " (slot " + lease.AgentSlot + ")");
                    }
                    else
                    {
                        unit.ExecutionStatus = EExecutionStatus.Blocked;
                        lease.Status = ELeaseStatus.Failed;
                        unit.LeaseId = null;
                        unit.BlockedReason = editBoundaryOk ? "Isolation violation" : "Edit boundary violation";

                        roundResult.BlockedTasks.Add(taskName);
                        Debug.LogWarning(LOG_PREFIX + "  BLOCKED: " + taskName + " → " + unit.BlockedReason);
                    }
                }

                ctx.Rounds.Add(roundResult);
            }

            ctx.TotalRounds = round;
        }

        static bool ValidateEditBoundary(string modulePath)
        {
            if (string.IsNullOrEmpty(modulePath))
                return false;
            if (!modulePath.StartsWith("Assets/Game/Modules/"))
                return false;

            string[] forbidden = new string[]
            {
                "Assets/Editor/AI/",
                "Assets/Game/Core/",
                "Assets/Game/Modules/Template/"
            };

            for (int i = 0; i < forbidden.Length; i++)
            {
                if (modulePath.StartsWith(forbidden[i]))
                    return false;
            }
            return true;
        }

        static bool ValidateIsolation(ExecutionContext ctx, TaskExecutionUnit currentUnit)
        {
            for (int i = 0; i < ctx.Leases.Count; i++)
            {
                AgentLease other = ctx.Leases[i];
                if (other.Status != ELeaseStatus.Active)
                    continue;
                if (other.TaskName == currentUnit.TaskName)
                    continue;

                TaskExecutionUnit otherUnit = FindUnit(ctx.Units, other.TaskName);
                if (otherUnit != null && otherUnit.ModulePath == currentUnit.ModulePath)
                    return false;
            }
            return true;
        }

        static string FindModulePath(DependencyGraphBuilder.RegistryModule[] modules, string name)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i].Name == name)
                    return modules[i].Path;
            }
            return "Assets/Game/Modules/" + name;
        }

        static TaskExecutionUnit FindUnit(List<TaskExecutionUnit> units, string name)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].TaskName == name)
                    return units[i];
            }
            return null;
        }

        static AgentLease FindActiveLease(List<AgentLease> leases, string taskName)
        {
            for (int i = 0; i < leases.Count; i++)
            {
                if (leases[i].TaskName == taskName && leases[i].Status == ELeaseStatus.Active)
                    return leases[i];
            }
            return null;
        }

        static string FormatExecutionReport(ExecutionContext ctx)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════");
            sb.AppendLine("  Parallel Execution Engine — Report");
            sb.AppendLine("═══════════════════════════════════════════════════");
            sb.AppendLine("Timestamp: " + ctx.Timestamp);
            sb.AppendLine("Total Rounds: " + ctx.TotalRounds);
            sb.AppendLine("Tasks Completed: " + ctx.CompletedCount);
            sb.AppendLine("Max Concurrent: " + ctx.MaxConcurrentAchieved);
            sb.AppendLine("");

            sb.AppendLine("── Execution Units ──");
            for (int i = 0; i < ctx.Units.Count; i++)
            {
                TaskExecutionUnit u = ctx.Units[i];
                sb.AppendLine("  " + u.TaskName
                    + " | status: " + u.ExecutionStatus
                    + " | dep: " + u.DependencyStatus
                    + " | path: " + u.ModulePath);
            }
            sb.AppendLine("");

            sb.AppendLine("── Rounds ──");
            for (int r = 0; r < ctx.Rounds.Count; r++)
            {
                ExecutionRound round = ctx.Rounds[r];
                sb.AppendLine("Round " + round.RoundNumber
                    + " | concurrent: " + round.ConcurrentCount);

                if (round.LeasedTasks.Count > 0)
                {
                    sb.Append("  Leased: ");
                    for (int i = 0; i < round.LeasedTasks.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(round.LeasedTasks[i]);
                    }
                    sb.AppendLine("");
                }

                if (round.CompletedTasks.Count > 0)
                {
                    sb.Append("  Completed: ");
                    for (int i = 0; i < round.CompletedTasks.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(round.CompletedTasks[i]);
                    }
                    sb.AppendLine("");
                }

                if (round.BlockedTasks.Count > 0)
                {
                    sb.Append("  Blocked: ");
                    for (int i = 0; i < round.BlockedTasks.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(round.BlockedTasks[i]);
                    }
                    sb.AppendLine("");
                }

                if (round.AllDone) sb.AppendLine("  ALL DONE");
                if (round.Deadlock) sb.AppendLine("  DEADLOCK");
            }

            sb.AppendLine("");

            sb.AppendLine("── Lease History ──");
            for (int i = 0; i < ctx.Leases.Count; i++)
            {
                AgentLease l = ctx.Leases[i];
                sb.AppendLine("  " + l.LeaseId
                    + " | " + l.AgentSlot
                    + " → " + l.TaskName
                    + " | " + l.Status);
            }

            sb.AppendLine("═══════════════════════════════════════════════════");
            return sb.ToString();
        }

        static void WriteExecutionLog(ExecutionContext ctx, string report)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string logPath = Path.Combine(projectRoot, EXECUTION_LOG_RELATIVE);
            string logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            StringBuilder sb = new StringBuilder();

            string existing = "";
            if (File.Exists(logPath))
                existing = File.ReadAllText(logPath);

            sb.AppendLine("");
            sb.AppendLine("---");
            sb.AppendLine("");
            sb.AppendLine("## Parallel Execution " + ctx.Timestamp);
            sb.AppendLine("");
            sb.AppendLine("- Rounds: " + ctx.TotalRounds);
            sb.AppendLine("- Completed: " + ctx.CompletedCount);
            sb.AppendLine("- Max Concurrent: " + ctx.MaxConcurrentAchieved);
            sb.AppendLine("");

            for (int r = 0; r < ctx.Rounds.Count; r++)
            {
                ExecutionRound round = ctx.Rounds[r];
                sb.AppendLine("### Round " + round.RoundNumber + " (×" + round.ConcurrentCount + ")");

                if (round.CompletedTasks.Count > 0)
                {
                    sb.Append("Completed: ");
                    for (int i = 0; i < round.CompletedTasks.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(round.CompletedTasks[i]);
                    }
                    sb.AppendLine("");
                }

                if (round.BlockedTasks.Count > 0)
                {
                    sb.Append("Blocked: ");
                    for (int i = 0; i < round.BlockedTasks.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(round.BlockedTasks[i]);
                    }
                    sb.AppendLine("");
                }

                sb.AppendLine("");
            }

            File.WriteAllText(logPath, existing + sb.ToString());
            Debug.Log(LOG_PREFIX + "Execution log written: " + logPath);
        }

        #region Data Structures

        public enum EExecutionStatus
        {
            Pending,
            Ready,
            Executing,
            Validating,
            Merging,
            Done,
            Blocked
        }

        public enum EDependencyStatus
        {
            Waiting,
            Satisfied,
            Blocked
        }

        public enum EAgentType
        {
            Primary,
            Subagent,
            Background,
            Cloud
        }

        public enum ELeaseStatus
        {
            Active,
            Completed,
            Released,
            Timeout,
            Failed
        }

        public enum EWorkspaceType
        {
            None,
            FolderIsolation,
            Branch,
            Worktree,
            Cloud
        }

        public class WorkspaceRef
        {
            public EWorkspaceType Type;
            public string Ref;
            public string Path;
        }

        public class TaskExecutionUnit
        {
            public string TaskName;
            public string UnitId;
            public string OriginalStatus;
            public string[] DependsOn;
            public string FeatureGroup;
            public string ModulePath;
            public string MergeTarget;
            public int TimeoutMinutes;

            public EExecutionStatus ExecutionStatus;
            public EDependencyStatus DependencyStatus;
            public string ExecutionOwner;
            public string LeaseId;
            public WorkspaceRef IsolatedWorkspace;
            public string BlockedReason;
        }

        public class AgentLease
        {
            public string LeaseId;
            public string TaskName;
            public string UnitId;
            public string AgentSlot;
            public EAgentType AgentType;
            public string StartedAt;
            public ELeaseStatus Status;
            public WorkspaceRef Workspace;
        }

        public class ReadyQueueEntry
        {
            public string UnitId;
            public string TaskName;
            public string FeatureGroup;
            public EWorkspaceType WorkspaceRequirement;
        }

        public class DependencyReadyQueue
        {
            public string Timestamp;
            public List<ReadyQueueEntry> Entries = new List<ReadyQueueEntry>();
            public int MaxConcurrent;
            public int ActiveLeases;
            public int AvailableSlots;
        }

        public class ExecutionRound
        {
            public int RoundNumber;
            public int ConcurrentCount;
            public List<string> LeasedTasks = new List<string>();
            public List<string> CompletedTasks = new List<string>();
            public List<string> BlockedTasks = new List<string>();
            public bool AllDone;
            public bool Deadlock;
        }

        public class ExecutionContext
        {
            public string Timestamp;
            public string[] TopologicalOrder;
            public DependencyGraphBuilder.DependencyGraph Graph;
            public List<TaskExecutionUnit> Units = new List<TaskExecutionUnit>();
            public List<AgentLease> Leases = new List<AgentLease>();
            public List<ExecutionRound> Rounds = new List<ExecutionRound>();
            public int TotalRounds;
            public int CompletedCount;
            public int MaxConcurrentAchieved;
            public bool Aborted;
            public string AbortReason;
        }

        #endregion
    }
}
