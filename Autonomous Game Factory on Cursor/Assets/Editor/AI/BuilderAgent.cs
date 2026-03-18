using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Editor.AI
{
    public class BuilderAgent
    {
        const string LOG_PREFIX = "[BuilderAgent] ";

        static readonly string[] FORBIDDEN_EDIT_PREFIXES = new string[]
        {
            "Assets/Editor/AI/",
            "Assets/Game/Core/",
            "Assets/Game/Modules/Template/",
            ".cursor/rules/"
        };

        static readonly string[] FORBIDDEN_SHARED_FILES = new string[]
        {
            "docs/ai/MODULE_REGISTRY.yaml",
            "docs/ai/ORCHESTRATION_RULES.md",
            "docs/ai/AGENT_ROLES.md",
            "docs/ai/MODULE_TEMPLATES.md"
        };

        static readonly string[] REQUIRED_MODULE_FILES = new string[]
        {
            "I{0}.cs",
            "{0}Runtime.cs",
            "{0}Config.cs",
            "{0}Factory.cs",
            "{0}Bootstrap.cs",
            "Tests/Editor/{0}Tests.cs"
        };

        readonly string _builderId;
        readonly string _assignedModule;
        readonly string _assignedModulePath;
        readonly string[] _moduleDependencies;
        bool _claimed;
        bool _completed;

        public string BuilderId => _builderId;
        public string AssignedModule => _assignedModule;
        public string AssignedModulePath => _assignedModulePath;
        public bool IsClaimed => _claimed;
        public bool IsCompleted => _completed;

        public BuilderAgent(string builderId, string assignedModule, string assignedModulePath, string[] moduleDependencies)
        {
            _builderId = builderId;
            _assignedModule = assignedModule;
            _assignedModulePath = assignedModulePath;
            _moduleDependencies = moduleDependencies;
            _claimed = false;
            _completed = false;
        }

        public bool ClaimTask(DependencyGraphBuilder.DependencyGraph graph)
        {
            if (_claimed)
            {
                Debug.LogWarning(LOG_PREFIX + _builderId + " already claimed " + _assignedModule);
                return false;
            }

            if (!DependencyGraphBuilder.IsModuleClaimable(graph, _assignedModule))
            {
                Debug.LogError(LOG_PREFIX + _builderId + " cannot claim " + _assignedModule + " — not claimable");
                return false;
            }

            _claimed = true;
            Debug.Log(LOG_PREFIX + _builderId + " claimed " + _assignedModule + " (path: " + _assignedModulePath + ")");
            return true;
        }

        public void ReleaseTask()
        {
            _claimed = false;
            _completed = false;
            Debug.Log(LOG_PREFIX + _builderId + " released " + _assignedModule);
        }

        public bool ValidateFileEdit(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string normalizedPath = filePath.Replace("\\", "/");

            for (int i = 0; i < FORBIDDEN_EDIT_PREFIXES.Length; i++)
            {
                if (normalizedPath.StartsWith(FORBIDDEN_EDIT_PREFIXES[i]))
                {
                    Debug.LogError(LOG_PREFIX + _builderId + " BLOCKED edit to forbidden path: " + filePath);
                    return false;
                }
            }

            for (int i = 0; i < FORBIDDEN_SHARED_FILES.Length; i++)
            {
                if (normalizedPath.EndsWith(FORBIDDEN_SHARED_FILES[i]) ||
                    normalizedPath.Contains("/" + FORBIDDEN_SHARED_FILES[i]))
                {
                    Debug.LogError(LOG_PREFIX + _builderId + " BLOCKED edit to shared file: " + filePath);
                    return false;
                }
            }

            if (!normalizedPath.StartsWith(_assignedModulePath + "/") &&
                normalizedPath != _assignedModulePath &&
                !normalizedPath.StartsWith("Assets/Game/Shared/"))
            {
                Debug.LogError(LOG_PREFIX + _builderId + " BLOCKED edit outside assigned module: " + filePath + " (assigned: " + _assignedModulePath + ")");
                return false;
            }

            return true;
        }

        public string[] GetRequiredFiles()
        {
            string[] result = new string[REQUIRED_MODULE_FILES.Length];
            for (int i = 0; i < REQUIRED_MODULE_FILES.Length; i++)
            {
                string fileName = string.Format(REQUIRED_MODULE_FILES[i], _assignedModule);
                result[i] = _assignedModulePath + "/" + fileName;
            }
            return result;
        }

        public bool CompleteTask()
        {
            if (!_claimed)
            {
                Debug.LogError(LOG_PREFIX + _builderId + " cannot complete unclaimed task " + _assignedModule);
                return false;
            }

            _completed = true;
            Debug.Log(LOG_PREFIX + _builderId + " completed " + _assignedModule + " → status: review");
            return true;
        }

        public string GetSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Builder: ").Append(_builderId);
            sb.Append(" | Module: ").Append(_assignedModule);
            sb.Append(" | Path: ").Append(_assignedModulePath);
            sb.Append(" | Claimed: ").Append(_claimed);
            sb.Append(" | Completed: ").Append(_completed);
            return sb.ToString();
        }

        public static bool ValidateNoDuplicateClaims(BuilderAgent[] agents)
        {
            var claimedModules = new HashSet<string>();
            var claimedPaths = new HashSet<string>();

            for (int i = 0; i < agents.Length; i++)
            {
                if (!agents[i].IsClaimed)
                    continue;

                if (!claimedModules.Add(agents[i].AssignedModule))
                {
                    Debug.LogError(LOG_PREFIX + "DUPLICATE CLAIM: " + agents[i].AssignedModule + " claimed by multiple builders!");
                    return false;
                }

                if (!claimedPaths.Add(agents[i].AssignedModulePath))
                {
                    Debug.LogError(LOG_PREFIX + "DUPLICATE PATH: " + agents[i].AssignedModulePath + " claimed by multiple builders!");
                    return false;
                }
            }

            return true;
        }

        public static BuilderAgent[] CreateBuilderPool(DependencyGraphBuilder.BuilderAssignment[] assignments, DependencyGraphBuilder.DependencyGraph graph)
        {
            BuilderAgent[] agents = new BuilderAgent[assignments.Length];

            for (int i = 0; i < assignments.Length; i++)
            {
                string moduleName = assignments[i].ModuleName;
                string[] deps = new string[0];

                DependencyGraphBuilder.RegistryModule regModule;
                if (graph.ModuleMap.TryGetValue(moduleName, out regModule))
                    deps = regModule.Dependencies;

                agents[i] = new BuilderAgent(
                    assignments[i].BuilderId,
                    moduleName,
                    assignments[i].ModulePath,
                    deps
                );
            }

            return agents;
        }
    }
}
