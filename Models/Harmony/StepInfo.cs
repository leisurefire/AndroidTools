using System.Collections.Generic;

namespace HarmonyOSToolbox.Models.Harmony
{
    public class StepInfo
    {
        public string Name { get; set; }
        public bool Finish { get; set; }
        public string Value { get; set; }
        public bool Loading { get; set; }
        public string Message { get; set; }
        public string Url { get; set; }
    }

    public class EnvInfo
    {
        public List<StepInfo> Steps { get; set; } = new List<StepInfo>();
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
