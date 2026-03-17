using System.Diagnostics;

namespace FrameworkExporter.Scanner
{
    public static class GitChangedFilesCollector
    {
        const int GIT_TIMEOUT_MS = 30000;

        public static HashSet<string> Collect(string sourceRoot)
        {
            HashSet<string> changedFiles = new(StringComparer.OrdinalIgnoreCase);

            string tracked = RunGit(sourceRoot, "diff --name-only HEAD");
            string untracked = RunGit(sourceRoot, "ls-files --others --exclude-standard");
            string staged = RunGit(sourceRoot, "diff --name-only --cached");

            ParseLines(tracked, changedFiles);
            ParseLines(untracked, changedFiles);
            ParseLines(staged, changedFiles);

            return changedFiles;
        }

        public static bool IsGitRepo(string sourceRoot)
        {
            string output = RunGit(sourceRoot, "rev-parse --is-inside-work-tree");
            return output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        static void ParseLines(string output, HashSet<string> set)
        {
            if (string.IsNullOrWhiteSpace(output))
                return;

            string[] lines = output.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim().Replace('\\', '/');
                if (line.Length > 0)
                    set.Add(line);
            }
        }

        static string RunGit(string workingDirectory, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(psi);
                if (process == null)
                    return string.Empty;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(GIT_TIMEOUT_MS);
                return output;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARN] Git command failed: git {arguments}");
                Console.WriteLine($"       {ex.Message}");
                Console.ResetColor();
                return string.Empty;
            }
        }
    }
}
