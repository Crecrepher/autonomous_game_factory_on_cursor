using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class CodingStyleValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "CodingStyle";
        const string MODULES_RELATIVE = "Game/Modules";
        const string TESTS_FOLDER = "Tests";

        static readonly Regex RegexForeach = new Regex(@"\bforeach\s*\(");
        static readonly Regex RegexLinqUsing = new Regex(@"using\s+System\.Linq\b");
        static readonly Regex RegexLinqMethod = new Regex(@"\.(Select|Where|ToList|ToArray|OrderBy|OrderByDescending|GroupBy|First|FirstOrDefault|Last|LastOrDefault|Any|All|Count|Sum|Min|Max|Average|Distinct|Skip|Take|Aggregate|Zip|SelectMany)\s*\(");
        static readonly Regex RegexStartCoroutine = new Regex(@"\bStartCoroutine\s*\(");
        static readonly Regex RegexIEnumerator = new Regex(@"\bIEnumerator\s+\w+\s*\(");
        static readonly Regex RegexInvoke = new Regex(@"(?<!\.)(?<!\w)\b(Invoke|InvokeRepeating)\s*\(\s*""");
        static readonly Regex RegexLambdaBody = new Regex(@"=>\s*\{");
        static readonly Regex RegexLambdaCallback = new Regex(@"[\(,]\s*\w+\s*=>");
        static readonly Regex RegexPropertyArrow = new Regex(@"(public|private|protected|internal|static|readonly|override|virtual|abstract|sealed)\s+[\w<>\[\],\s]+\s+=>");
        static readonly Regex RegexNullableField = new Regex(@"(private|protected|internal|\[SerializeField\])\s+\w+\?\s+_\w+");

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
            if (RegexForeach.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "foreach is forbidden — use for loop instead: " + fileName, reportPath);

            if (RegexLinqUsing.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "LINQ (System.Linq) is forbidden — causes GC allocation: " + fileName, reportPath);

            if (RegexLinqMethod.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "LINQ method call detected — use manual loops instead: " + fileName, reportPath);

            if (RegexInvoke.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "Invoke/InvokeRepeating is forbidden — use timer or state machine: " + fileName, reportPath);

            if (RegexStartCoroutine.IsMatch(content))
                report.AddWarning(VALIDATOR_NAME, "StartCoroutine detected — prefer update loop or timer: " + fileName, reportPath);

            if (RegexIEnumerator.IsMatch(content))
                report.AddWarning(VALIDATOR_NAME, "IEnumerator method detected — prefer update loop or timer: " + fileName, reportPath);

            if (HasLambda(content))
                report.AddWarning(VALIDATOR_NAME, "Lambda expression detected — cache as method to avoid GC: " + fileName, reportPath);

            if (RegexNullableField.IsMatch(content))
                report.AddWarning(VALIDATOR_NAME, "Nullable '?' field detected — project convention forbids '?': " + fileName, reportPath);
        }

        bool HasLambda(string content)
        {
            if (!RegexLambdaBody.IsMatch(content) && !RegexLambdaCallback.IsMatch(content))
                return false;

            if (RegexPropertyArrow.IsMatch(content))
            {
                string stripped = RegexPropertyArrow.Replace(content, "");
                return RegexLambdaBody.IsMatch(stripped) || RegexLambdaCallback.IsMatch(stripped);
            }

            return true;
        }
    }
}
