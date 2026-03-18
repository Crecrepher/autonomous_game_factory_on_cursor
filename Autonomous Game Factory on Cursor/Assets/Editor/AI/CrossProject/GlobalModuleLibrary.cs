using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Editor.AI.CrossProject
{
    public static class GlobalModuleLibrary
    {
        const string CATALOG_PATH = "GlobalModules/GLOBAL_MODULE_CATALOG.yaml";
        const string GLOBAL_MODULES_DIR = "GlobalModules";
        const string LOG_PREFIX = "[GlobalLib] ";

        static readonly Regex REGEX_MOD_NAME = new Regex(@"^\s*-\s*name:\s*(.+)");
        static readonly Regex REGEX_CATEGORY = new Regex(@"^\s*category:\s*(.+)");
        static readonly Regex REGEX_SOURCE = new Regex(@"^\s*source_project:\s*(.+)");
        static readonly Regex REGEX_STABILITY = new Regex(@"^\s*stability:\s*(.+)");
        static readonly Regex REGEX_DESCRIPTION = new Regex(@"^\s*description:\s*""?(.+?)""?\s*$");

        public struct GlobalModule
        {
            public string Name;
            public string Category;
            public string SourceProject;
            public string Stability;
            public string Description;
        }

        public struct LibrarySearchResult
        {
            public GlobalModule Module;
            public float RelevanceScore;
        }

        public static GlobalModule[] LoadCatalog()
        {
            string path = Path.Combine(Application.dataPath, "..", CATALOG_PATH);
            if (!File.Exists(path)) return new GlobalModule[0];

            string[] lines = File.ReadAllLines(path);
            List<GlobalModule> modules = new List<GlobalModule>();
            GlobalModule current = new GlobalModule();
            bool inModules = false;
            bool inEntry = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("modules:"))
                {
                    inModules = true;
                    continue;
                }

                if (!inModules) continue;

                Match m = REGEX_MOD_NAME.Match(lines[i]);
                if (m.Success)
                {
                    if (inEntry) modules.Add(current);
                    current = new GlobalModule { Name = m.Groups[1].Value.Trim() };
                    inEntry = true;
                    continue;
                }

                if (!inEntry) continue;

                m = REGEX_CATEGORY.Match(lines[i]);
                if (m.Success) { current.Category = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_SOURCE.Match(lines[i]);
                if (m.Success) { current.SourceProject = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_STABILITY.Match(lines[i]);
                if (m.Success) { current.Stability = m.Groups[1].Value.Trim(); continue; }

                m = REGEX_DESCRIPTION.Match(lines[i]);
                if (m.Success) { current.Description = m.Groups[1].Value.Trim(); continue; }
            }

            if (inEntry) modules.Add(current);
            return modules.ToArray();
        }

        public static LibrarySearchResult[] Search(string query, string categoryFilter)
        {
            GlobalModule[] catalog = LoadCatalog();
            string queryLower = query.ToLower();
            string[] queryWords = queryLower.Split(' ', ',', '.', '/', '-');

            List<LibrarySearchResult> results = new List<LibrarySearchResult>();

            for (int i = 0; i < catalog.Length; i++)
            {
                GlobalModule mod = catalog[i];

                if (!string.IsNullOrEmpty(categoryFilter) && mod.Category != categoryFilter)
                    continue;

                float score = 0f;
                string nameLower = mod.Name.ToLower();
                string descLower = mod.Description != null ? mod.Description.ToLower() : "";
                string catLower = mod.Category != null ? mod.Category.ToLower() : "";

                for (int w = 0; w < queryWords.Length; w++)
                {
                    if (queryWords[w].Length < 2) continue;

                    if (nameLower.Contains(queryWords[w])) score += 0.4f;
                    if (descLower.Contains(queryWords[w])) score += 0.3f;
                    if (catLower.Contains(queryWords[w])) score += 0.2f;
                }

                if (mod.Stability == "high") score += 0.1f;

                if (score > 0.2f)
                {
                    results.Add(new LibrarySearchResult
                    {
                        Module = mod,
                        RelevanceScore = score
                    });
                }
            }

            for (int i = 0; i < results.Count - 1; i++)
            {
                for (int j = i + 1; j < results.Count; j++)
                {
                    if (results[j].RelevanceScore > results[i].RelevanceScore)
                    {
                        LibrarySearchResult tmp = results[i];
                        results[i] = results[j];
                        results[j] = tmp;
                    }
                }
            }

            return results.ToArray();
        }

        public static bool ExportModule(string moduleName, string projectName)
        {
            string srcDir = Path.Combine(Application.dataPath, "..", "Assets/Game/Modules", moduleName);
            if (!Directory.Exists(srcDir))
            {
                Debug.LogError(LOG_PREFIX + "Module not found: " + srcDir);
                return false;
            }

            string destDir = Path.Combine(Application.dataPath, "..", GLOBAL_MODULES_DIR, moduleName);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            string[] files = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relativePath = files[i].Substring(srcDir.Length + 1);
                string destFile = Path.Combine(destDir, relativePath);
                string destSubDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destSubDir) && !Directory.Exists(destSubDir))
                    Directory.CreateDirectory(destSubDir);
                File.Copy(files[i], destFile, true);
            }

            Debug.Log(LOG_PREFIX + "Exported " + moduleName + " (" + files.Length + " files) to GlobalModules/");
            return true;
        }

        public static bool ImportModule(string moduleName)
        {
            string srcDir = Path.Combine(Application.dataPath, "..", GLOBAL_MODULES_DIR, moduleName);
            if (!Directory.Exists(srcDir))
            {
                Debug.LogError(LOG_PREFIX + "Global module not found: " + srcDir);
                return false;
            }

            string destDir = Path.Combine(Application.dataPath, "..", "Assets/Game/Modules", moduleName);
            if (Directory.Exists(destDir))
            {
                Debug.LogWarning(LOG_PREFIX + "Module already exists locally: " + moduleName + ". Skipping.");
                return false;
            }

            Directory.CreateDirectory(destDir);
            string[] files = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relativePath = files[i].Substring(srcDir.Length + 1);
                string destFile = Path.Combine(destDir, relativePath);
                string destSubDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destSubDir) && !Directory.Exists(destSubDir))
                    Directory.CreateDirectory(destSubDir);
                File.Copy(files[i], destFile, true);
            }

            Debug.Log(LOG_PREFIX + "Imported " + moduleName + " (" + files.Length + " files) from GlobalModules/");
            return true;
        }

        public static string FormatCatalog()
        {
            GlobalModule[] catalog = LoadCatalog();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Global Module Library — " + catalog.Length + " modules");
            sb.AppendLine();

            string currentCat = "";
            for (int i = 0; i < catalog.Length; i++)
            {
                GlobalModule mod = catalog[i];
                if (mod.Category != currentCat)
                {
                    currentCat = mod.Category;
                    sb.AppendLine("## " + currentCat);
                }
                sb.AppendLine("- **" + mod.Name + "** [" + mod.Stability + "] — " + mod.Description);
                sb.AppendLine("  Source: " + mod.SourceProject);
            }

            return sb.ToString();
        }

        [UnityEditor.MenuItem("Tools/AI/CPIL/Show Global Module Library")]
        static void ShowLibrary()
        {
            Debug.Log(FormatCatalog());
        }

        [UnityEditor.MenuItem("Tools/AI/CPIL/Search Global Modules (Test — Inventory)")]
        static void SearchTest()
        {
            LibrarySearchResult[] results = Search("inventory item slot stack", null);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[GlobalLib] Search results for 'inventory item slot stack':");
            for (int i = 0; i < results.Length; i++)
            {
                sb.AppendLine("  " + (i + 1) + ". " + results[i].Module.Name
                    + " (score: " + results[i].RelevanceScore.ToString("F2")
                    + ", stability: " + results[i].Module.Stability + ")");
            }
            Debug.Log(sb.ToString());
        }
    }
}
