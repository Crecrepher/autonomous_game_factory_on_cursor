namespace FrameworkExporter.Scanner
{
    public enum EFileVerdict
    {
        Included,
        IncludedRisky,
        Denied,
        OutsideAllowedPaths,
        Unknown,
        FilteredByGit
    }

    public readonly struct ScannedFile
    {
        public readonly string AbsolutePath;
        public readonly string RelativePath;
        public readonly EFileVerdict Verdict;
        public readonly string Reason;

        public ScannedFile(string absolutePath, string relativePath, EFileVerdict verdict, string reason)
        {
            AbsolutePath = absolutePath;
            RelativePath = relativePath;
            Verdict = verdict;
            Reason = reason;
        }

        public bool IsIncluded => Verdict == EFileVerdict.Included || Verdict == EFileVerdict.IncludedRisky;
    }

    public sealed class ScanResult
    {
        public List<ScannedFile> IncludedFiles { get; } = new();
        public List<ScannedFile> RiskyFiles { get; } = new();
        public List<ScannedFile> DeniedFiles { get; } = new();
        public List<ScannedFile> SkippedFiles { get; } = new();
        public List<ScannedFile> UnknownFiles { get; } = new();
        public List<ScannedFile> GitFilteredFiles { get; } = new();

        public int TotalScanned =>
            IncludedFiles.Count + DeniedFiles.Count +
            SkippedFiles.Count + UnknownFiles.Count + GitFilteredFiles.Count;

        public void Add(ScannedFile file)
        {
            switch (file.Verdict)
            {
                case EFileVerdict.Included:
                    IncludedFiles.Add(file);
                    break;
                case EFileVerdict.IncludedRisky:
                    RiskyFiles.Add(file);
                    IncludedFiles.Add(file);
                    break;
                case EFileVerdict.Denied:
                    DeniedFiles.Add(file);
                    break;
                case EFileVerdict.OutsideAllowedPaths:
                    SkippedFiles.Add(file);
                    break;
                case EFileVerdict.Unknown:
                    UnknownFiles.Add(file);
                    break;
                case EFileVerdict.FilteredByGit:
                    GitFilteredFiles.Add(file);
                    break;
            }
        }
    }
}
