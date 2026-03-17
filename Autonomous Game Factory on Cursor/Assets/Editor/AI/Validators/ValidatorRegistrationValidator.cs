using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI
{
    public class ValidatorRegistrationValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "ValidatorRegistration";
        const string VALIDATORS_RELATIVE = "Editor/AI/Validators";
        const string RUNNER_RELATIVE = "Editor/AI/ValidationRunner.cs";
        const string CS_EXTENSION = "*.cs";

        static readonly Regex RegexClassDeclaration =
            new Regex(@"class\s+(\w+)\s*:\s*.*IModuleValidator");

        public int Validate(ValidationReport report)
        {
            string validatorsPath = Path.Combine(Application.dataPath, VALIDATORS_RELATIVE);
            if (!Directory.Exists(validatorsPath))
            {
                report.AddWarning(VALIDATOR_NAME, "Validators folder not found: " + VALIDATORS_RELATIVE, null);
                return 0;
            }

            string runnerPath = Path.Combine(Application.dataPath, RUNNER_RELATIVE);
            if (!File.Exists(runnerPath))
            {
                report.AddError(VALIDATOR_NAME, "ValidationRunner.cs not found: " + RUNNER_RELATIVE, RUNNER_RELATIVE);
                return 0;
            }

            string runnerContent = File.ReadAllText(runnerPath);
            string[] validatorFiles = Directory.GetFiles(validatorsPath, CS_EXTENSION, SearchOption.TopDirectoryOnly);
            int scannedCount = validatorFiles.Length;

            for (int i = 0; i < validatorFiles.Length; i++)
            {
                string fileContent = File.ReadAllText(validatorFiles[i]);
                Match match = RegexClassDeclaration.Match(fileContent);
                if (!match.Success)
                    continue;

                string className = match.Groups[1].Value;
                string registrationPattern = "new " + className + "()";

                if (runnerContent.Contains(registrationPattern))
                    continue;

                string fileName = Path.GetFileName(validatorFiles[i]);
                report.AddError(VALIDATOR_NAME,
                    "Validator '" + className + "' implements IModuleValidator but is NOT registered in ValidationRunner. Add 'new " + className + "()' to the validators array.",
                    "Assets/" + VALIDATORS_RELATIVE + "/" + fileName);
            }

            return scannedCount;
        }
    }
}
