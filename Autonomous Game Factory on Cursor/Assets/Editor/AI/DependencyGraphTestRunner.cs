using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class DependencyGraphTestRunner
    {
        const string MENU_PATH = "Tools/AI/Run Dependency Graph Tests";
        const string LOG_PREFIX = "[DependencyGraphTest] ";
        const int TOTAL_TESTS = 4;

        [MenuItem(MENU_PATH)]
        public static void RunAllTests()
        {
            int passed = 0;

            Debug.Log(LOG_PREFIX + "=== Dependency Graph Tests Start ===");

            if (Test1_NormalDependencyChain()) passed++;
            if (Test2_UndeclaredDependency()) passed++;
            if (Test3_MissingModuleReference()) passed++;
            if (Test4_CircularDependency()) passed++;

            string result = passed == TOTAL_TESTS ? "ALL PASSED" : "FAILED (" + passed + "/" + TOTAL_TESTS + ")";
            Debug.Log(LOG_PREFIX + "=== " + result + " ===");

            EditorUtility.DisplayDialog("Dependency Graph Tests", result, "OK");
        }

        // ──────────────────────────────────────────────
        // Test 1: 정상 의존성 체인 StatusEffect → BuffSystem → BuffIconUI
        // ──────────────────────────────────────────────
        static bool Test1_NormalDependencyChain()
        {
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
                "    status: done",
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

            // 순환 없어야 함
            string cycleChain;
            bool hasCycle = DependencyGraphBuilder.DetectCycle(modules, out cycleChain);
            if (hasCycle)
            {
                Debug.LogError(LOG_PREFIX + "Test1 FAIL: Unexpected cycle detected: " + cycleChain);
                return false;
            }

            // 토폴로지 정렬 순서 확인
            string[] sorted = DependencyGraphBuilder.TopologicalSort(modules);
            if (sorted.Length != 3)
            {
                Debug.LogError(LOG_PREFIX + "Test1 FAIL: Expected 3 sorted modules, got " + sorted.Length);
                return false;
            }

            int idxStatus = -1, idxBuff = -1, idxIcon = -1;
            for (int i = 0; i < sorted.Length; i++)
            {
                if (sorted[i] == "StatusEffect") idxStatus = i;
                if (sorted[i] == "BuffSystem") idxBuff = i;
                if (sorted[i] == "BuffIconUI") idxIcon = i;
            }

            if (idxStatus >= idxBuff || idxBuff >= idxIcon)
            {
                Debug.LogError(LOG_PREFIX + "Test1 FAIL: Wrong order. StatusEffect=" + idxStatus + " BuffSystem=" + idxBuff + " BuffIconUI=" + idxIcon);
                return false;
            }

            // 실행 가능 모듈 확인: StatusEffect done이면 BuffSystem만 실행 가능
            var graph = BuildTestGraph(modules, tasks);
            string[] executable = DependencyGraphBuilder.GetExecutableModules(graph);
            if (executable.Length != 1 || executable[0] != "BuffSystem")
            {
                Debug.LogError(LOG_PREFIX + "Test1 FAIL: Expected only BuffSystem executable, got: " + JoinArray(executable));
                return false;
            }

            // BuffIconUI는 blocked
            string[] blocked = DependencyGraphBuilder.GetBlockedModules(graph);
            bool buffIconBlocked = false;
            for (int i = 0; i < blocked.Length; i++)
            {
                if (blocked[i] == "BuffIconUI") buffIconBlocked = true;
            }
            if (!buffIconBlocked)
            {
                Debug.LogError(LOG_PREFIX + "Test1 FAIL: BuffIconUI should be blocked");
                return false;
            }

            Debug.Log(LOG_PREFIX + "Test1 PASS: Normal dependency chain (StatusEffect → BuffSystem → BuffIconUI)");
            return true;
        }

        // ──────────────────────────────────────────────
        // Test 2: 선언 안 된 의존성 (TASK_QUEUE에는 있지만 REGISTRY에 없음)
        // ──────────────────────────────────────────────
        static bool Test2_UndeclaredDependency()
        {
            string[] registryLines = new string[]
            {
                "modules:",
                "  - name: StatusEffect",
                "    path: Assets/Game/Modules/StatusEffect",
                "    dependencies: [UnityEngine]",
                "",
                "  - name: BuffSystem",
                "    path: Assets/Game/Modules/BuffSystem",
                "    dependencies: [UnityEngine]",
            };

            string[] queueLines = new string[]
            {
                "modules:",
                "  - name: StatusEffect",
                "    status: done",
                "    depends_on: []",
                "",
                "  - name: BuffSystem",
                "    status: planned",
                "    depends_on: [StatusEffect]",
            };

            DependencyGraphBuilder.RegistryModule[] modules = DependencyGraphBuilder.ParseModuleRegistry(registryLines);
            DependencyGraphBuilder.TaskEntry[] tasks = DependencyGraphBuilder.ParseTaskQueue(queueLines);
            var graph = BuildTestGraph(modules, tasks);

            ValidationReport report = new ValidationReport();
            ValidateTaskQueueConsistency(graph, report);

            bool foundWarning = false;
            for (int i = 0; i < report.Entries.Count; i++)
            {
                if (report.Entries[i].Message.Contains("BuffSystem") &&
                    report.Entries[i].Message.Contains("StatusEffect") &&
                    report.Entries[i].Message.Contains("not listed in MODULE_REGISTRY"))
                {
                    foundWarning = true;
                    break;
                }
            }

            if (!foundWarning)
            {
                Debug.LogError(LOG_PREFIX + "Test2 FAIL: Should warn about TASK_QUEUE depends_on 'StatusEffect' not in REGISTRY dependencies");
                LogReportEntries(report);
                return false;
            }

            Debug.Log(LOG_PREFIX + "Test2 PASS: Undeclared dependency detected (TASK_QUEUE has dep not in REGISTRY)");
            return true;
        }

        // ──────────────────────────────────────────────
        // Test 3: 존재하지 않는 모듈 참조
        // ──────────────────────────────────────────────
        static bool Test3_MissingModuleReference()
        {
            string[] registryLines = new string[]
            {
                "modules:",
                "  - name: StatusEffect",
                "    path: Assets/Game/Modules/StatusEffect",
                "    dependencies: [UnityEngine, GhostModule]",
            };

            DependencyGraphBuilder.RegistryModule[] modules = DependencyGraphBuilder.ParseModuleRegistry(registryLines);
            var graph = BuildTestGraph(modules, new DependencyGraphBuilder.TaskEntry[0]);

            ValidationReport report = new ValidationReport();
            ValidateRegistryDependencies(graph, report);

            bool foundError = false;
            for (int i = 0; i < report.Entries.Count; i++)
            {
                if (report.Entries[i].Severity == ValidationReport.SEVERITY_ERROR &&
                    report.Entries[i].Message.Contains("GhostModule") &&
                    report.Entries[i].Message.Contains("not registered"))
                {
                    foundError = true;
                    break;
                }
            }

            if (!foundError)
            {
                Debug.LogError(LOG_PREFIX + "Test3 FAIL: Should error about 'GhostModule' not registered");
                LogReportEntries(report);
                return false;
            }

            Debug.Log(LOG_PREFIX + "Test3 PASS: Missing module reference detected (GhostModule)");
            return true;
        }

        // ──────────────────────────────────────────────
        // Test 4: 순환 의존성 A → B → C → A
        // ──────────────────────────────────────────────
        static bool Test4_CircularDependency()
        {
            string[] registryLines = new string[]
            {
                "modules:",
                "  - name: ModuleA",
                "    path: Assets/Game/Modules/ModuleA",
                "    dependencies: [UnityEngine, ModuleC]",
                "",
                "  - name: ModuleB",
                "    path: Assets/Game/Modules/ModuleB",
                "    dependencies: [UnityEngine, ModuleA]",
                "",
                "  - name: ModuleC",
                "    path: Assets/Game/Modules/ModuleC",
                "    dependencies: [UnityEngine, ModuleB]",
            };

            DependencyGraphBuilder.RegistryModule[] modules = DependencyGraphBuilder.ParseModuleRegistry(registryLines);

            string cycleChain;
            bool hasCycle = DependencyGraphBuilder.DetectCycle(modules, out cycleChain);

            if (!hasCycle)
            {
                Debug.LogError(LOG_PREFIX + "Test4 FAIL: Should detect circular dependency A → B → C → A");
                return false;
            }

            if (string.IsNullOrEmpty(cycleChain))
            {
                Debug.LogError(LOG_PREFIX + "Test4 FAIL: Cycle detected but chain string is empty");
                return false;
            }

            // 토폴로지 정렬도 불완전해야 함
            string[] sorted = DependencyGraphBuilder.TopologicalSort(modules);
            if (sorted.Length >= modules.Length)
            {
                Debug.LogError(LOG_PREFIX + "Test4 FAIL: Topological sort should be incomplete with cycle, got " + sorted.Length + "/" + modules.Length);
                return false;
            }

            Debug.Log(LOG_PREFIX + "Test4 PASS: Circular dependency detected: " + cycleChain);
            return true;
        }

        // ──────────────────────────────────────────────
        // Helper: 테스트용 그래프 빌드 (파일 I/O 없이)
        // ──────────────────────────────────────────────
        static DependencyGraphBuilder.DependencyGraph BuildTestGraph(
            DependencyGraphBuilder.RegistryModule[] modules,
            DependencyGraphBuilder.TaskEntry[] tasks)
        {
            DependencyGraphBuilder.DependencyGraph graph = new DependencyGraphBuilder.DependencyGraph();
            graph.Modules = modules;
            graph.ModuleMap = new System.Collections.Generic.Dictionary<string, DependencyGraphBuilder.RegistryModule>(modules.Length);
            for (int i = 0; i < modules.Length; i++)
                graph.ModuleMap[modules[i].Name] = modules[i];

            graph.Tasks = tasks;
            graph.TaskMap = new System.Collections.Generic.Dictionary<string, DependencyGraphBuilder.TaskEntry>(tasks.Length);
            for (int i = 0; i < tasks.Length; i++)
                graph.TaskMap[tasks[i].Name] = tasks[i];

            return graph;
        }

        // DependencyValidator의 로직을 인라인으로 재현 (파일 I/O 없이 테스트)
        static void ValidateRegistryDependencies(DependencyGraphBuilder.DependencyGraph graph, ValidationReport report)
        {
            for (int i = 0; i < graph.Modules.Length; i++)
            {
                DependencyGraphBuilder.RegistryModule module = graph.Modules[i];
                for (int d = 0; d < module.Dependencies.Length; d++)
                {
                    string dep = module.Dependencies[d];
                    if (dep == "UnityEngine" || dep == "System")
                        continue;
                    if (!graph.ModuleMap.ContainsKey(dep))
                    {
                        report.AddError("Dependency",
                            "Module '" + module.Name + "' depends on '" + dep + "' which is not registered in MODULE_REGISTRY.yaml",
                            module.Path);
                    }
                }
            }
        }

        static void ValidateTaskQueueConsistency(DependencyGraphBuilder.DependencyGraph graph, ValidationReport report)
        {
            for (int i = 0; i < graph.Tasks.Length; i++)
            {
                DependencyGraphBuilder.TaskEntry task = graph.Tasks[i];
                if (!graph.ModuleMap.ContainsKey(task.Name))
                {
                    report.AddError("Dependency",
                        "Task '" + task.Name + "' in TASK_QUEUE.yaml has no matching entry in MODULE_REGISTRY.yaml",
                        "TASK_QUEUE.yaml");
                    continue;
                }

                DependencyGraphBuilder.RegistryModule registryModule = graph.ModuleMap[task.Name];
                var registryModuleDeps = new System.Collections.Generic.HashSet<string>();
                for (int d = 0; d < registryModule.Dependencies.Length; d++)
                {
                    string dep = registryModule.Dependencies[d];
                    if (dep != "UnityEngine" && dep != "System")
                        registryModuleDeps.Add(dep);
                }

                for (int d = 0; d < task.DependsOn.Length; d++)
                {
                    string taskDep = task.DependsOn[d];
                    if (!graph.ModuleMap.ContainsKey(taskDep))
                    {
                        report.AddError("Dependency",
                            "Task '" + task.Name + "' depends on '" + taskDep + "' which is not registered in MODULE_REGISTRY.yaml",
                            "TASK_QUEUE.yaml");
                    }
                    if (!registryModuleDeps.Contains(taskDep))
                    {
                        report.AddWarning("Dependency",
                            "Task '" + task.Name + "' has depends_on '" + taskDep + "' in TASK_QUEUE but this is not listed in MODULE_REGISTRY.yaml dependencies",
                            "TASK_QUEUE.yaml");
                    }
                }
            }
        }

        static string JoinArray(string[] arr)
        {
            if (arr == null || arr.Length == 0) return "(empty)";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(arr[i]);
            }
            return sb.ToString();
        }

        static void LogReportEntries(ValidationReport report)
        {
            for (int i = 0; i < report.Entries.Count; i++)
            {
                ValidationEntry e = report.Entries[i];
                Debug.Log(LOG_PREFIX + "  [" + e.Severity + "] " + e.ValidatorName + ": " + e.Message);
            }
        }
    }
}
