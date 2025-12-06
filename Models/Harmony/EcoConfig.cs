namespace HarmonyOSToolbox.Models.Harmony
{
    public class EcoConfig
    {
        public string TeamId { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public string Keystore { get; set; } = string.Empty;
        public string Storepass { get; set; } = "xiaobai123";
        public string KeyAlias { get; set; } = "xiaobai";
        public string OutPath { get; set; } = string.Empty;
        public CertInfo? DebugCert { get; set; }
        public ProfileInfo? DebugProfile { get; set; }
    }

    public class CertInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public class ProfileInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
