using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ArchitecturePatternValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ArchitecturePattern";
        const string MODULES_RELATIVE = "Game/Modules";
        const string TESTS_FOLDER = "Tests";

        static readonly Regex RegexMonoBehaviour = new Regex(@"class\s+\w+\s*:\s*.*MonoBehaviour");
        static readonly Regex RegexStaticInstance = new Regex(@"static\s+.*\bInstance\b");
        static readonly Regex RegexSingletonPattern = new Regex(@"Singleton");
        static readonly Regex RegexNamespace = new Regex(@"^\s*namespace\s+\w+", RegexOptions.Multiline);
        static readonly Regex RegexPublicFieldCandidate = new Regex(@"^\s*public\s+(?!event\b)(?!static\b)(?!delegate\b)(?!override\b)(?!abstract\b)(?!virtual\b)(?!const\b)(?!readonly\s+static\b)(?!class\b)(?!interface\b)(?!enum\b)(?!struct\b)[^;\n=>{]+\s+[A-Z]\w*\s*;", RegexOptions.Multiline);
        static readonly Regex RegexAwake = new Regex(@"void\s+Awake\s*\(\s*\)");
        static readonly Regex RegexReflectionUsing = new Regex(@"using\s+System\.Reflection\b");
        static readonly Regex RegexReflectionCall = new Regex(@"GetType\s*\(\s*\)\s*\.\s*(GetMethod|GetField|GetProperty|GetMembers|GetMember|InvokeMember)\s*\(");

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
            string dirName = Path.GetFileName(fullPath);
            if (dirName == TESTS_FOLDER)
                return;

            string[] files = Directory.GetFiles(fullPath, "*.cs", SearchOption.TopDirectoryOnly);
            _scannedCount += files.Length;

            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                string content = File.ReadAllText(files[i]);
                string reportPath = relativePath + "/" + fileName;

                ValidateFile(fileName, content, reportPath, report);
            }

            string[] dirs = Directory.GetDirectories(fullPath);
            for (int i = 0; i < dirs.Length; i++)
            {
                string childDirName = Path.GetFileName(dirs[i]);
                ValidateRecursive(dirs[i], relativePath + "/" + childDirName, report);
            }
        }

        void ValidateFile(string fileName, string content, string reportPath, ValidationReport report)
        {
            CheckMonoBehaviourSingleton(fileName, content, reportPath, report);
            CheckNamespace(fileName, content, reportPath, report);
            CheckPublicField(fileName, content, reportPath, report);
            CheckAwakeUsage(fileName, content, reportPath, report);
            CheckReflection(fileName, content, reportPath, report);
        }

        void CheckMonoBehaviourSingleton(string fileName, string content, string reportPath, ValidationReport report)
        {
            if (!RegexMonoBehaviour.IsMatch(content))
                return;

            if (RegexStaticInstance.IsMatch(content) || RegexSingletonPattern.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "MonoBehaviour singleton pattern is forbidden: " + fileName, reportPath);
        }

        void CheckNamespace(string fileName, string content, string reportPath, ValidationReport report)
        {
            if (!RegexNamespace.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "Missing namespace declaration: " + fileName, reportPath);
        }

        void CheckPublicField(string fileName, string content, string reportPath, ValidationReport report)
        {
            MatchCollection publicFields = RegexPublicFieldCandidate.Matches(content);
            if (publicFields.Count == 0)
                return;

            for (int i = 0; i < publicFields.Count; i++)
            {
                string line = publicFields[i].Value.Trim();

                if (line.Contains("event ") || line.Contains("static ") || line.Contains("const ") || line.Contains("delegate "))
                    continue;

                report.AddWarning(VALIDATOR_NAME, "Public field exposed directly — use property or '=>' instead: " + fileName, reportPath);
                return;
            }
        }

        void CheckAwakeUsage(string fileName, string content, string reportPath, ValidationReport report)
        {
            if (!RegexAwake.IsMatch(content))
                return;

            if (fileName.EndsWith("Bootstrap.cs"))
                return;

            report.AddWarning(VALIDATOR_NAME, "Awake() used in non-Bootstrap class — prefer Init() pattern: " + fileName, reportPath);
        }

        void CheckReflection(string fileName, string content, string reportPath, ValidationReport report)
        {
            if (RegexReflectionUsing.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "System.Reflection is forbidden: " + fileName, reportPath);

            if (RegexReflectionCall.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "Reflection method call (GetType().GetMethod/GetField) is forbidden: " + fileName, reportPath);
        }
    }
}
