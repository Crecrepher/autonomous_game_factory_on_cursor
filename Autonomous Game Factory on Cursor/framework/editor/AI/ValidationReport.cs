using System;
using System.Collections.Generic;

namespace Game.Editor.AI
{
    [Serializable]
    public class ValidationEntry
    {
        public string Message;
        public string Path;
        public string Severity;
        public string ValidatorName;

        public ValidationEntry(string message, string path, string severity, string validatorName)
        {
            Message = message;
            Path = path ?? string.Empty;
            Severity = severity;
            ValidatorName = validatorName;
        }
    }

    [Serializable]
    public class ValidationReport
    {
        public const string SEVERITY_ERROR = "Error";
        public const string SEVERITY_WARNING = "Warning";

        public string Timestamp;
        public int ErrorCount;
        public int WarningCount;
        public bool Passed;
        public List<ValidationEntry> Entries;

        public ValidationReport()
        {
            Timestamp = DateTime.UtcNow.ToString("o");
            Entries = new List<ValidationEntry>();
        }

        public void AddError(string validatorName, string message, string path = null)
        {
            Entries.Add(new ValidationEntry(message, path, SEVERITY_ERROR, validatorName));
            ErrorCount++;
        }

        public void AddWarning(string validatorName, string message, string path = null)
        {
            Entries.Add(new ValidationEntry(message, path, SEVERITY_WARNING, validatorName));
            WarningCount++;
        }

        public void FinalizeReport()
        {
            Passed = ErrorCount == 0;
        }
    }
}
