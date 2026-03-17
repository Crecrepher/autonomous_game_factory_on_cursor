using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class ValidationRunner
    {
        const string REPORT_FILENAME = "AIValidationReport.json";
        const string REPORT_SUBFOLDER = "Editor/AI";
        const string LOG_PREFIX = "[AI Validation] ";

        public static string RunValidation()
        {
            ValidationReport report = new ValidationReport();

            Debug.Log(LOG_PREFIX + "=== Validation Start ===");

            RunAllValidators(report);

            FinalizeAndWriteReport(report);

            return GetReportPath();
        }

        static void RunAllValidators(ValidationReport report)
        {
            IModuleValidator[] validators = new IModuleValidator[]
            {
                new CompileErrorValidator(),
                new ForbiddenFolderValidator(),
                new ModuleStructureValidator(),
                new ArchitectureRuleValidator()
            };
            for (int i = 0; i < validators.Length; i++)
            {
                string validatorName = validators[i].GetType().Name;
                Debug.Log(LOG_PREFIX + "Running: " + validatorName + "...");
                int fileCount = validators[i].Validate(report);
                report.ScannedFileCount += fileCount;
                Debug.Log(LOG_PREFIX + validatorName + " done. Scanned: " + fileCount + " (Errors: " + report.ErrorCount + " / Warnings: " + report.WarningCount + ")");
            }
        }

        static void FinalizeAndWriteReport(ValidationReport report)
        {
            report.FinalizeReport();
            string path = GetReportPath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string json = JsonUtility.ToJson(new ValidationReportJson(report), true);
            File.WriteAllText(path, json);

            string summary = report.Passed ? "PASSED" : "FAILED";
            Debug.Log(LOG_PREFIX + "=== Validation Complete: " + summary + " === (Total scanned: " + report.ScannedFileCount + ")");
            Debug.Log(LOG_PREFIX + "Report: " + path + " | Errors: " + report.ErrorCount + " | Warnings: " + report.WarningCount);

            if (report.ErrorCount > 0)
            {
                for (int i = 0; i < report.Entries.Count; i++)
                {
                    ValidationEntry entry = report.Entries[i];
                    if (entry.Severity == ValidationReport.SEVERITY_ERROR)
                        Debug.LogError(LOG_PREFIX + "[" + entry.ValidatorName + "] " + entry.Message + (string.IsNullOrEmpty(entry.Path) ? "" : " @ " + entry.Path));
                }
            }

            if (report.WarningCount > 0)
            {
                for (int i = 0; i < report.Entries.Count; i++)
                {
                    ValidationEntry entry = report.Entries[i];
                    if (entry.Severity == ValidationReport.SEVERITY_WARNING)
                        Debug.LogWarning(LOG_PREFIX + "[" + entry.ValidatorName + "] " + entry.Message + (string.IsNullOrEmpty(entry.Path) ? "" : " @ " + entry.Path));
                }
            }

            ShowResultDialog(report);
        }

        static void ShowResultDialog(ValidationReport report)
        {
            string status = report.Passed ? "PASSED" : "FAILED";
            string message = "Result: " + status
                + "\nScanned files: " + report.ScannedFileCount
                + "\nErrors: " + report.ErrorCount
                + "\nWarnings: " + report.WarningCount
                + "\n\nReport: " + GetReportPath()
                + "\n\nSee Console for details.";

            EditorUtility.DisplayDialog("AI Validation - " + status, message, "OK");
        }

        public static string GetReportPath()
        {
            return Path.Combine(Application.dataPath, REPORT_SUBFOLDER, REPORT_FILENAME);
        }

        [Serializable]
        struct ValidationReportJson
        {
            public string Timestamp;
            public int ScannedFileCount;
            public int ErrorCount;
            public int WarningCount;
            public bool Passed;
            public ValidationEntryJson[] Entries;

            public ValidationReportJson(ValidationReport r)
            {
                Timestamp = r.Timestamp;
                ScannedFileCount = r.ScannedFileCount;
                ErrorCount = r.ErrorCount;
                WarningCount = r.WarningCount;
                Passed = r.Passed;
                Entries = new ValidationEntryJson[r.Entries.Count];
                for (int i = 0; i < r.Entries.Count; i++)
                {
                    ValidationEntry e = r.Entries[i];
                    Entries[i] = new ValidationEntryJson { Message = e.Message, Path = e.Path, Severity = e.Severity, ValidatorName = e.ValidatorName };
                }
            }
        }

        [Serializable]
        struct ValidationEntryJson
        {
            public string Message;
            public string Path;
            public string Severity;
            public string ValidatorName;
        }
    }
}
