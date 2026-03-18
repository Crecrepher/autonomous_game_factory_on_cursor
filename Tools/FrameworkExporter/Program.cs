using FrameworkExporter.Config;
using FrameworkExporter.Exporter;
using FrameworkExporter.Report;
using FrameworkExporter.Scanner;

namespace FrameworkExporter
{
    internal static class Program
    {
        const string VERSION = "2.0.0";
        const string ARG_CONFIG = "--config";
        const string ARG_DRY_RUN = "--dry-run";
        const string ARG_SOURCE = "--source";
        const string ARG_OUTPUT = "--output";
        const string ARG_HELP = "--help";
        const string ARG_INIT = "--init";
        const string ARG_ALLOW = "--allow";
        const string ARG_MODE = "--mode";
        const string ARG_CHANGED_ONLY = "--changed-only";
        const string ARG_YES = "--yes";

        const string MODE_FRAMEWORK = "framework";
        const string MODE_FULL = "full";

        static int Main(string[] args)
        {
            Console.WriteLine($"FrameworkExporter v{VERSION}");
            Console.WriteLine(new string('=', 50));

            if (HasArg(args, ARG_HELP))
            {
                PrintUsage();
                return 0;
            }

            if (HasArg(args, ARG_INIT))
                return HandleInit(args);

            try
            {
                return RunExport(args);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FATAL] {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static int RunExport(string[] args)
        {
            string configPath = GetArgValue(args, ARG_CONFIG) ?? "export-config.json";
            ExportConfig config = ConfigLoader.Load(configPath);

            ApplyCliOverrides(args, config);

            string sourceRoot = Path.GetFullPath(config.SourceRoot);
            string exportRoot = Path.GetFullPath(config.ExportRoot);

            Console.WriteLine();
            Console.WriteLine($"  Source:       {sourceRoot}");
            Console.WriteLine($"  Output:       {exportRoot}");
            Console.WriteLine($"  Mode:         {config.ExportMode}");
            Console.WriteLine($"  Dry run:      {config.DryRun}");
            Console.WriteLine($"  Changed only: {config.IncludeOnlyChangedFiles}");
            Console.WriteLine();

            string[] effectivePaths = config.GetEffectiveAllowedPaths();
            Console.WriteLine($"[INFO] Allowed paths ({effectivePaths.Length}):");
            for (int i = 0; i < effectivePaths.Length; i++)
            {
                Console.WriteLine($"  + {effectivePaths[i]}");
            }
            Console.WriteLine();

            Console.WriteLine("[STEP 1/4] Scanning files...");
            FileScanner scanner = new(config);
            ScanResult scanResult = scanner.Scan();

            PrintScanSummary(scanResult);

            if (config.RiskyWarning && scanResult.RiskyFiles.Count > 0)
                PrintRiskyWarning(scanResult);

            if (scanResult.UnknownFiles.Count > 0)
                PrintUnknownWarning(scanResult);

            if (scanResult.IncludedFiles.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[DONE] No files to export. Check your config paths.");
                Console.ResetColor();
                return 0;
            }

            if (!config.DryRun && config.ConfirmBeforeExport && !HasArg(args, ARG_YES))
            {
                if (!PromptConfirmation(scanResult))
                {
                    Console.WriteLine("[ABORT] Export cancelled by user.");
                    return 0;
                }
            }

            Console.WriteLine("[STEP 2/4] Exporting files...");
            FileExporter exporter = new(config);
            ExportResult exportResult = exporter.Export(scanResult);
            Console.WriteLine($"  Exported {exportResult.ExportedFiles.Count} files ({FormatSize(exportResult.TotalSizeBytes)})");
            PrintErrors(exportResult);
            Console.WriteLine();

            Console.WriteLine("[STEP 3/4] Writing manifest...");
            ManifestGenerator.Generate(exportRoot, scanResult, exportResult, config);
            Console.WriteLine();

            Console.WriteLine("[STEP 4/4] Writing report...");
            ReportGenerator.Generate(exportRoot, scanResult, exportResult, config);
            Console.WriteLine();

            PrintFinalStatus(config, exportRoot, exportResult);
            return exportResult.Errors.Count > 0 ? 1 : 0;
        }

        static void PrintScanSummary(ScanResult scan)
        {
            Console.WriteLine();
            Console.WriteLine("  +------------------------------+---------+");
            Console.WriteLine("  | Category                     | Count   |");
            Console.WriteLine("  +------------------------------+---------+");
            Console.WriteLine($"  | Total scanned                | {scan.TotalScanned,7} |");
            Console.WriteLine($"  | Included (will export)       | {scan.IncludedFiles.Count,7} |");
            Console.WriteLine($"  | Risky (included, flagged)    | {scan.RiskyFiles.Count,7} |");
            Console.WriteLine($"  | Unknown (included, flagged)  | {scan.UnknownFiles.Count,7} |");
            Console.WriteLine($"  | Skipped (outside paths)      | {scan.SkippedFiles.Count,7} |");
            Console.WriteLine($"  | Denied (blocked by rules)    | {scan.DeniedFiles.Count,7} |");
            Console.WriteLine($"  | Git-filtered (not changed)   | {scan.GitFilteredFiles.Count,7} |");
            Console.WriteLine("  +------------------------------+---------+");
            Console.WriteLine();
        }

        static void PrintRiskyWarning(ScanResult scan)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARN] Risky files will be exported:");
            int count = Math.Min(scan.RiskyFiles.Count, 15);
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine($"  ! {scan.RiskyFiles[i].RelativePath}");
            }
            if (scan.RiskyFiles.Count > 15)
                Console.WriteLine($"  ... and {scan.RiskyFiles.Count - 15} more (see report)");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void PrintUnknownWarning(ScanResult scan)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[WARN] Unknown file types detected (will NOT be exported):");
            int count = Math.Min(scan.UnknownFiles.Count, 10);
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine($"  ? {scan.UnknownFiles[i].RelativePath}");
            }
            if (scan.UnknownFiles.Count > 10)
                Console.WriteLine($"  ... and {scan.UnknownFiles.Count - 10} more (see report)");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void PrintErrors(ExportResult result)
        {
            if (result.Errors.Count == 0)
                return;

            Console.ForegroundColor = ConsoleColor.Red;
            for (int i = 0; i < result.Errors.Count; i++)
            {
                Console.WriteLine($"  [ERROR] {result.Errors[i]}");
            }
            Console.ResetColor();
        }

        static void PrintFinalStatus(ExportConfig config, string exportRoot, ExportResult result)
        {
            if (config.DryRun)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[DONE] Dry run complete. No files were copied.");
                Console.WriteLine("       Remove --dry-run or set dryRun:false in config to export for real.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[DONE] Export complete!");
                Console.WriteLine($"       Output:   {exportRoot}");
                Console.WriteLine($"       Files:    {result.ExportedFiles.Count}");
                Console.WriteLine($"       Size:     {FormatSize(result.TotalSizeBytes)}");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("  Next steps:");
                Console.WriteLine("  1. Review ExportPackage/export_report.md");
                Console.WriteLine("  2. Review ExportPackage/manifest.json");
                Console.WriteLine("  3. Copy contents to your original Git project:");
                Console.WriteLine($"       xcopy /E /Y \"{exportRoot}\\*\" \"<your-original-project>\\\"");
                Console.WriteLine("  4. Run git diff in the original project to verify changes");
                Console.WriteLine("  5. Commit if everything looks good");
            }
        }

        static bool PromptConfirmation(ScanResult scan)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[CONFIRM] Export {scan.IncludedFiles.Count} files");
            if (scan.RiskyFiles.Count > 0)
                Console.Write($" ({scan.RiskyFiles.Count} risky)");
            Console.Write("? [y/N] ");
            Console.ResetColor();

            string input = Console.ReadLine();
            return !string.IsNullOrEmpty(input) &&
                   (input[0] == 'y' || input[0] == 'Y');
        }

        static int HandleInit(string[] args)
        {
            string configPath = GetArgValue(args, ARG_CONFIG) ?? "export-config.json";
            if (File.Exists(configPath))
            {
                Console.WriteLine($"[WARN] Config already exists at '{configPath}'. Delete it first to regenerate.");
                return 1;
            }

            ExportConfig defaultConfig = ConfigLoader.CreateDefault();
            string json = ConfigLoader.Serialize(defaultConfig);
            File.WriteAllText(configPath, json);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[DONE] Default config created at '{configPath}'");
            Console.ResetColor();
            return 0;
        }

        static void ApplyCliOverrides(string[] args, ExportConfig config)
        {
            if (HasArg(args, ARG_DRY_RUN))
                config.DryRun = true;

            if (HasArg(args, ARG_CHANGED_ONLY))
                config.IncludeOnlyChangedFiles = true;

            string sourceOverride = GetArgValue(args, ARG_SOURCE);
            if (sourceOverride != null)
                config.SourceRoot = sourceOverride;

            string outputOverride = GetArgValue(args, ARG_OUTPUT);
            if (outputOverride != null)
                config.ExportRoot = outputOverride;

            string modeOverride = GetArgValue(args, ARG_MODE);
            if (modeOverride != null)
            {
                if (string.Equals(modeOverride, MODE_FULL, StringComparison.OrdinalIgnoreCase))
                    config.ExportMode = EExportMode.Full;
                else if (string.Equals(modeOverride, MODE_FRAMEWORK, StringComparison.OrdinalIgnoreCase))
                    config.ExportMode = EExportMode.Framework;
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[WARN] Unknown mode '{modeOverride}'. Using '{config.ExportMode}'.");
                    Console.ResetColor();
                }
            }

            string[] extraAllowed = GetAllArgValues(args, ARG_ALLOW);
            if (extraAllowed.Length > 0)
            {
                string[] merged = new string[config.AdditionalAllowedPaths.Length + extraAllowed.Length];
                Array.Copy(config.AdditionalAllowedPaths, 0, merged, 0, config.AdditionalAllowedPaths.Length);
                Array.Copy(extraAllowed, 0, merged, config.AdditionalAllowedPaths.Length, extraAllowed.Length);
                config.AdditionalAllowedPaths = merged;
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: FrameworkExporter [options]");
            Console.WriteLine();
            Console.WriteLine("Modes:");
            Console.WriteLine("  framework (default)  Export only framework/tooling files");
            Console.WriteLine("  full                 Export framework + gameplay module files");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine($"  {ARG_HELP}                Show this help message");
            Console.WriteLine($"  {ARG_INIT}                Create default export-config.json");
            Console.WriteLine($"  {ARG_CONFIG} <path>       Config file path (default: export-config.json)");
            Console.WriteLine($"  {ARG_MODE} <mode>         Export mode: framework | full");
            Console.WriteLine($"  {ARG_DRY_RUN}             Preview only, no files copied");
            Console.WriteLine($"  {ARG_CHANGED_ONLY}        Only export git-changed files");
            Console.WriteLine($"  {ARG_SOURCE} <path>       Override source root directory (default: auto-detect)");
            Console.WriteLine($"  {ARG_OUTPUT} <path>       Override export output directory");
            Console.WriteLine($"  {ARG_ALLOW} <path>        Add extra allowed path (repeatable)");
            Console.WriteLine($"  {ARG_YES}                 Skip confirmation prompt");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Preview framework-only export");
            Console.WriteLine("  dotnet run -- --dry-run");
            Console.WriteLine();
            Console.WriteLine("  # Export framework + gameplay modules");
            Console.WriteLine("  dotnet run -- --mode full");
            Console.WriteLine();
            Console.WriteLine("  # Export only git-changed framework files");
            Console.WriteLine("  dotnet run -- --changed-only --dry-run");
            Console.WriteLine();
            Console.WriteLine("  # Add a custom path on the fly");
            Console.WriteLine("  dotnet run -- --allow Assets/Game/Modules/StatusEffect --dry-run");
            Console.WriteLine();
            Console.WriteLine("  # Skip confirmation and export immediately");
            Console.WriteLine("  dotnet run -- --yes");
            Console.WriteLine();
            Console.WriteLine("Workflow:");
            Console.WriteLine("  1. Run with --dry-run to preview");
            Console.WriteLine("  2. Review the console summary");
            Console.WriteLine("  3. Run without --dry-run to export");
            Console.WriteLine("  4. Review ExportPackage/export_report.md");
            Console.WriteLine("  5. Copy ExportPackage contents to original Git project");
            Console.WriteLine("  6. Run git diff in original project to verify");
            Console.WriteLine("  7. Commit if satisfied");
        }

        static bool HasArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static string GetArgValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }

        static string[] GetAllArgValues(string[] args, string name)
        {
            List<string> values = new();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    values.Add(args[i + 1]);
            }
            return values.ToArray();
        }

        static string FormatSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            if (bytes < KB) return $"{bytes} B";
            if (bytes < MB) return $"{bytes / KB:F1} KB";
            return $"{bytes / MB:F2} MB";
        }
    }
}
