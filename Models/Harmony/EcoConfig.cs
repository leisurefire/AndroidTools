namespace HarmonyOSToolbox.Models.Harmony
{
    public class EcoConfig
    {
        public string TeamId { get; set; }
        public string Uid { get; set; }
        public string Keystore { get; set; }
        public string Storepass { get; set; } = "xiaobai123";
        public string KeyAlias { get; set; } = "xiaobai";
        public string OutPath { get; set; }
        public CertInfo DebugCert { get; set; }
        public ProfileInfo DebugProfile { get; set; }
    }

    public class CertInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class ProfileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}
