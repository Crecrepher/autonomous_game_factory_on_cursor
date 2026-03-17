using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class ValidationRunner
    {
        const string REPORT_FILENAME = "AIValidationReport.json";
        const string REPORT_SUBFOLDER = "Editor/AI";

        static List<CompilerMessage> _compileErrors;
        static Action _onCompilationComplete;

        public static string RunValidation(bool triggerRecompile = true)
        {
            var report = new ValidationReport();
            _compileErrors = new List<CompilerMessage>();

            RunSyncValidators(report);

            if (triggerRecompile)
            {
                _onCompilationComplete = () =>
                {
                    CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
                    CompileErrorValidator.AddCompileMessagesToReport(report, _compileErrors);
                    FinalizeAndWriteReport(report);
                    _onCompilationComplete = null;
                };
                CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
                CompilationPipeline.RequestScriptCompilation();
                EditorApplication.update += WaitForCompilationEnd;
            }
            else
            {
                FinalizeAndWriteReport(report);
                return GetReportPath();
            }

            return GetReportPath();
        }

        static void OnAssemblyCompilationFinished(string path, CompilerMessage[] messages)
        {
            if (messages == null)
                return;
            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i].type == CompilerMessageType.Error)
                    _compileErrors.Add(messages[i]);
            }
        }

        static void WaitForCompilationEnd()
        {
            if (EditorApplication.isCompiling)
                return;
            EditorApplication.update -= WaitForCompilationEnd;
            if (_onCompilationComplete != null)
                _onCompilationComplete();
        }

        static void RunSyncValidators(ValidationReport report)
        {
            IModuleValidator[] validators = new IModuleValidator[]
            {
                new ForbiddenFolderValidator(),
                new ModuleStructureValidator(),
                new ArchitectureRuleValidator()
            };
            for (int i = 0; i < validators.Length; i++)
                validators[i].Validate(report);
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
            Debug.Log("[AI Validation] Report written: " + path + " | Passed: " + report.Passed + " | Errors: " + report.ErrorCount + " | Warnings: " + report.WarningCount);
        }

        public static string GetReportPath()
        {
            return Path.Combine(Application.dataPath, REPORT_SUBFOLDER, REPORT_FILENAME);
        }

        [Serializable]
        struct ValidationReportJson
        {
            public string Timestamp;
            public int ErrorCount;
            public int WarningCount;
            public bool Passed;
            public ValidationEntryJson[] Entries;

            public ValidationReportJson(ValidationReport r)
            {
                Timestamp = r.Timestamp;
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
