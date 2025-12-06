using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HarmonyOSToolbox.Models.Harmony
{
    public class ModuleJson
    {
        [JsonPropertyName("app")]
        public AppInfo App { get; set; } = new();

        [JsonPropertyName("module")]
        public ModuleInfo Module { get; set; } = new();
    }

    public class AppInfo
    {
        [JsonPropertyName("bundleName")]
        public string BundleName { get; set; } = string.Empty;

        [JsonPropertyName("versionName")]
        public string VersionName { get; set; } = string.Empty;

        [JsonPropertyName("vendor")]
        public string Vendor { get; set; } = string.Empty;

        [JsonPropertyName("versionCode")]
        public int VersionCode { get; set; }
    }

    public class ModuleInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("requestPermissions")]
        public List<PermissionInfo> RequestPermissions { get; set; } = new();

        [JsonPropertyName("abilities")]
        public List<AbilityInfo> Abilities { get; set; } = new();
    }

    public class PermissionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class AbilityInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
        
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }
}
