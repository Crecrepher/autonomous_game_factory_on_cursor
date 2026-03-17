using System.Text;
using FrameworkExporter.Config;
using FrameworkExporter.Exporter;
using FrameworkExporter.Scanner;

namespace FrameworkExporter.Report
{
    public static class ReportGenerator
    {
        const string REPORT_FILE_NAME = "export_report.md";
        const int MAX_LIST_ITEMS = 200;

        public static void Generate(string exportRoot, ScanResult scanResult, ExportResult exportResult, ExportConfig config)
        {
            StringBuilder sb = new();

            WriteHeader(sb, exportResult, config);
            WriteSummaryTable(sb, scanResult, exportResult);
            WriteExportedSection(sb, exportResult);
            WriteRiskySection(sb, scanResult);
            WriteUnknownSection(sb, scanResult);
            WriteGitFilteredSection(sb, scanResult);
            WriteSkippedSection(sb, scanResult);
            WriteDeniedSection(sb, scanResult);
            WriteErrorsSection(sb, exportResult);
            WriteApplyGuide(sb, exportResult);
            WriteFooter(sb);

            string report = sb.ToString();

            if (!exportResult.IsDryRun)
            {
                string path = Path.Combine(exportRoot, REPORT_FILE_NAME);
                File.WriteAllText(path, report);
                Console.WriteLine($"  Report -> {path}");
            }
            else
            {
                string dryPath = Path.Combine(exportRoot, REPORT_FILE_NAME);
                string dryDir = Path.GetDirectoryName(dryPath);
                if (!string.IsNullOrEmpty(dryDir) && !Directory.Exists(dryDir))
                    Directory.CreateDirectory(dryDir);
                File.WriteAllText(dryPath, report);
                Console.WriteLine($"  Report (dry-run) -> {dryPath}");
            }
        }

        static void WriteHeader(StringBuilder sb, ExportResult result, ExportConfig config)
        {
            sb.AppendLine("# Framework Export Report");
            sb.AppendLine();
            sb.AppendLine($"- **Timestamp**: {result.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"- **Mode**: {config.ExportMode}");
            sb.AppendLine($"- **Dry Run**: {(result.IsDryRun ? "Yes" : "No")}");
            sb.AppendLine($"- **Changed-only filter**: {config.IncludeOnlyChangedFiles}");
            sb.AppendLine($"- **Export Path**: `{result.ExportRootPath}`");
            sb.AppendLine();
        }

        static void WriteSummaryTable(StringBuilder sb, ScanResult scan, ExportResult export)
        {
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine("| Category | Count |");
            sb.AppendLine("|----------|------:|");
            sb.AppendLine($"| Total files scanned | {scan.TotalScanned} |");
            sb.AppendLine($"| **Exported** | **{export.ExportedFiles.Count}** |");
            sb.AppendLine($"| Risky (included, flagged) | {scan.RiskyFiles.Count} |");
            sb.AppendLine($"| Unknown (NOT exported) | {scan.UnknownFiles.Count} |");
            sb.AppendLine($"| Skipped (outside paths) | {scan.SkippedFiles.Count} |");
            sb.AppendLine($"| Denied (blocked) | {scan.DeniedFiles.Count} |");
            sb.AppendLine($"| Git-filtered (unchanged) | {scan.GitFilteredFiles.Count} |");
            sb.AppendLine($"| Errors | {export.Errors.Count} |");
            sb.AppendLine($"| Total export size | {FormatSize(export.TotalSizeBytes)} |");
            sb.AppendLine();
        }

        static void WriteExportedSection(StringBuilder sb, ExportResult export)
        {
            sb.AppendLine("## Exported Files");
            sb.AppendLine();
            if (export.ExportedFiles.Count == 0)
            {
                sb.AppendLine("_No files exported._");
                sb.AppendLine();
                return;
            }

            int count = Math.Min(export.ExportedFiles.Count, MAX_LIST_ITEMS);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"- `{export.ExportedFiles[i].RelativePath}`");
            }
            if (export.ExportedFiles.Count > MAX_LIST_ITEMS)
                sb.AppendLine($"- ... and {export.ExportedFiles.Count - MAX_LIST_ITEMS} more");
            sb.AppendLine();
        }

        static void WriteRiskySection(StringBuilder sb, ScanResult scan)
        {
            sb.AppendLine("## Risky Files (Review Before Applying!)");
            sb.AppendLine();
            if (scan.RiskyFiles.Count == 0)
            {
                sb.AppendLine("_No risky files detected._");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("> **WARNING**: These files have risky extensions (.unity, .prefab, .asmdef, .asset, .meta, etc.).");
            sb.AppendLine("> They can cause merge conflicts, break scene references, or alter project structure.");
            sb.AppendLine("> Review each one carefully before applying to the original project.");
            sb.AppendLine();

            int count = Math.Min(scan.RiskyFiles.Count, MAX_LIST_ITEMS);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"- **{scan.RiskyFiles[i].RelativePath}** — {scan.RiskyFiles[i].Reason}");
            }
            if (scan.RiskyFiles.Count > MAX_LIST_ITEMS)
                sb.AppendLine($"- ... and {scan.RiskyFiles.Count - MAX_LIST_ITEMS} more");
            sb.AppendLine();
        }

        static void WriteUnknownSection(StringBuilder sb, ScanResult scan)
        {
            sb.AppendLine("## Unknown Files (NOT Exported)");
            sb.AppendLine();
            if (scan.UnknownFiles.Count == 0)
            {
                sb.AppendLine("_No unknown file types detected._");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("> These files have unrecognized extensions and were NOT included in the export.");
            sb.AppendLine("> If any should be included, add their extension to a known list or use `--allow`.");
            sb.AppendLine();

            int count = Math.Min(scan.UnknownFiles.Count, MAX_LIST_ITEMS);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"- `{scan.UnknownFiles[i].RelativePath}` — {scan.UnknownFiles[i].Reason}");
            }
            if (scan.UnknownFiles.Count > MAX_LIST_ITEMS)
                sb.AppendLine($"- ... and {scan.UnknownFiles.Count - MAX_LIST_ITEMS} more");
            sb.AppendLine();
        }

        static void WriteGitFilteredSection(StringBuilder sb, ScanResult scan)
        {
            if (scan.GitFilteredFiles.Count == 0)
                return;

            sb.AppendLine("## Git-Filtered Files (Unchanged, NOT Exported)");
            sb.AppendLine();
            sb.AppendLine($"_{scan.GitFilteredFiles.Count} files were in allowed paths but had no git changes._");
            sb.AppendLine();

            int count = Math.Min(scan.GitFilteredFiles.Count, 50);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"- `{scan.GitFilteredFiles[i].RelativePath}`");
            }
            if (scan.GitFilteredFiles.Count > 50)
                sb.AppendLine($"- ... and {scan.GitFilteredFiles.Count - 50} more");
            sb.AppendLine();
        }

        static void WriteSkippedSection(StringBuilder sb, ScanResult scan)
        {
            sb.AppendLine("## Skipped Files (Outside Allowed Paths)");
            sb.AppendLine();
            if (scan.SkippedFiles.Count == 0)
            {
                sb.AppendLine("_No files skipped._");
                sb.AppendLine();
                return;
            }

            sb.AppendLine($"_{scan.SkippedFiles.Count} files were outside the allowed paths._");
            sb.AppendLine();

            int count = Math.Min(scan.SkippedFiles.Count, MAX_LIST_ITEMS);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"- `{scan.SkippedFiles[i].RelativePath}`");
            }
            if (scan.SkippedFiles.Count > MAX_LIST_ITEMS)
                sb.AppendLine($"- ... and {scan.SkippedFiles.Count - MAX_LIST_ITEMS} more");
            sb.AppendLine();
        }

        static void WriteDeniedSection(StringBuilder sb, ScanResult scan)
        {
            sb.AppendLine("## Denied Files (Blocked by Rules)");
            sb.AppendLine();
            if (scan.DeniedFiles.Count == 0)
            {
                sb.AppendLine("_No files denied._");
                sb.AppendLine();
                return;
            }

            sb.AppendLine($"_{scan.DeniedFiles.Count} files were blocked by deny rules._");
            sb.AppendLine();

            int count = Math.Min(scan.DeniedFiles.Count, MAX_LIST_ITEMS);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"- `{scan.DeniedFiles[i].RelativePath}` — {scan.DeniedFiles[i].Reason}");
            }
            if (scan.DeniedFiles.Count > MAX_LIST_ITEMS)
                sb.AppendLine($"- ... and {scan.DeniedFiles.Count - MAX_LIST_ITEMS} more");
            sb.AppendLine();
        }

        static void WriteErrorsSection(StringBuilder sb, ExportResult export)
        {
            sb.AppendLine("## Errors");
            sb.AppendLine();
            if (export.Errors.Count == 0)
            {
                sb.AppendLine("_No errors._");
                sb.AppendLine();
                return;
            }

            for (int i = 0; i < export.Errors.Count; i++)
            {
                sb.AppendLine($"- {export.Errors[i]}");
            }
            sb.AppendLine();
        }

        static void WriteApplyGuide(StringBuilder sb, ExportResult export)
        {
            sb.AppendLine("## How to Apply This Export");
            sb.AppendLine();
            sb.AppendLine("### Safe Apply Workflow");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# 1. Make sure original project is on a clean branch");
            sb.AppendLine("cd <your-original-project>");
            sb.AppendLine("git status          # should be clean");
            sb.AppendLine("git checkout -b backport/framework-update");
            sb.AppendLine();
            sb.AppendLine("# 2. Copy exported files (preserves relative paths)");
            sb.AppendLine("# Windows:");
            sb.AppendLine($"xcopy /E /Y \"{export.ExportRootPath}\\*\" \".\\\"");
            sb.AppendLine("# macOS/Linux:");
            sb.AppendLine($"cp -R {export.ExportRootPath}/* .");
            sb.AppendLine();
            sb.AppendLine("# 3. Remove the manifest/report (they're for review only)");
            sb.AppendLine("del manifest.json export_report.md");
            sb.AppendLine("# or: rm manifest.json export_report.md");
            sb.AppendLine();
            sb.AppendLine("# 4. Review what changed");
            sb.AppendLine("git diff");
            sb.AppendLine("git diff --stat");
            sb.AppendLine();
            sb.AppendLine("# 5. Stage and commit if satisfied");
            sb.AppendLine("git add -A");
            sb.AppendLine("git commit -m \"backport: framework update from working project\"");
            sb.AppendLine();
            sb.AppendLine("# 6. If something went wrong, reset");
            sb.AppendLine("git checkout main");
            sb.AppendLine("git branch -D backport/framework-update");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("### Key Safety Tips");
            sb.AppendLine();
            sb.AppendLine("- Always create a new branch before applying");
            sb.AppendLine("- Always review `git diff` before committing");
            sb.AppendLine("- Check risky files (.unity, .prefab, .asset) individually");
            sb.AppendLine("- Run Unity and check for compile errors after applying");
            sb.AppendLine("- If the original project has diverged, use `git diff --stat` to compare scope");
            sb.AppendLine();
        }

        static void WriteFooter(StringBuilder sb)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("Generated by **FrameworkExporter v2.0.0**.");
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
