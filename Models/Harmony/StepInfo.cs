using System.Collections.Generic;

namespace HarmonyOSToolbox.Models.Harmony
{
    public class StepInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool Finish { get; set; }
        public string Value { get; set; } = string.Empty;
        public bool Loading { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class EnvInfo
    {
        public List<StepInfo> Steps { get; set; } = new List<StepInfo>();
        public Dictionary<string, string> ToolPaths { get; set; } = new Dictionary<string, string>();
    }

    public class AccountInfo
    {
        public List<StepInfo> Steps { get; set; } = new List<StepInfo>();
    }

    public class BuildInfo
    {
        public List<StepInfo> Steps { get; set; } = new List<StepInfo>();
        public List<StepInfo> Install { get; set; } = new List<StepInfo>();
    }
}
