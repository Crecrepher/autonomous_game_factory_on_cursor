using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI.CrossProject
{
    public static class IntelligenceFeedbackLoop
    {
        const string LOG_PREFIX = "[FeedbackLoop] ";
        const string MODULES_ROOT = "Assets/Game/Modules";
        const string CATALOG_PATH = "GlobalModules/GLOBAL_MODULE_CATALOG.yaml";

        public struct FeedbackReport
        {
            public int ModulesExported;
            public int LearningEventsRecorded;
            public int PatternsReinforced;
            public string[] ExportedModuleNames;
        }

        public static FeedbackReport RunPostPipelineFeedback(string projectName)
        {
            int exported = 0;
            int learned = 0;
            int reinforced = 0;

            string[] exportedNames = ExportStableModules(projectName, ref exported);
            RecordValidatorLessons(projectName, ref learned);
            ReinforceGlobalRules(ref reinforced);

            FeedbackReport report = new FeedbackReport
            {
                ModulesExported = exported,
                LearningEventsRecorded = learned,
                PatternsReinforced = reinforced,
                ExportedModuleNames = exportedNames
            };

            Debug.Log(LOG_PREFIX + "Feedback complete. Exported: " + exported
                + ", Learned: " + learned + ", Reinforced: " + reinforced);

            return report;
        }

        static string[] ExportStableModules(string projectName, ref int count)
        {
            string modulesDir = Path.Combine(Application.dataPath, "..", MODULES_ROOT);
            if (!Directory.Exists(modulesDir)) return new string[0];

            string[] moduleDirs = Directory.GetDirectories(modulesDir);
            var exported = new System.Collections.Generic.List<string>();

            for (int i = 0; i < moduleDirs.Length; i++)
            {
                string dirName = Path.GetFileName(moduleDirs[i]);
                if (dirName == "Template") continue;

                string runtimePath = Path.Combine(moduleDirs[i], dirName + "Runtime.cs");
                string interfacePath = Path.Combine(moduleDirs[i], "I" + dirName + ".cs");
                string configPath = Path.Combine(moduleDirs[i], dirName + "Config.cs");
                string factoryPath = Path.Combine(moduleDirs[i], dirName + "Factory.cs");

                bool hasAllFiles = File.Exists(runtimePath) && File.Exists(interfacePath)
                    && File.Exists(configPath) && File.Exists(factoryPath);

                if (!hasAllFiles) continue;

                string globalDir = Path.Combine(Application.dataPath, "..", "GlobalModules", dirName);
                if (Directory.Exists(globalDir)) continue;

                bool success = GlobalModuleLibrary.ExportModule(dirName, projectName);
                if (success)
                {
                    exported.Add(dirName);
                    count++;
                }
            }

            return exported.ToArray();
        }

        static void RecordValidatorLessons(string projectName, ref int count)
        {
            string reportPath = Path.Combine(Application.dataPath, "Editor/AI/AIValidationReport.json");
            if (!File.Exists(reportPath)) return;

            string json = File.ReadAllText(reportPath);
            if (json.Contains("\"Passed\":true") || json.Contains("\"Passed\": true"))
            {
                CrossProjectLearning.AppendLearningEvent(
                    projectName, "Pipeline Success",
                    "파이프라인이 에러 없이 완료됨",
                    "정상 실행",
                    "N/A",
                    "N/A",
                    "project-specific");
                count++;
            }
            else
            {
                Regex errorMsg = new Regex(@"""Message""\s*:\s*""([^""]+)""");
                MatchCollection matches = errorMsg.Matches(json);
                int maxRecord = matches.Count > 3 ? 3 : matches.Count;
                for (int i = 0; i < maxRecord; i++)
                {
                    string msg = matches[i].Groups[1].Value;
                    if (msg.Length > 100) msg = msg.Substring(0, 100) + "...";

                    CrossProjectLearning.AppendLearningEvent(
                        projectName, "Validator Failure",
                        msg,
                        "코드가 규칙 위반",
                        "Validator 자동 감지",
                        "N/A",
                        "cross-project");
                    count++;
                }
            }
        }

        static void ReinforceGlobalRules(ref int count)
        {
            CrossProjectLearning.GlobalRule[] rules = CrossProjectLearning.LoadGlobalRules();
            count = rules.Length;
        }

        public static string FormatReport(FeedbackReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Intelligence Feedback Report");
            sb.AppendLine();
            sb.AppendLine("- Modules Exported to Global Library: " + report.ModulesExported);
            sb.AppendLine("- Learning Events Recorded: " + report.LearningEventsRecorded);
            sb.AppendLine("- Global Rules Reinforced: " + report.PatternsReinforced);
            sb.AppendLine();

            if (report.ExportedModuleNames != null && report.ExportedModuleNames.Length > 0)
            {
                sb.AppendLine("## Exported Modules");
                for (int i = 0; i < report.ExportedModuleNames.Length; i++)
                    sb.AppendLine("- " + report.ExportedModuleNames[i]);
            }

            return sb.ToString();
        }

        [UnityEditor.MenuItem("Tools/AI/CPIL/Run Intelligence Feedback Loop")]
        static void RunFromMenu()
        {
            FeedbackReport report = RunPostPipelineFeedback("luna_lumberchopper");
            Debug.Log(FormatReport(report));
        }
    }
}
