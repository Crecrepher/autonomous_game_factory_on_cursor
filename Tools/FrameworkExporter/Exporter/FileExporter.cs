using FrameworkExporter.Config;
using FrameworkExporter.Scanner;

namespace FrameworkExporter.Exporter
{
    public sealed class FileExporter
    {
        readonly ExportConfig _config;

        public FileExporter(ExportConfig config)
        {
            _config = config;
        }

        public ExportResult Export(ScanResult scanResult)
        {
            string exportRoot = Path.GetFullPath(_config.ExportRoot);
            bool isDryRun = _config.DryRun;

            ExportResult result = new()
            {
                IsDryRun = isDryRun,
                ExportRootPath = exportRoot,
                Timestamp = DateTime.UtcNow
            };

            if (!isDryRun)
            {
                if (Directory.Exists(exportRoot))
                {
                    Directory.Delete(exportRoot, true);
                    Console.WriteLine($"[INFO] Cleaned previous export at '{exportRoot}'");
                }
                Directory.CreateDirectory(exportRoot);
            }

            List<ScannedFile> filesToExport = scanResult.IncludedFiles;
            long totalSize = 0;

            for (int i = 0; i < filesToExport.Count; i++)
            {
                ScannedFile scanned = filesToExport[i];
                string relativePath = scanned.RelativePath.Replace('\\', '/');
                string destPath = Path.Combine(exportRoot, relativePath);

                try
                {
                    FileInfo sourceInfo = new(scanned.AbsolutePath);
                    long fileSize = sourceInfo.Exists ? sourceInfo.Length : 0;
                    totalSize += fileSize;

                    if (!isDryRun)
                    {
                        string destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        File.Copy(scanned.AbsolutePath, destPath, true);
                    }

                    result.ExportedFiles.Add(new ExportedFile(
                        scanned.AbsolutePath, destPath, relativePath, fileSize));
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to copy '{relativePath}': {ex.Message}");
                }
            }

            result.TotalSizeBytes = totalSize;
            return result;
        }
    }
}
