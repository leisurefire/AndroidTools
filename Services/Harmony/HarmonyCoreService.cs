using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using HarmonyOSToolbox.Models.Harmony;

namespace HarmonyOSToolbox.Services.Harmony
{
    public class HarmonyCoreService
    {
        public HarmonyDownloadHelper Dh { get; }
        public HarmonyCmdService Cmd { get; }
        public HarmonyEcoService Eco { get; }
        public HarmonyBuildService Build { get; }

        public CommonInfo CommonInfo { get; set; } = new CommonInfo();
        public EnvInfo EnvInfo { get; set; } = new EnvInfo();
        public AccountInfo AccountInfo { get; set; } = new AccountInfo();
        public BuildInfo BuildInfo { get; set; } = new BuildInfo();

        public HarmonyCoreService()
        {
            Dh = new HarmonyDownloadHelper();
            Cmd = new HarmonyCmdService();
            Eco = new HarmonyEcoService(); 
            Build = new HarmonyBuildService(this);

            InitializeInfo();
        }
        
        private void InitializeInfo()
        {
            EnvInfo.Steps = new List<StepInfo> {
                 new StepInfo { Name = "检查Node环境", Finish = true, Message = "内置Node.js环境" },
                 new StepInfo { Name = "检查Java环境", Finish = true, Message = "内置JBR 17" },
                 new StepInfo { Name = "检查HDC工具", Finish = true, Message = "内置HDC工具" },
                 new StepInfo { Name = "检查OHPM", Finish = true, Message = "内置OHPM" }
            };
            
            AccountInfo.Steps = new List<StepInfo> {
                new StepInfo { Name = "登录华为账号", Finish = false, Message = "未登录" },
                new StepInfo { Name = "申请调试证书", Finish = false, Message = "未申请" },
                new StepInfo { Name = "注册调试设备", Finish = false, Message = "未注册" },
                new StepInfo { Name = "申请调试Profile", Finish = false, Message = "未申请" }
            };
            
            BuildInfo.Steps = new List<StepInfo> {
                new StepInfo { Name = "解析HAP", Finish = false },
                new StepInfo { Name = "签名HAP", Finish = false },
                new StepInfo { Name = "连接设备", Finish = false },
                new StepInfo { Name = "安装应用", Finish = false }
            };
        }

        public async Task<HapInfo> SaveFileToLocal(byte[] buffer, string fileName)
        {
             var filePath = Path.Combine(Dh.HapDir, fileName);
             await File.WriteAllBytesAsync(filePath, buffer);
             
             ModuleJson? moduleJson = null;
             try {
                moduleJson = Cmd.LoadModuleJson(filePath);
             } catch (Exception ex) {
                Console.WriteLine("Parse HAP failed: " + ex.Message);
             }
             
             var info = new HapInfo
             {
                 HapPath = filePath,
                 PackageName = moduleJson?.App?.BundleName ?? "Unknown",
                 AppName = moduleJson?.Module?.Abilities?[0]?.Label ?? "Unknown",
                 VersionName = moduleJson?.App?.VersionName ?? "1.0.0",
                 Icon = "" 
             };
             
             CommonInfo.HapPath = filePath;
             CommonInfo.PackageName = info.PackageName;
             CommonInfo.AppName = info.AppName;
             
             return info;
        }
        
        public async Task<List<string>> GetGitBranches(string url)
        {
            await Task.Delay(100);
            return new List<string> { "master", "main" };
        }

        public async Task<EnvInfo> GetEnvInfo()
        {
            var tools = await Cmd.CheckTools();
            
            EnvInfo.ToolPaths = tools;
            EnvInfo.Steps = new List<StepInfo>
            {
                new StepInfo 
                { 
                    Name = "检查Node环境", 
                    Finish = !string.IsNullOrEmpty(tools.GetValueOrDefault("Node")), 
                    Message = !string.IsNullOrEmpty(tools.GetValueOrDefault("Node")) ? "已安装" : "未找到Node.js" 
                },
                new StepInfo 
                { 
                    Name = "检查Java环境", 
                    Finish = !string.IsNullOrEmpty(tools.GetValueOrDefault("Java")), 
                    Message = !string.IsNullOrEmpty(tools.GetValueOrDefault("Java")) ? "已安装" : "未找到JBR" 
                },
                new StepInfo 
                { 
                    Name = "检查HDC工具", 
                    Finish = !string.IsNullOrEmpty(tools.GetValueOrDefault("HDC")), 
                    Message = !string.IsNullOrEmpty(tools.GetValueOrDefault("HDC")) ? "已安装" : "未找到HDC" 
                },
                new StepInfo 
                { 
                    Name = "检查OHPM", 
                    Finish = !string.IsNullOrEmpty(tools.GetValueOrDefault("OHPM")), 
                    Message = !string.IsNullOrEmpty(tools.GetValueOrDefault("OHPM")) ? "已安装" : "未找到OHPM" 
                }
            };

            return EnvInfo;
        }

        public AccountInfo GetAccountInfo() => AccountInfo;
        public BuildInfo GetBuildInfo() => BuildInfo;
    }
}
