using System.Text.Json;
using FrameworkExporter.Config;
using FrameworkExporter.Exporter;
using FrameworkExporter.Scanner;

namespace FrameworkExporter.Report
{
    public static class ManifestGenerator
    {
        const string MANIFEST_FILE_NAME = "manifest.json";

        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public static void Generate(string exportRoot, ScanResult scanResult, ExportResult exportResult, ExportConfig config)
        {
            ManifestData manifest = BuildManifest(scanResult, exportResult, config);
            string json = JsonSerializer.Serialize(manifest, _jsonOptions);

            if (!exportResult.IsDryRun)
            {
                string path = Path.Combine(exportRoot, MANIFEST_FILE_NAME);
                File.WriteAllText(path, json);
                Console.WriteLine($"  Manifest -> {path}");
            }
            else
            {
                string dryPath = Path.Combine(exportRoot, MANIFEST_FILE_NAME);
                string dryDir = Path.GetDirectoryName(dryPath);
                if (!string.IsNullOrEmpty(dryDir) && !Directory.Exists(dryDir))
                    Directory.CreateDirectory(dryDir);
                File.WriteAllText(dryPath, json);
                Console.WriteLine($"  Manifest (dry-run) -> {dryPath}");
            }
        }

        static ManifestData BuildManifest(ScanResult scanResult, ExportResult exportResult, ExportConfig config)
        {
            string[] exportedPaths = new string[exportResult.ExportedFiles.Count];
            string[] exportedReasons = new string[exportResult.ExportedFiles.Count];
            for (int i = 0; i < exportResult.ExportedFiles.Count; i++)
            {
                exportedPaths[i] = exportResult.ExportedFiles[i].RelativePath;

                ScannedFile match = FindScannedFile(scanResult, exportResult.ExportedFiles[i].RelativePath);
                exportedReasons[i] = match.Reason;
            }

            string[] skippedPaths = new string[scanResult.SkippedFiles.Count];
            for (int i = 0; i < scanResult.SkippedFiles.Count; i++)
                skippedPaths[i] = scanResult.SkippedFiles[i].RelativePath;

            string[] deniedPaths = new string[scanResult.DeniedFiles.Count];
            for (int i = 0; i < scanResult.DeniedFiles.Count; i++)
                deniedPaths[i] = scanResult.DeniedFiles[i].RelativePath;

            string[] riskyPaths = new string[scanResult.RiskyFiles.Count];
            for (int i = 0; i < scanResult.RiskyFiles.Count; i++)
                riskyPaths[i] = scanResult.RiskyFiles[i].RelativePath;

            string[] unknownPaths = new string[scanResult.UnknownFiles.Count];
            for (int i = 0; i < scanResult.UnknownFiles.Count; i++)
                unknownPaths[i] = scanResult.UnknownFiles[i].RelativePath;

            string[] gitFilteredPaths = new string[scanResult.GitFilteredFiles.Count];
            for (int i = 0; i < scanResult.GitFilteredFiles.Count; i++)
                gitFilteredPaths[i] = scanResult.GitFilteredFiles[i].RelativePath;

            return new ManifestData
            {
                Timestamp = exportResult.Timestamp.ToString("o"),
                ExportMode = config.ExportMode.ToString(),
                IsDryRun = exportResult.IsDryRun,
                IncludeOnlyChangedFiles = config.IncludeOnlyChangedFiles,
                TotalScanned = scanResult.TotalScanned,
                TotalExported = exportResult.ExportedFiles.Count,
                TotalSkipped = scanResult.SkippedFiles.Count,
                TotalDenied = scanResult.DeniedFiles.Count,
                TotalRisky = scanResult.RiskyFiles.Count,
                TotalUnknown = scanResult.UnknownFiles.Count,
                TotalGitFiltered = scanResult.GitFilteredFiles.Count,
                TotalSizeBytes = exportResult.TotalSizeBytes,
                ExportedFiles = exportedPaths,
                ExportedReasons = exportedReasons,
                SkippedFiles = skippedPaths,
                DeniedFiles = deniedPaths,
                RiskyFiles = riskyPaths,
                UnknownFiles = unknownPaths,
                GitFilteredFiles = gitFilteredPaths,
                Errors = exportResult.Errors.ToArray()
            };
        }

        static ScannedFile FindScannedFile(ScanResult scanResult, string relativePath)
        {
            string normalized = relativePath.Replace('\\', '/');
            for (int i = 0; i < scanResult.IncludedFiles.Count; i++)
            {
                if (string.Equals(
                    scanResult.IncludedFiles[i].RelativePath.Replace('\\', '/'),
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return scanResult.IncludedFiles[i];
                }
            }
            return new ScannedFile("", relativePath, EFileVerdict.Included, "unknown");
        }
    }

    internal sealed class ManifestData
    {
        public string Timestamp { get; set; } = string.Empty;
        public string ExportMode { get; set; } = string.Empty;
        public bool IsDryRun { get; set; }
        public bool IncludeOnlyChangedFiles { get; set; }
        public int TotalScanned { get; set; }
        public int TotalExported { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalDenied { get; set; }
        public int TotalRisky { get; set; }
        public int TotalUnknown { get; set; }
        public int TotalGitFiltered { get; set; }
        public long TotalSizeBytes { get; set; }
        public string[] ExportedFiles { get; set; } = Array.Empty<string>();
        public string[] ExportedReasons { get; set; } = Array.Empty<string>();
        public string[] SkippedFiles { get; set; } = Array.Empty<string>();
        public string[] DeniedFiles { get; set; } = Array.Empty<string>();
        public string[] RiskyFiles { get; set; } = Array.Empty<string>();
        public string[] UnknownFiles { get; set; } = Array.Empty<string>();
        public string[] GitFilteredFiles { get; set; } = Array.Empty<string>();
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}
