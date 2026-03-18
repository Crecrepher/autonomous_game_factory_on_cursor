using System.Diagnostics;
using System.Text.Json;

namespace FrameworkExporter.Config
{
    public static class ConfigLoader
    {
        const string DEFAULT_CONFIG_NAME = "export-config.json";
        const string AUTO_SOURCE = "auto";
        const string UNITY_PROJECT_DIR = "unity_project";
        const string UNITY_ASSETS_DIR = "Assets";

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

            if (IsAutoSource(config.SourceRoot))
            {
                string resolved = ResolveUnityProjectRoot();
                Console.WriteLine($"[AUTO] Resolved Unity project: '{resolved}'");
                config.SourceRoot = resolved;
            }

            return config;
        }

        static bool IsAutoSource(string sourceRoot)
        {
            return string.IsNullOrWhiteSpace(sourceRoot)
                || string.Equals(sourceRoot, AUTO_SOURCE, StringComparison.OrdinalIgnoreCase)
                || string.Equals(sourceRoot, ".", StringComparison.Ordinal);
        }

        static string ResolveUnityProjectRoot()
        {
            string gitRoot = FindGitRoot();
            if (gitRoot == null)
                throw new InvalidOperationException("Cannot auto-detect: not inside a git repository. Set sourceRoot explicitly.");

            string unityProjectDir = Path.Combine(gitRoot, UNITY_PROJECT_DIR);
            if (!Directory.Exists(unityProjectDir))
                throw new DirectoryNotFoundException($"Cannot auto-detect: '{UNITY_PROJECT_DIR}/' not found under git root '{gitRoot}'.");

            string[] candidates = Directory.GetDirectories(unityProjectDir);
            for (int i = 0; i < candidates.Length; i++)
            {
                string assetsPath = Path.Combine(candidates[i], UNITY_ASSETS_DIR);
                if (Directory.Exists(assetsPath))
                    return candidates[i];
            }

            throw new DirectoryNotFoundException(
                $"Cannot auto-detect: no Unity project (with Assets/ folder) found under '{unityProjectDir}'.");
        }

        static string FindGitRoot()
        {
            ProcessStartInfo psi = new()
            {
                FileName = "git",
                Arguments = "rev-parse --show-toplevel",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(psi);
            if (process == null)
                return null;

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : null;
        }

        public static ExportConfig CreateDefault()
        {
            return new ExportConfig
            {
                SourceRoot = AUTO_SOURCE,
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
