using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class PerformanceValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "Performance";
        const string MODULES_RELATIVE = "Game/Modules";
        const string TESTS_FOLDER = "Tests";

        static readonly Regex RegexGetComponent = new Regex(@"\bGetComponent(InChildren|InParent|s|sInChildren|sInParent)?\s*[<(]");
        static readonly Regex RegexGameObjectFind = new Regex(@"\bGameObject\.Find\s*\(");
        static readonly Regex RegexFindObjectOfType = new Regex(@"\bFindObject(OfType|sOfType)\s*[<(]");
        static readonly Regex RegexUpdateMethod = new Regex(@"void\s+(Update|LateUpdate|FixedUpdate|Tick)\s*\(", RegexOptions.Multiline);
        static readonly Regex RegexMagnitude = new Regex(@"\.(magnitude|distance)\b");
        static readonly Regex RegexInstantiate = new Regex(@"\bInstantiate\s*[<(]");
        static readonly Regex RegexDestroy = new Regex(@"\b(Destroy|DestroyImmediate)\s*\(");

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
            if (RegexGetComponent.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "GetComponent at runtime is forbidden — cache via SerializeField or editor-time: " + fileName, reportPath);

            if (RegexGameObjectFind.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "GameObject.Find is forbidden at runtime — cache reference: " + fileName, reportPath);

            if (RegexFindObjectOfType.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "FindObjectOfType is forbidden at runtime — cache reference: " + fileName, reportPath);

            if (RegexInstantiate.IsMatch(content))
                report.AddWarning(VALIDATOR_NAME, "Instantiate detected — consider object pooling: " + fileName, reportPath);

            if (RegexDestroy.IsMatch(content))
                report.AddWarning(VALIDATOR_NAME, "Destroy detected — consider object pooling: " + fileName, reportPath);

            CheckMagnitudeInUpdateMethods(fileName, content, reportPath, report);
        }

        void CheckMagnitudeInUpdateMethods(string fileName, string content, string reportPath, ValidationReport report)
        {
            MatchCollection updateMatches = RegexUpdateMethod.Matches(content);
            if (updateMatches.Count == 0)
                return;

            for (int i = 0; i < updateMatches.Count; i++)
            {
                int methodStart = updateMatches[i].Index;
                string methodBody = ExtractMethodBody(content, methodStart);
                if (methodBody == null)
                    continue;

                if (RegexMagnitude.IsMatch(methodBody))
                    report.AddWarning(VALIDATOR_NAME, ".magnitude/.distance in Update/Tick — use sqrMagnitude for performance: " + fileName, reportPath);
            }
        }

        static string ExtractMethodBody(string content, int methodSignatureStart)
        {
            int braceStart = content.IndexOf('{', methodSignatureStart);
            if (braceStart < 0)
                return null;

            int depth = 0;
            int bodyEnd = -1;
            for (int i = braceStart; i < content.Length; i++)
            {
                char c = content[i];
                if (c == '{')
                    depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        bodyEnd = i;
                        break;
                    }
                }
            }

            if (bodyEnd < 0)
                return null;

            return content.Substring(braceStart, bodyEnd - braceStart + 1);
        }
    }
}
