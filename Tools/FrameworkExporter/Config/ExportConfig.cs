using System.Text.Json.Serialization;

namespace FrameworkExporter.Config
{
    public enum EExportMode
    {
        Framework,
        Full
    }

    public sealed class ExportConfig
    {
        [JsonPropertyName("sourceRoot")]
        public string SourceRoot { get; set; } = ".";

        [JsonPropertyName("exportRoot")]
        public string ExportRoot { get; set; } = "./ExportPackage";

        [JsonPropertyName("dryRun")]
        public bool DryRun { get; set; }

        [JsonPropertyName("exportMode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EExportMode ExportMode { get; set; } = EExportMode.Framework;

        [JsonPropertyName("frameworkPaths")]
        public string[] FrameworkPaths { get; set; } = Array.Empty<string>();

        [JsonPropertyName("gameplayModulePaths")]
        public string[] GameplayModulePaths { get; set; } = Array.Empty<string>();

        [JsonPropertyName("additionalAllowedPaths")]
        public string[] AdditionalAllowedPaths { get; set; } = Array.Empty<string>();

        [JsonPropertyName("deniedPaths")]
        public string[] DeniedPaths { get; set; } = Array.Empty<string>();

        [JsonPropertyName("deniedExtensions")]
        public string[] DeniedExtensions { get; set; } = Array.Empty<string>();

        [JsonPropertyName("riskyExtensions")]
        public string[] RiskyExtensions { get; set; } = Array.Empty<string>();

        [JsonPropertyName("riskyWarning")]
        public bool RiskyWarning { get; set; } = true;

        [JsonPropertyName("includeMetaFiles")]
        public bool IncludeMetaFiles { get; set; } = true;

        [JsonPropertyName("includeOnlyChangedFiles")]
        public bool IncludeOnlyChangedFiles { get; set; }

        [JsonPropertyName("confirmBeforeExport")]
        public bool ConfirmBeforeExport { get; set; } = true;

        public string[] GetEffectiveAllowedPaths()
        {
            int frameworkLen = FrameworkPaths.Length;
            int gameplayLen = ExportMode == EExportMode.Full ? GameplayModulePaths.Length : 0;
            int additionalLen = AdditionalAllowedPaths.Length;
            int totalLength = frameworkLen + gameplayLen + additionalLen;

            string[] merged = new string[totalLength];
            int offset = 0;

            Array.Copy(FrameworkPaths, 0, merged, offset, frameworkLen);
            offset += frameworkLen;

            if (gameplayLen > 0)
            {
                Array.Copy(GameplayModulePaths, 0, merged, offset, gameplayLen);
                offset += gameplayLen;
            }

            Array.Copy(AdditionalAllowedPaths, 0, merged, offset, additionalLen);
            return merged;
        }
    }
}
