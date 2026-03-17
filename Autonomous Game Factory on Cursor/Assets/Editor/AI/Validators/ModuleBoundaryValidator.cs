using System.IO;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ModuleBoundaryValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ModuleBoundary";
        const string MODULES_RELATIVE = "Game/Modules";
        const string TESTS_FOLDER_NAME = "Tests";
        const string EDITOR_FOLDER_NAME = "Editor";
        const string CS_EXTENSION = "*.cs";

        static readonly string[] SUSPICIOUS_GAMEPLAY_SUFFIXES = new string[]
        {
            "Manager", "Controller", "System", "Handler", "Service"
        };

        static readonly string[] UI_KEYWORDS = new string[]
        {
            "UI", "Panel", "Popup", "Screen", "Canvas", "Dialog", "Window", "HUD"
        };

        int _scannedCount;

        public int Validate(ValidationReport report)
        {
            _scannedCount = 0;
            string modulesPath = Path.Combine(Application.dataPath, MODULES_RELATIVE);
            if (!Directory.Exists(modulesPath))
                return 0;

            string[] moduleDirs = Directory.GetDirectories(modulesPath);
            for (int i = 0; i < moduleDirs.Length; i++)
            {
                string moduleName = Path.GetFileName(moduleDirs[i]);
                ValidateModule(moduleDirs[i], moduleName, report);
            }

            return _scannedCount;
        }

        void ValidateModule(string moduleFullPath, string moduleName, ValidationReport report)
        {
            string moduleRelative = "Assets/" + MODULES_RELATIVE + "/" + moduleName;

            string[] rootFiles = Directory.GetFiles(moduleFullPath, CS_EXTENSION, SearchOption.TopDirectoryOnly);
            _scannedCount += rootFiles.Length;

            for (int i = 0; i < rootFiles.Length; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(rootFiles[i]);
                string reportPath = moduleRelative + "/" + Path.GetFileName(rootFiles[i]);

                if (!IsAllowedRootFile(fileName, moduleName))
                {
                    report.AddError(VALIDATOR_NAME,
                        "Unrecognized file in module root: " + fileName + ".cs (expected pattern: I" + moduleName + " / " + moduleName + "Config / " + moduleName + "Runtime / " + moduleName + "Factory / " + moduleName + "Instance / E" + moduleName + "*)",
                        reportPath);
                }

                CheckEditorScriptInRuntime(fileName, reportPath, report);
                CheckUIFileInNonUIModule(fileName, moduleName, reportPath, report);
                CheckSuspiciousGameplayFile(fileName, moduleName, reportPath, report);
            }

            ValidateTestsFolder(moduleFullPath, moduleName, moduleRelative, report);
            ValidateSubfolders(moduleFullPath, moduleName, moduleRelative, report);
        }

        static bool IsAllowedRootFile(string fileNameWithoutExt, string moduleName)
        {
            if (fileNameWithoutExt == "I" + moduleName)
                return true;
            if (fileNameWithoutExt == moduleName + "Config")
                return true;
            if (fileNameWithoutExt == moduleName + "Runtime")
                return true;
            if (fileNameWithoutExt == moduleName + "Factory")
                return true;
            if (fileNameWithoutExt == moduleName + "Instance")
                return true;
            if (fileNameWithoutExt == moduleName + "Bootstrap")
                return true;
            if (fileNameWithoutExt.StartsWith("E" + moduleName))
                return true;

            return false;
        }

        void CheckEditorScriptInRuntime(string fileName, string reportPath, ValidationReport report)
        {
            if (fileName.Contains(EDITOR_FOLDER_NAME))
            {
                report.AddError(VALIDATOR_NAME,
                    "Editor-related file found in runtime module root: " + fileName + ".cs — move to Tests/Editor or a dedicated Editor folder.",
                    reportPath);
            }
        }

        void CheckUIFileInNonUIModule(string fileName, string moduleName, string reportPath, ValidationReport report)
        {
            bool isUIModule = moduleName.Contains("UI") || moduleName.Contains("Hud") || moduleName.Contains("HUD");
            if (isUIModule)
                return;

            for (int i = 0; i < UI_KEYWORDS.Length; i++)
            {
                if (fileName.Contains(UI_KEYWORDS[i]))
                {
                    report.AddWarning(VALIDATOR_NAME,
                        "UI-related file '" + fileName + ".cs' found inside non-UI module '" + moduleName + "'. Consider moving to a UI module.",
                        reportPath);
                    return;
                }
            }
        }

        void CheckSuspiciousGameplayFile(string fileName, string moduleName, string reportPath, ValidationReport report)
        {
            for (int i = 0; i < SUSPICIOUS_GAMEPLAY_SUFFIXES.Length; i++)
            {
                string suffix = SUSPICIOUS_GAMEPLAY_SUFFIXES[i];
                if (fileName.EndsWith(suffix) && !fileName.StartsWith(moduleName))
                {
                    report.AddWarning(VALIDATOR_NAME,
                        "Suspicious gameplay file '" + fileName + ".cs' in module '" + moduleName + "'. File name does not start with module name — might belong elsewhere.",
                        reportPath);
                    return;
                }
            }
        }

        void ValidateTestsFolder(string moduleFullPath, string moduleName, string moduleRelative, ValidationReport report)
        {
            string testsPath = Path.Combine(moduleFullPath, TESTS_FOLDER_NAME);
            if (!Directory.Exists(testsPath))
                return;

            string editorPath = Path.Combine(testsPath, EDITOR_FOLDER_NAME);
            if (Directory.Exists(editorPath))
            {
                string[] editorFiles = Directory.GetFiles(editorPath, CS_EXTENSION, SearchOption.TopDirectoryOnly);
                _scannedCount += editorFiles.Length;

                for (int i = 0; i < editorFiles.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(editorFiles[i]);
                    string reportPath = moduleRelative + "/" + TESTS_FOLDER_NAME + "/" + EDITOR_FOLDER_NAME + "/" + Path.GetFileName(editorFiles[i]);

                    if (!fileName.EndsWith("Tests"))
                    {
                        report.AddWarning(VALIDATOR_NAME,
                            "Non-test file '" + fileName + ".cs' found in Tests/Editor. Expected files ending with 'Tests'.",
                            reportPath);
                    }
                }
            }

            string[] testsRootFiles = Directory.GetFiles(testsPath, CS_EXTENSION, SearchOption.TopDirectoryOnly);
            _scannedCount += testsRootFiles.Length;

            for (int i = 0; i < testsRootFiles.Length; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(testsRootFiles[i]);
                string reportPath = moduleRelative + "/" + TESTS_FOLDER_NAME + "/" + Path.GetFileName(testsRootFiles[i]);

                if (!fileName.EndsWith("Tests"))
                {
                    report.AddWarning(VALIDATOR_NAME,
                        "Non-test file '" + fileName + ".cs' found in Tests root. Expected files ending with 'Tests'.",
                        reportPath);
                }
            }
        }

        void ValidateSubfolders(string moduleFullPath, string moduleName, string moduleRelative, ValidationReport report)
        {
            string[] subDirs = Directory.GetDirectories(moduleFullPath);
            for (int i = 0; i < subDirs.Length; i++)
            {
                string dirName = Path.GetFileName(subDirs[i]);
                if (dirName == TESTS_FOLDER_NAME)
                    continue;

                string dirRelative = moduleRelative + "/" + dirName;
                string[] subFiles = Directory.GetFiles(subDirs[i], CS_EXTENSION, SearchOption.AllDirectories);
                _scannedCount += subFiles.Length;

                for (int j = 0; j < subFiles.Length; j++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(subFiles[j]);
                    string reportPath = dirRelative + "/" + Path.GetFileName(subFiles[j]);

                    CheckUIFileInNonUIModule(fileName, moduleName, reportPath, report);
                    CheckSuspiciousGameplayFile(fileName, moduleName, reportPath, report);
                }
            }
        }
    }
}
