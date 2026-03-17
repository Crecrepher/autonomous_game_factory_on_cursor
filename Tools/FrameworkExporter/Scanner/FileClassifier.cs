using FrameworkExporter.Config;

namespace FrameworkExporter.Scanner
{
    public sealed class FileClassifier
    {
        readonly string[] _allowedPaths;
        readonly string[] _deniedPaths;
        readonly HashSet<string> _deniedExtensions;
        readonly HashSet<string> _riskyExtensions;
        readonly bool _includeMetaFiles;
        readonly bool _onlyChanged;
        readonly HashSet<string> _changedFilesSet;

        static readonly HashSet<string> _knownCodeExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".md", ".yaml", ".yml", ".json", ".txt",
            ".xml", ".mdc", ".cfg", ".csv", ".tsv", ".ini"
        };

        public FileClassifier(ExportConfig config, HashSet<string> changedFiles)
        {
            _allowedPaths = NormalizePaths(config.GetEffectiveAllowedPaths());
            _deniedPaths = NormalizePaths(config.DeniedPaths);
            _deniedExtensions = new HashSet<string>(config.DeniedExtensions, StringComparer.OrdinalIgnoreCase);
            _riskyExtensions = new HashSet<string>(config.RiskyExtensions, StringComparer.OrdinalIgnoreCase);
            _includeMetaFiles = config.IncludeMetaFiles;
            _onlyChanged = config.IncludeOnlyChangedFiles;
            _changedFilesSet = changedFiles;
        }

        public ScannedFile Classify(string absolutePath, string relativePath)
        {
            string normalized = NormalizePath(relativePath);
            string extension = Path.GetExtension(normalized);

            if (IsDeniedPath(normalized))
                return new ScannedFile(absolutePath, relativePath, EFileVerdict.Denied, "Path in deny list");

            if (IsMetaFile(normalized))
            {
                if (!_includeMetaFiles)
                    return new ScannedFile(absolutePath, relativePath, EFileVerdict.Denied, "Meta files excluded by config");

                string originalPath = normalized.Substring(0, normalized.Length - 5);
                if (IsDeniedPath(originalPath))
                    return new ScannedFile(absolutePath, relativePath, EFileVerdict.Denied, "Meta for denied path");
            }

            if (_deniedExtensions.Contains(extension))
                return new ScannedFile(absolutePath, relativePath, EFileVerdict.Denied, $"Extension '{extension}' denied");

            string matchedAllowedPath = FindMatchingAllowedPath(normalized);
            if (matchedAllowedPath == null)
                return new ScannedFile(absolutePath, relativePath, EFileVerdict.OutsideAllowedPaths, "Not under any allowed path");

            if (_onlyChanged && _changedFilesSet.Count > 0)
            {
                if (!_changedFilesSet.Contains(normalized) && !IsMetaOfChanged(normalized))
                    return new ScannedFile(absolutePath, relativePath, EFileVerdict.FilteredByGit, "File not changed (git filter)");
            }

            bool isRisky = _riskyExtensions.Contains(extension);
            bool isKnown = isRisky || _knownCodeExtensions.Contains(extension);

            if (!isKnown && !IsMetaFile(normalized))
                return new ScannedFile(absolutePath, relativePath, EFileVerdict.Unknown, $"Unknown extension '{extension}' under '{matchedAllowedPath}'");

            if (isRisky)
                return new ScannedFile(absolutePath, relativePath, EFileVerdict.IncludedRisky, $"Risky extension '{extension}' under '{matchedAllowedPath}'");

            return new ScannedFile(absolutePath, relativePath, EFileVerdict.Included, $"Allowed under '{matchedAllowedPath}'");
        }

        string FindMatchingAllowedPath(string normalized)
        {
            for (int i = 0; i < _allowedPaths.Length; i++)
            {
                if (normalized.StartsWith(_allowedPaths[i], StringComparison.OrdinalIgnoreCase))
                    return _allowedPaths[i];
            }
            return null;
        }

        bool IsDeniedPath(string normalized)
        {
            for (int i = 0; i < _deniedPaths.Length; i++)
            {
                if (normalized.StartsWith(_deniedPaths[i] + "/", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (normalized.Equals(_deniedPaths[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        bool IsMetaOfChanged(string normalized)
        {
            if (!IsMetaFile(normalized))
                return false;

            string originalPath = normalized.Substring(0, normalized.Length - 5);
            return _changedFilesSet.Contains(originalPath);
        }

        static bool IsMetaFile(string path) => path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);

        static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

        static string[] NormalizePaths(string[] paths)
        {
            string[] result = new string[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                result[i] = NormalizePath(paths[i]).TrimEnd('/');
            }
            return result;
        }
    }
}
