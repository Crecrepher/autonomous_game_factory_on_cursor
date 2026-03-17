using System.Text.Json;

namespace FrameworkExporter.Config
{
    public static class ConfigLoader
    {
        const string DEFAULT_CONFIG_NAME = "export-config.json";

        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public static ExportConfig Load(string configPath)
        {
            if (string.IsNullOrEmpty(configPath))
                configPath = DEFAULT_CONFIG_NAME;

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[WARN] Config not found at '{configPath}'. Generating default...");
                ExportConfig fallback = CreateDefault();
                string json = JsonSerializer.Serialize(fallback, _jsonOptions);
                File.WriteAllText(configPath, json);
                Console.WriteLine($"[INFO] Default config written to '{configPath}'. Edit and re-run.");
                return fallback;
            }

            string raw = File.ReadAllText(configPath);
            ExportConfig config = JsonSerializer.Deserialize<ExportConfig>(raw, _jsonOptions);

            if (config == null)
                throw new InvalidOperationException($"Failed to deserialize config from '{configPath}'.");

            return config;
        }

        public static ExportConfig CreateDefault()
        {
            return new ExportConfig
            {
                SourceRoot = ".",
                ExportRoot = "./ExportPackage",
                DryRun = false,
                ExportMode = EExportMode.Framework,
                FrameworkPaths = new[]
                {
                    ".cursor/rules",
                    "Docs/ai",
                    "Assets/Editor/AI",
                    "Assets/Game/Modules/Template"
                },
                GameplayModulePaths = new[]
                {
                    "Assets/Projects/FortressSaga/Playable016/Scripts",
                    "Assets/Projects/FortressSaga/Playable016/Data",
                    "Assets/Game/Modules"
                },
                AdditionalAllowedPaths = Array.Empty<string>(),
                DeniedPaths = new[]
                {
                    "Library", "Temp", "Logs", "obj", "Build",
                    ".vs", ".idea", "UserSettings", "LunaTemp",
                    "Packages", ".vscode"
                },
                DeniedExtensions = new[]
                {
                    ".log", ".tmp", ".pdb", ".dll", ".exe",
                    ".so", ".dylib", ".mdb", ".cache"
                },
                RiskyExtensions = new[]
                {
                    ".unity", ".prefab", ".asmdef", ".asmref",
                    ".asset", ".meta", ".mat", ".shader", ".cginc",
                    ".compute", ".shadergraph", ".shadersubgraph"
                },
                RiskyWarning = true,
                IncludeMetaFiles = true,
                IncludeOnlyChangedFiles = false,
                ConfirmBeforeExport = true
            };
        }

        public static string Serialize(ExportConfig config)
        {
            return JsonSerializer.Serialize(config, _jsonOptions);
        }
    }
}
