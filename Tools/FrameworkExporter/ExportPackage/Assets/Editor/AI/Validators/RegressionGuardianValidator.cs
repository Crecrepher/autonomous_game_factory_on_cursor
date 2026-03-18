namespace Game.Editor.AI
{
    public class RegressionGuardianValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "RegressionGuardian";

        public int Validate(ValidationReport report)
        {
            RegressionGuardian.RegressionReport regression = RegressionGuardian.RunFullScan();

            for (int i = 0; i < regression.Issues.Length; i++)
            {
                RegressionGuardian.RegressionIssue issue = regression.Issues[i];

                if (issue.Severity == "critical")
                {
                    report.AddError(VALIDATOR_NAME,
                        "[" + issue.Type + "] " + issue.Detail,
                        issue.Module);
                }
                else if (issue.Severity == "high")
                {
                    report.AddWarning(VALIDATOR_NAME,
                        "[" + issue.Type + "] " + issue.Detail,
                        issue.Module);
                }
            }

            return regression.TotalIssues;
        }
    }
}
