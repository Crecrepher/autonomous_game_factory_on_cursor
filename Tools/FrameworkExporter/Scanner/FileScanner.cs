using FrameworkExporter.Config;

namespace FrameworkExporter.Scanner
{
    public sealed class FileScanner
    {
        readonly ExportConfig _config;

        public FileScanner(ExportConfig config)
        {
            _config = config;
        }

        public ScanResult Scan()
        {
            string sourceRoot = Path.GetFullPath(_config.SourceRoot);
            if (!Directory.Exists(sourceRoot))
                throw new DirectoryNotFoundException($"Source root not found: '{sourceRoot}'");

            HashSet<string> changedFiles = new(StringComparer.OrdinalIgnoreCase);

            if (_config.IncludeOnlyChangedFiles)
            {
                if (!GitChangedFilesCollector.IsGitRepo(sourceRoot))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[WARN] includeOnlyChangedFiles is enabled but source is not a git repo.");
                    Console.WriteLine("       All allowed files will be included instead.");
                    Console.ResetColor();
                }
                else
                {
                    changedFiles = GitChangedFilesCollector.Collect(sourceRoot);
                    Console.WriteLine($"[INFO] Git reports {changedFiles.Count} changed/untracked files");
                }
            }

            FileClassifier classifier = new(_config, changedFiles);
            ScanResult result = new();

            string[] allFiles = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);

            int sourceRootLength = sourceRoot.Length;
            if (!sourceRoot.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                !sourceRoot.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                sourceRootLength++;
            }

            for (int i = 0; i < allFiles.Length; i++)
            {
                string absolutePath = allFiles[i];
                string relativePath = absolutePath.Substring(sourceRootLength);
                ScannedFile classified = classifier.Classify(absolutePath, relativePath);
                result.Add(classified);
            }

            return result;
        }
    }
}
