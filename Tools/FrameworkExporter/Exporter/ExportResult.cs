namespace FrameworkExporter.Exporter
{
    public readonly struct ExportedFile
    {
        public readonly string SourcePath;
        public readonly string DestinationPath;
        public readonly string RelativePath;
        public readonly long FileSizeBytes;

        public ExportedFile(string sourcePath, string destinationPath, string relativePath, long fileSizeBytes)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            RelativePath = relativePath;
            FileSizeBytes = fileSizeBytes;
        }
    }

    public sealed class ExportResult
    {
        public List<ExportedFile> ExportedFiles { get; } = new();
        public List<string> Errors { get; } = new();
        public bool IsDryRun { get; set; }
        public string ExportRootPath { get; set; } = string.Empty;
        public long TotalSizeBytes { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
