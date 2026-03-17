using UnityEditor;

namespace Game.Editor.AI
{
    public class CompileErrorValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "CompileError";

        public int Validate(ValidationReport report)
        {
            if (!EditorUtility.scriptCompilationFailed)
                return 0;

            report.AddError(VALIDATOR_NAME, "Script compilation has errors. Check Unity Console for details.");
            return 0;
        }
    }
}
