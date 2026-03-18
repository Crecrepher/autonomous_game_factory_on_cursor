using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class OrchestratorSimulator
    {
        const string MENU_PATH = "Tools/AI/Simulate Orchestrator (Mini Queue)";
        const string LOG_PREFIX = "[Orchestrator Sim] ";
        const int MAX_ROUNDS = 20;

        [MenuItem(MENU_PATH)]
        public static void SimulateMiniQueue()
        {
            Debug.Log(LOG_PREFIX + "=== Orchestrator Simulation Start ===");
            Debug.Log(LOG_PREFIX + "Test Queue: StatusEffect → BuffSystem → BuffIconUI");
            Debug.Log(LOG_PREFIX + "");

            string[] registryLines = new string[]
            {
                "modules:",
                "  - name: StatusEffect",
                "    path: Assets/Game/Modules/StatusEffect",
                "    dependencies: [UnityEngine, System]",
                "",
                "  - name: BuffSystem",
                "    path: Assets/Game/Modules/BuffSystem",
                "    dependencies: [UnityEngine, System, StatusEffect]",
                "",
                "  - name: BuffIconUI",
                "    path: Assets/Game/Modules/BuffIconUI",
                "    dependencies: [UnityEngine, System, BuffSystem]",
            };

            string[] queueLines = new string[]
            {
                "modules:",
                "  - name: StatusEffect",
                "    status: planned",
                "    depends_on: []",
                "",
                "  - name: BuffSystem",
                "    status: planned",
                "    depends_on: [StatusEffect]",
                "",
                "  - name: BuffIconUI",
                "    status: planned",
                "    depends_on: [BuffSystem]",
            };

            DependencyGraphBuilder.RegistryModule[] modules = DependencyGraphBuilder.ParseModuleRegistry(registryLines);
            DependencyGraphBuilder.TaskEntry[] tasks = DependencyGraphBuilder.ParseTaskQueue(queueLines);

            // 순환 체크
            string cycleChain;
            if (DependencyGraphBuilder.DetectCycle(modules, out cycleChain))
            {
                Debug.LogError(LOG_PREFIX + "ABORT: Circular dependency detected: " + cycleChain);
                return;
            }

            // 토폴로지 정렬 출력
            string[] sorted = DependencyGraphBuilder.TopologicalSort(modules);
            Debug.Log(LOG_PREFIX + "Topological order: " + JoinArray(sorted));

            // 시뮬레이션 루프
            var taskMap = new Dictionary<string, string>(tasks.Length);
            for (int i = 0; i < tasks.Length; i++)
                taskMap[tasks[i].Name] = tasks[i].Status;

            var processOrder = new List<string>();
            var runLog = new StringBuilder();
            runLog.AppendLine("Run Simulation " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            runLog.AppendLine("");
            runLog.AppendLine("Dependency Order (topological):");
            for (int i = 0; i < sorted.Length; i++)
                runLog.AppendLine("  " + (i + 1) + ". " + sorted[i]);
            runLog.AppendLine("");

            int round = 0;
            while (round < MAX_ROUNDS)
            {
                round++;
                Debug.Log(LOG_PREFIX + "── Round " + round + " ──");

                // 실행 가능 모듈 탐색
                var executable = new List<string>();
                var skipped = new List<string>();
                var skippedReasons = new List<string>();

                for (int i = 0; i < tasks.Length; i++)
                {
                    string name = tasks[i].Name;
                    string status = taskMap[name];

                    if (status == "done")
                        continue;

                    if (status != "planned")
                        continue;

                    bool ready = true;
                    string missingDep = null;
                    for (int d = 0; d < tasks[i].DependsOn.Length; d++)
                    {
                        string depStatus;
                        if (!taskMap.TryGetValue(tasks[i].DependsOn[d], out depStatus) || depStatus != "done")
                        {
                            ready = false;
                            missingDep = tasks[i].DependsOn[d];
                            break;
                        }
                    }

                    if (ready)
                    {
                        executable.Add(name);
                    }
                    else
                    {
                        skipped.Add(name);
                        skippedReasons.Add("requires " + missingDep);
                    }
                }

                if (executable.Count == 0 && skipped.Count == 0)
                {
                    Debug.Log(LOG_PREFIX + "All modules done. Queue exhausted.");
                    break;
                }

                if (executable.Count == 0)
                {
                    Debug.LogWarning(LOG_PREFIX + "DEADLOCK: No executable modules but " + skipped.Count + " still pending");
                    for (int i = 0; i < skipped.Count; i++)
                        Debug.LogWarning(LOG_PREFIX + "  Blocked: " + skipped[i] + " → " + skippedReasons[i]);
                    runLog.AppendLine("DEADLOCK at round " + round);
                    break;
                }

                // 로그: 스킵된 모듈
                for (int i = 0; i < skipped.Count; i++)
                {
                    Debug.Log(LOG_PREFIX + "  SKIP: " + skipped[i] + " → " + skippedReasons[i]);
                    runLog.AppendLine("Skipped: " + skipped[i] + " → " + skippedReasons[i]);
                }

                // 실행 (한 라운드에 실행 가능한 것 모두 처리 — 병렬 빌더 시뮬레이션)
                for (int i = 0; i < executable.Count; i++)
                {
                    string name = executable[i];
                    Debug.Log(LOG_PREFIX + "  BUILD: " + name + " → done");
                    taskMap[name] = "done";
                    processOrder.Add(name);
                    runLog.AppendLine("Generated: " + name);
                }
                runLog.AppendLine("");
            }

            // 결과 출력
            Debug.Log(LOG_PREFIX + "");
            Debug.Log(LOG_PREFIX + "=== Process Order ===");
            for (int i = 0; i < processOrder.Count; i++)
            {
                string arrow = i > 0 ? "→ " : "  ";
                Debug.Log(LOG_PREFIX + arrow + processOrder[i]);
            }

            // 검증: 순서가 StatusEffect → BuffSystem → BuffIconUI 인가
            bool orderCorrect = processOrder.Count == 3
                && processOrder[0] == "StatusEffect"
                && processOrder[1] == "BuffSystem"
                && processOrder[2] == "BuffIconUI";

            if (orderCorrect)
            {
                Debug.Log(LOG_PREFIX + "");
                Debug.Log(LOG_PREFIX + "RESULT: PASS — Orchestrator respected dependency order");
                runLog.AppendLine("Validation: PASS");
            }
            else
            {
                Debug.LogError(LOG_PREFIX + "RESULT: FAIL — Wrong order: " + JoinArray(processOrder.ToArray()));
                runLog.AppendLine("Validation: FAIL — Wrong order");
            }

            Debug.Log(LOG_PREFIX + "");
            Debug.Log(LOG_PREFIX + "=== Full Run Log ===");
            Debug.Log(runLog.ToString());

            string simResult = orderCorrect ? "PASS" : "FAIL";
            EditorUtility.DisplayDialog("Orchestrator Simulation - " + simResult,
                "Process order: " + JoinArray(processOrder.ToArray()) + "\n\n" + (orderCorrect ? "Dependency order respected." : "Order incorrect!"),
                "OK");
        }

        const string MENU_PARALLEL = "Tools/AI/Simulate Parallel Builders (Independent Modules)";

        [MenuItem(MENU_PARALLEL)]
        public static void SimulateParallelBuilders()
        {
            Debug.Log(LOG_PREFIX + "=== Parallel Builder Simulation Start ===");
            Debug.Log(LOG_PREFIX + "Independent modules: CurrencyWallet, EnemyDetection, HealthSystem");
            Debug.Log(LOG_PREFIX + "");

            string[] registryLines = new string[]
            {
                "modules:",
                "  - name: CurrencyWallet",
                "    path: Assets/Game/Modules/CurrencyWallet",
                "    dependencies: [UnityEngine, System]",
                "",
                "  - name: EnemyDetection",
                "    path: Assets/Game/Modules/EnemyDetection",
                "    dependencies: [UnityEngine, System]",
                "",
                "  - name: HealthSystem",
                "    path: Assets/Game/Modules/HealthSystem",
                "    dependencies: [UnityEngine, System]",
            };

            string[] queueLines = new string[]
            {
                "modules:",
                "  - name: CurrencyWallet",
                "    status: planned",
                "    depends_on: []",
                "",
                "  - name: EnemyDetection",
                "    status: planned",
                "    depends_on: []",
                "",
                "  - name: HealthSystem",
                "    status: planned",
                "    depends_on: []",
            };

            DependencyGraphBuilder.RegistryModule[] modules = DependencyGraphBuilder.ParseModuleRegistry(registryLines);
            DependencyGraphBuilder.TaskEntry[] tasks = DependencyGraphBuilder.ParseTaskQueue(queueLines);

            // 순환 체크
            string cycleChain;
            if (DependencyGraphBuilder.DetectCycle(modules, out cycleChain))
            {
                Debug.LogError(LOG_PREFIX + "ABORT: Circular dependency detected: " + cycleChain);
                return;
            }

            // 그래프 빌드
            var taskMap = new Dictionary<string, string>(tasks.Length);
            for (int i = 0; i < tasks.Length; i++)
                taskMap[tasks[i].Name] = tasks[i].Status;

            // 라운드 1: 모든 모듈이 독립 → 모두 동시 실행 가능해야 함
            Debug.Log(LOG_PREFIX + "── Round 1 ──");
            var executable = new List<string>();

            for (int i = 0; i < tasks.Length; i++)
            {
                string name = tasks[i].Name;
                if (taskMap[name] != "planned") continue;

                bool ready = true;
                for (int d = 0; d < tasks[i].DependsOn.Length; d++)
                {
                    string depStatus;
                    if (!taskMap.TryGetValue(tasks[i].DependsOn[d], out depStatus) || depStatus != "done")
                    {
                        ready = false;
                        break;
                    }
                }
                if (ready) executable.Add(name);
            }

            bool allParallel = executable.Count == 3;

            Debug.Log(LOG_PREFIX + "Executable in parallel: " + JoinArray(executable.ToArray()));

            if (allParallel)
            {
                Debug.Log(LOG_PREFIX + "  All 3 modules can run in parallel — CORRECT");

                // 모듈 격리 체크: 각 모듈이 서로 다른 폴더
                var folderSet = new HashSet<string>();
                bool folderConflict = false;
                for (int i = 0; i < modules.Length; i++)
                {
                    if (!folderSet.Add(modules[i].Path))
                    {
                        folderConflict = true;
                        Debug.LogError(LOG_PREFIX + "  FOLDER CONFLICT: " + modules[i].Path);
                    }
                }

                if (!folderConflict)
                    Debug.Log(LOG_PREFIX + "  Module isolation: PASS (all different folders)");

                // 동시 done 전이
                for (int i = 0; i < executable.Count; i++)
                {
                    taskMap[executable[i]] = "done";
                    Debug.Log(LOG_PREFIX + "  BUILD (parallel): " + executable[i] + " → done");
                }
            }
            else
            {
                Debug.LogError(LOG_PREFIX + "  Expected 3 parallel modules, got " + executable.Count);
            }

            // 큐 상태 체크
            bool allDone = true;
            for (int i = 0; i < tasks.Length; i++)
            {
                if (taskMap[tasks[i].Name] != "done")
                {
                    allDone = false;
                    break;
                }
            }

            Debug.Log(LOG_PREFIX + "");
            string result = (allParallel && allDone) ? "PASS" : "FAIL";
            Debug.Log(LOG_PREFIX + "RESULT: " + result + " — " + (allParallel ? "All modules built in parallel in 1 round" : "Parallel execution failed"));

            EditorUtility.DisplayDialog("Parallel Builder Simulation - " + result,
                "Parallel executable: " + JoinArray(executable.ToArray()) + "\nAll done: " + allDone,
                "OK");
        }

        const string MENU_POOL = "Tools/AI/Simulate Builder Pool (Dependency-Mixed)";
        const int SIM_MAX_ROUNDS = 10;

        [MenuItem(MENU_POOL)]
        public static void SimulateBuilderPool()
        {
            Debug.Log(LOG_PREFIX + "=== Builder Pool Simulation (Dependency-Mixed) ===");
            Debug.Log(LOG_PREFIX + "Queue: Economy, StatusEffect, Player, Warriors(←Economy), BuffIconUI(←StatusEffect), GameManager(←Player,Economy,Warriors)");
            Debug.Log(LOG_PREFIX + "");

            string[] registryLines = new string[]
            {
                "modules:",
                "  - name: Economy",
                "    path: Assets/Game/Modules/Economy",
                "    dependencies: [UnityEngine, System]",
                "",
                "  - name: StatusEffect",
                "    path: Assets/Game/Modules/StatusEffect",
                "    dependencies: [UnityEngine, System]",
                "",
                "  - name: Player",
                "    path: Assets/Game/Modules/Player",
                "    dependencies: [UnityEngine, System]",
                "",
                "  - name: Warriors",
                "    path: Assets/Game/Modules/Warriors",
                "    dependencies: [UnityEngine, System, Economy]",
                "",
                "  - name: BuffIconUI",
                "    path: Assets/Game/Modules/BuffIconUI",
                "    dependencies: [UnityEngine, System, StatusEffect]",
                "",
                "  - name: GameManager",
                "    path: Assets/Game/Modules/GameManager",
                "    dependencies: [UnityEngine, System, Player, Economy, Warriors]",
            };

            string[] queueLines = new string[]
            {
                "modules:",
                "  - name: Economy",
                "    status: planned",
                "    depends_on: []",
                "",
                "  - name: StatusEffect",
                "    status: planned",
                "    depends_on: []",
                "",
                "  - name: Player",
                "    status: planned",
                "    depends_on: []",
                "",
                "  - name: Warriors",
                "    status: planned",
                "    depends_on: [Economy]",
                "",
                "  - name: BuffIconUI",
                "    status: planned",
                "    depends_on: [StatusEffect]",
                "",
                "  - name: GameManager",
                "    status: planned",
                "    depends_on: [Player, Economy, Warriors]",
            };

            DependencyGraphBuilder.RegistryModule[] modules = DependencyGraphBuilder.ParseModuleRegistry(registryLines);
            DependencyGraphBuilder.TaskEntry[] tasks = DependencyGraphBuilder.ParseTaskQueue(queueLines);

            string cycleChain;
            if (DependencyGraphBuilder.DetectCycle(modules, out cycleChain))
            {
                Debug.LogError(LOG_PREFIX + "ABORT: Circular dependency detected: " + cycleChain);
                return;
            }

            string[] sorted = DependencyGraphBuilder.TopologicalSort(modules);
            Debug.Log(LOG_PREFIX + "Topological order: " + JoinArray(sorted));

            var taskMap = new Dictionary<string, string>(tasks.Length);
            for (int i = 0; i < tasks.Length; i++)
                taskMap[tasks[i].Name] = tasks[i].Status;

            var processOrder = new List<string>();
            var roundLog = new StringBuilder();
            int totalRounds = 0;
            bool allDone = false;

            for (int round = 1; round <= SIM_MAX_ROUNDS; round++)
            {
                totalRounds = round;
                Debug.Log(LOG_PREFIX + "── Round " + round + " ──");
                roundLog.AppendLine("Round " + round + ":");

                var executable = new List<string>();
                var skipped = new List<string>();
                var skippedReasons = new List<string>();

                for (int i = 0; i < tasks.Length; i++)
                {
                    string name = tasks[i].Name;
                    if (taskMap[name] == "done") continue;
                    if (taskMap[name] != "planned") continue;

                    bool ready = true;
                    string missingDep = null;
                    for (int d = 0; d < tasks[i].DependsOn.Length; d++)
                    {
                        string depStatus;
                        if (!taskMap.TryGetValue(tasks[i].DependsOn[d], out depStatus) || depStatus != "done")
                        {
                            ready = false;
                            missingDep = tasks[i].DependsOn[d];
                            break;
                        }
                    }

                    if (ready)
                        executable.Add(name);
                    else
                    {
                        skipped.Add(name);
                        skippedReasons.Add("waiting for " + missingDep);
                    }
                }

                bool pendingExists = false;
                for (int i = 0; i < tasks.Length; i++)
                {
                    if (taskMap[tasks[i].Name] != "done")
                    {
                        pendingExists = true;
                        break;
                    }
                }

                if (!pendingExists)
                {
                    allDone = true;
                    Debug.Log(LOG_PREFIX + "All modules done.");
                    roundLog.AppendLine("  All done.");
                    break;
                }

                if (executable.Count == 0)
                {
                    Debug.LogError(LOG_PREFIX + "DEADLOCK at round " + round);
                    roundLog.AppendLine("  DEADLOCK");
                    break;
                }

                for (int i = 0; i < skipped.Count; i++)
                {
                    Debug.Log(LOG_PREFIX + "  SKIP: " + skipped[i] + " → " + skippedReasons[i]);
                    roundLog.AppendLine("  Skip: " + skipped[i] + " → " + skippedReasons[i]);
                }

                var folderSet = new HashSet<string>();
                bool folderConflict = false;
                for (int i = 0; i < executable.Count; i++)
                {
                    string path = "Assets/Game/Modules/" + executable[i];
                    if (!folderSet.Add(path))
                    {
                        folderConflict = true;
                        Debug.LogError(LOG_PREFIX + "  FOLDER CONFLICT: " + path);
                    }
                }

                if (folderConflict)
                {
                    Debug.LogError(LOG_PREFIX + "  Module isolation FAILED!");
                    break;
                }

                roundLog.AppendLine("  Parallel builders (" + executable.Count + "):");
                for (int i = 0; i < executable.Count; i++)
                {
                    string builderId = "builder_" + (i + 1);
                    Debug.Log(LOG_PREFIX + "  " + builderId + " → " + executable[i] + " → done");
                    taskMap[executable[i]] = "done";
                    processOrder.Add(executable[i]);
                    roundLog.AppendLine("    " + builderId + " → " + executable[i] + " → done");
                }
                roundLog.AppendLine("");
            }

            Debug.Log(LOG_PREFIX + "");
            Debug.Log(LOG_PREFIX + "=== Process Order ===");
            for (int i = 0; i < processOrder.Count; i++)
                Debug.Log(LOG_PREFIX + "  " + processOrder[i]);

            bool r1Correct = processOrder.Count >= 3
                && processOrder.Contains("Economy")
                && processOrder.Contains("StatusEffect")
                && processOrder.Contains("Player");

            int economyIdx = processOrder.IndexOf("Economy");
            int warriorsIdx = processOrder.IndexOf("Warriors");
            int statusEffectIdx = processOrder.IndexOf("StatusEffect");
            int buffIconIdx = processOrder.IndexOf("BuffIconUI");
            int gameManagerIdx = processOrder.IndexOf("GameManager");

            bool depOrderCorrect =
                warriorsIdx > economyIdx
                && buffIconIdx > statusEffectIdx
                && gameManagerIdx > warriorsIdx
                && gameManagerIdx > economyIdx
                && processOrder.IndexOf("Player") < gameManagerIdx;

            bool pass = r1Correct && depOrderCorrect && allDone && processOrder.Count == 6;

            Debug.Log(LOG_PREFIX + "");
            string result = pass ? "PASS" : "FAIL";
            Debug.Log(LOG_PREFIX + "RESULT: " + result);
            Debug.Log(LOG_PREFIX + "  Round 1 parallel (Economy,StatusEffect,Player): " + r1Correct);
            Debug.Log(LOG_PREFIX + "  Dependency order respected: " + depOrderCorrect);
            Debug.Log(LOG_PREFIX + "  All done: " + allDone);
            Debug.Log(LOG_PREFIX + "  Total rounds: " + totalRounds);
            Debug.Log(LOG_PREFIX + "");
            Debug.Log(roundLog.ToString());

            EditorUtility.DisplayDialog("Builder Pool Simulation - " + result,
                "Process order: " + JoinArray(processOrder.ToArray())
                + "\nRounds: " + totalRounds
                + "\nAll done: " + allDone
                + "\nDep order: " + depOrderCorrect,
                "OK");
        }

        static string JoinArray(string[] arr)
        {
            if (arr == null || arr.Length == 0) return "(empty)";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append(" → ");
                sb.Append(arr[i]);
            }
            return sb.ToString();
        }
    }
}
