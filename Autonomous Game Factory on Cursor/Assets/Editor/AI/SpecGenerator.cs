using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class SpecGenerator
    {
        const string LOG_PREFIX = "[SpecGenerator] ";
        const string SPECS_DIR_RELATIVE = "docs/ai/generated_specs";
        const string PLANS_DIR_RELATIVE = "docs/ai/plans";

        public struct ModuleSpec
        {
            public string ModuleName;
            public string FeatureGroup;
            public string Purpose;
            public string[] PublicAPI;
            public string[] RuntimeResponsibilities;
            public string[] ConfigNeeds;
            public string FactoryResponsibility;
            public string[] TestScope;
            public string[] DependencyConstraints;
            public string[] MustNotImplement;
        }

        public static ModuleSpec CreateSpec(FeatureDecomposer.DecomposedModule module)
        {
            ModuleSpec spec = new ModuleSpec();
            spec.ModuleName = module.Name;
            spec.FeatureGroup = module.FeatureGroup;
            spec.Purpose = module.Responsibility;

            spec.PublicAPI = new string[]
            {
                "I" + module.Name + " interface",
                "Init() — initialization",
                "Tick(float deltaTime) — per-frame update"
            };

            spec.RuntimeResponsibilities = new string[]
            {
                "Core business logic for " + module.Name,
                "State management",
                "Event emission for state changes"
            };

            spec.ConfigNeeds = new string[]
            {
                module.Name + "Config : ScriptableObject",
                "Domain-specific configuration fields"
            };

            spec.FactoryResponsibility = "static " + module.Name + "Factory.CreateRuntime(" + module.Name + "Config) → I" + module.Name;

            spec.TestScope = new string[]
            {
                "Factory creates non-null runtime",
                "Init → Tick does not throw",
                "Domain-specific behavior tests"
            };

            spec.DependencyConstraints = module.Dependencies;

            spec.MustNotImplement = new string[]
            {
                "MonoBehaviour logic (only in Bootstrap)",
                "Direct references to other modules' Runtime classes",
                "GC-allocating patterns (coroutines, lambdas, LINQ, foreach)",
                "GetComponent at runtime",
                "Magic numbers (use const)"
            };

            return spec;
        }

        public static string WriteSpec(ModuleSpec spec)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string specsDir = Path.Combine(projectRoot, SPECS_DIR_RELATIVE);
            if (!Directory.Exists(specsDir))
                Directory.CreateDirectory(specsDir);

            string fileName = spec.ModuleName + "_SPEC.md";
            string filePath = Path.Combine(specsDir, fileName);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Module Spec: " + spec.ModuleName);
            sb.AppendLine("");
            sb.AppendLine("Feature Group: " + spec.FeatureGroup);
            sb.AppendLine("");
            sb.AppendLine("---");
            sb.AppendLine("");

            sb.AppendLine("## Purpose");
            sb.AppendLine("");
            sb.AppendLine(spec.Purpose);
            sb.AppendLine("");

            sb.AppendLine("## Public API");
            sb.AppendLine("");
            for (int i = 0; i < spec.PublicAPI.Length; i++)
                sb.AppendLine("- " + spec.PublicAPI[i]);
            sb.AppendLine("");

            sb.AppendLine("## Runtime Responsibilities");
            sb.AppendLine("");
            for (int i = 0; i < spec.RuntimeResponsibilities.Length; i++)
                sb.AppendLine("- " + spec.RuntimeResponsibilities[i]);
            sb.AppendLine("");

            sb.AppendLine("## Config Needs");
            sb.AppendLine("");
            for (int i = 0; i < spec.ConfigNeeds.Length; i++)
                sb.AppendLine("- " + spec.ConfigNeeds[i]);
            sb.AppendLine("");

            sb.AppendLine("## Factory Responsibility");
            sb.AppendLine("");
            sb.AppendLine(spec.FactoryResponsibility);
            sb.AppendLine("");

            sb.AppendLine("## Test Scope");
            sb.AppendLine("");
            for (int i = 0; i < spec.TestScope.Length; i++)
                sb.AppendLine("- " + spec.TestScope[i]);
            sb.AppendLine("");

            sb.AppendLine("## Dependency Constraints");
            sb.AppendLine("");
            if (spec.DependencyConstraints != null && spec.DependencyConstraints.Length > 0)
            {
                for (int i = 0; i < spec.DependencyConstraints.Length; i++)
                    sb.AppendLine("- " + spec.DependencyConstraints[i]);
            }
            else
            {
                sb.AppendLine("- None (independent module)");
            }
            sb.AppendLine("");

            sb.AppendLine("## Must NOT Implement");
            sb.AppendLine("");
            for (int i = 0; i < spec.MustNotImplement.Length; i++)
                sb.AppendLine("- " + spec.MustNotImplement[i]);
            sb.AppendLine("");

            File.WriteAllText(filePath, sb.ToString());
            Debug.Log(LOG_PREFIX + "Spec written: " + filePath);
            return filePath;
        }

        public static string[] WriteAllSpecs(FeatureDecomposer.DecomposedModule[] modules)
        {
            string[] paths = new string[modules.Length];
            for (int i = 0; i < modules.Length; i++)
            {
                ModuleSpec spec = CreateSpec(modules[i]);
                paths[i] = WriteSpec(spec);
            }
            Debug.Log(LOG_PREFIX + modules.Length + " specs generated");
            return paths;
        }
    }
}
