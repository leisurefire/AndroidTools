using System;

namespace HarmonyOSToolbox.Models.Harmony
{
    public class CommonInfo
    {
        public string PackageName { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Github { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string DeviceIp { get; set; } = string.Empty;
        public string HapPath { get; set; } = string.Empty;
        public int Type { get; set; }
    }
}
