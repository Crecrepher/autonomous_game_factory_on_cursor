using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AI
{
    public static class ValidateGeneratedModulesMenu
    {
        const string MENU_VALIDATE = "Tools/AI/Validate Generated Modules";
        const string MENU_UPDATE_BASELINE = "Tools/AI/Update Core Baseline";

        [MenuItem(MENU_VALIDATE)]
        public static void RunValidation()
        {
            string reportPath = ValidationRunner.RunValidation(triggerRecompile: true);
            EditorUtility.DisplayDialog(
                "AI Module Validation",
                "Validation started. Compilation will run; when it finishes, the report will be written to:\n" + reportPath + "\n\nCheck the Console for the result.",
                "OK");
        }

        [MenuItem(MENU_UPDATE_BASELINE)]
        public static void UpdateCoreBaseline()
        {
            const string CORE_RELATIVE = "Game/Core";
            const string BASELINE_FOLDER = "Editor/AI/Baseline";
            const string CORE_BASELINE_FILENAME = "CoreFiles.txt";

            string corePath = Path.Combine(Application.dataPath, CORE_RELATIVE);
            if (!Directory.Exists(corePath))
            {
                EditorUtility.DisplayDialog("Update Core Baseline", "Core folder not found: " + corePath, "OK");
                return;
            }

            string[] files = Directory.GetFiles(corePath, "*.cs", SearchOption.AllDirectories);
            string baselineDir = Path.Combine(Application.dataPath, BASELINE_FOLDER);
            if (!Directory.Exists(baselineDir))
                Directory.CreateDirectory(baselineDir);

            string baselinePath = Path.Combine(baselineDir, CORE_BASELINE_FILENAME);
            using (var writer = new StreamWriter(baselinePath, false))
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string relative = GetRelativePathFromDataPath(files[i]);
                    if (!string.IsNullOrEmpty(relative))
                        writer.WriteLine(relative.Replace("\\", "/"));
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Update Core Baseline", "Baseline written: " + baselinePath + "\nLines: " + files.Length, "OK");
        }

        static string GetRelativePathFromDataPath(string fullPath)
        {
            string dataPath = Application.dataPath;
            if (!fullPath.StartsWith(dataPath))
                return null;
            string sub = fullPath.Substring(dataPath.Length);
            if (sub.StartsWith("/") || sub.StartsWith("\\"))
                sub = sub.Substring(1);
            return "Assets/" + sub;
        }
    }
}
