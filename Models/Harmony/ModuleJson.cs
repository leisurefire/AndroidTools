using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HarmonyOSToolbox.Models.Harmony
{
    public class ModuleJson
    {
        [JsonPropertyName("app")]
        public AppInfo App { get; set; }

        [JsonPropertyName("module")]
        public ModuleInfo Module { get; set; }
    }

    public class AppInfo
    {
        [JsonPropertyName("bundleName")]
        public string BundleName { get; set; }

        [JsonPropertyName("versionName")]
        public string VersionName { get; set; }

        [JsonPropertyName("vendor")]
        public string Vendor { get; set; }

        [JsonPropertyName("versionCode")]
        public int VersionCode { get; set; }
    }

    public class ModuleInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("requestPermissions")]
        public List<PermissionInfo> RequestPermissions { get; set; }

        [JsonPropertyName("abilities")]
        public List<AbilityInfo> Abilities { get; set; }
    }

    public class PermissionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class AbilityInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("label")]
        public string Label { get; set; }
        
        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }
}
