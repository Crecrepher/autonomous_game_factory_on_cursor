using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ForbiddenFolderValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ForbiddenFolder";
        const string CORE_RELATIVE_PATH = "Game/Core";
        const string BASELINE_FOLDER = "Editor/AI/Baseline";
        const string CORE_BASELINE_FILENAME = "CoreFiles.txt";

        public void Validate(ValidationReport report)
        {
            string coreFullPath = Path.Combine(Application.dataPath, CORE_RELATIVE_PATH);
            if (!Directory.Exists(coreFullPath))
                return;

            string[] currentFiles = Directory.GetFiles(coreFullPath, "*.cs", SearchOption.AllDirectories);
            HashSet<string> currentSet = new HashSet<string>();
            for (int i = 0; i < currentFiles.Length; i++)
            {
                string relative = GetRelativePathFromDataPath(currentFiles[i]);
                if (!string.IsNullOrEmpty(relative))
                    currentSet.Add(relative.Replace("\\", "/"));
            }

            string baselinePath = Path.Combine(Application.dataPath, BASELINE_FOLDER, CORE_BASELINE_FILENAME);
            if (!File.Exists(baselinePath))
            {
                report.AddWarning(VALIDATOR_NAME, "Core baseline not found. Run Tools/AI/Update Core Baseline after any intentional Core change.", baselinePath);
                return;
            }

            string[] baselineLines = File.ReadAllLines(baselinePath);
            HashSet<string> baselineSet = new HashSet<string>();
            for (int i = 0; i < baselineLines.Length; i++)
            {
                string line = baselineLines[i].Trim();
                if (line.Length > 0 && !line.StartsWith("#"))
                    baselineSet.Add(line.Replace("\\", "/"));
            }

            foreach (string cur in currentSet)
            {
                if (!baselineSet.Contains(cur))
                    report.AddError(VALIDATOR_NAME, "File in forbidden folder (Core): not in baseline. Remove or run Update Core Baseline if intentional.", cur);
            }

            foreach (string baseLine in baselineSet)
            {
                string fullPath = Path.Combine(Application.dataPath, "..", baseLine);
                fullPath = Path.GetFullPath(fullPath);
                if (!File.Exists(fullPath))
                    report.AddWarning(VALIDATOR_NAME, "Baseline lists file that no longer exists: " + baseLine, baseLine);
            }
        }

        static string GetRelativePathFromDataPath(string fullPath)
        {
            string dataPath = Application.dataPath;
            if (!fullPath.StartsWith(dataPath))
                return null;
            string sub = fullPath.Substring(dataPath.Length);
            if (sub.StartsWith("/") || sub.StartsWith("\\"))
                sub = sub.Substring(1);
            return "Assets/" + sub;
        }
    }
}
