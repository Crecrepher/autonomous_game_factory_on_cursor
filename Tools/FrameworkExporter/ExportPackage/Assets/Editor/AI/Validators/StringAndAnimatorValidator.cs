using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class StringAndAnimatorValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "StringAndAnimator";
        const string MODULES_RELATIVE = "Game/Modules";
        const string TESTS_FOLDER = "Tests";

        static readonly Regex RegexAnimatorStringParam = new Regex(@"\b(SetFloat|SetBool|SetInteger|SetTrigger|ResetTrigger|GetFloat|GetBool|GetInteger)\s*\(\s*""");
        static readonly Regex RegexAnimatorPlay = new Regex(@"\.Play\s*\(\s*""");
        static readonly Regex RegexAnimatorCrossFade = new Regex(@"\.(CrossFade|CrossFadeInFixedTime)\s*\(\s*""");
        static readonly Regex RegexUpdateMethod = new Regex(@"void\s+(Update|LateUpdate|FixedUpdate|Tick)\s*\(", RegexOptions.Multiline);
        static readonly Regex RegexStringConcat = new Regex(@"""[^""]*""\s*\+|\+\s*""[^""]*""");

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
            CheckAnimatorStringParam(fileName, content, reportPath, report);
            CheckStringConcatInUpdate(fileName, content, reportPath, report);
        }

        void CheckAnimatorStringParam(string fileName, string content, string reportPath, ValidationReport report)
        {
            if (RegexAnimatorStringParam.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "Animator parameter used with raw string — cache hash via Animator.StringToHash: " + fileName, reportPath);

            if (RegexAnimatorPlay.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "Animator.Play with raw string — cache hash via Animator.StringToHash: " + fileName, reportPath);

            if (RegexAnimatorCrossFade.IsMatch(content))
                report.AddError(VALIDATOR_NAME, "Animator.CrossFade with raw string — cache hash via Animator.StringToHash: " + fileName, reportPath);
        }

        void CheckStringConcatInUpdate(string fileName, string content, string reportPath, ValidationReport report)
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

                if (RegexStringConcat.IsMatch(methodBody))
                {
                    report.AddWarning(VALIDATOR_NAME, "String concatenation in Update/Tick — use StringBuilder or cached strings: " + fileName, reportPath);
                    return;
                }
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
