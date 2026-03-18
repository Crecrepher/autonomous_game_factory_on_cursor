using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ArchitectureRuleValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ArchitectureRule";
        const string MODULES_RELATIVE = "Game/Modules";

        static readonly Regex RegexMonoBehaviour = new Regex(@"class\s+\w+\s*:\s*.*MonoBehaviour");
        static readonly Regex RegexScriptableObject = new Regex(@"class\s+\w+\s*:\s*.*ScriptableObject");

        int _scannedCount;

        public int Validate(ValidationReport report)
        {
            _scannedCount = 0;
            string modulesPath = Path.Combine(Application.dataPath, MODULES_RELATIVE);
            if (!Directory.Exists(modulesPath))
                return 0;

            ValidateRecursive(modulesPath, MODULES_RELATIVE, report);
            return _scannedCount;
        }

        void ValidateRecursive(string fullPath, string relativePath, ValidationReport report)
        {
            string[] files = Directory.GetFiles(fullPath, "*.cs", SearchOption.TopDirectoryOnly);
            _scannedCount += files.Length;

            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                string content = File.ReadAllText(files[i]);
                string reportPath = relativePath + "/" + fileName;

                if (fileName.EndsWith("Runtime.cs") && RegexMonoBehaviour.IsMatch(content))
                    report.AddError(VALIDATOR_NAME, "Runtime file must not inherit MonoBehaviour: " + fileName, reportPath);

                if (fileName.EndsWith("Config.cs") && !fileName.StartsWith("I") && !RegexScriptableObject.IsMatch(content))
                    report.AddError(VALIDATOR_NAME, "Config file must inherit ScriptableObject: " + fileName, reportPath);
            }

            string[] dirs = Directory.GetDirectories(fullPath);
            for (int i = 0; i < dirs.Length; i++)
            {
                string dirName = Path.GetFileName(dirs[i]);
                ValidateRecursive(dirs[i], relativePath + "/" + dirName, report);
            }
        }
    }
}
