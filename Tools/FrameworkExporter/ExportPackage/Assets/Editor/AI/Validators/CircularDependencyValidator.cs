using System.IO;

namespace Game.Editor.AI
{
    public class CircularDependencyValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "CircularDependency";

        public int Validate(ValidationReport report)
        {
            string registryPath = DependencyGraphBuilder.GetRegistryPath();
            if (!File.Exists(registryPath))
            {
                report.AddWarning(VALIDATOR_NAME, "MODULE_REGISTRY.yaml not found — skipping circular dependency check", null);
                return 0;
            }

            DependencyGraphBuilder.RegistryModule[] modules =
                DependencyGraphBuilder.ParseModuleRegistry(File.ReadAllLines(registryPath));

            string cycleChain;
            if (DependencyGraphBuilder.DetectCycle(modules, out cycleChain))
            {
                report.AddError(VALIDATOR_NAME,
                    "Circular dependency detected: " + cycleChain,
                    "MODULE_REGISTRY.yaml");
            }

            string[] sorted = DependencyGraphBuilder.TopologicalSort(modules);
            if (sorted.Length < modules.Length)
            {
                int missing = modules.Length - sorted.Length;
                report.AddError(VALIDATOR_NAME,
                    "Topological sort incomplete: " + missing + " module(s) could not be ordered. Possible hidden cycle or missing module.",
                    "MODULE_REGISTRY.yaml");
            }

            return modules.Length;
        }
    }
}
