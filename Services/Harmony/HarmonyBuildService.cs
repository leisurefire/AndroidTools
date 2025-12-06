using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using HarmonyOSToolbox.Models.Harmony;

namespace HarmonyOSToolbox.Services.Harmony
{
    public class HarmonyBuildService
    {
        private readonly HarmonyCoreService _core;
        private EcoConfig _ecoConfig = new();

        public HarmonyBuildService(HarmonyCoreService core)
        {
            _core = core;
        }

        public async Task CheckEcoAccount(CommonInfo commonInfo)
        {
            _core.CommonInfo = commonInfo;
            
            // Try load auth info
            try
            {
                var authInfoJson = _core.Dh.ReadFileToObj<UserInfo>("ds-authInfo.json");
                if (authInfoJson != null)
                {
                    Console.WriteLine($"[账号检查] 发现已保存的认证信息");
                    
                    // 初始化 Eco 服务的认证信息
                    _core.Eco.InitCookie(authInfoJson);
                    
                    // 更新账号状态 - 已登录
                    await UpdateStep(_core.AccountInfo.Steps, "登录华为账号", true, $"已登录: {authInfoJson.NickName ?? authInfoJson.UserId}");
                    
                    Console.WriteLine($"[账号检查] 账号已登录: {authInfoJson.NickName ?? authInfoJson.UserId}");
                    
                    // 检查是否已有证书和 Profile 配置
                    _ecoConfig = _core.Dh.ReadFileToObj<EcoConfig>("eco_config.json") ?? new EcoConfig();
                    
                    // 检查证书状态
                    if (!string.IsNullOrEmpty(_ecoConfig.DebugCert?.Path) && File.Exists(_ecoConfig.DebugCert.Path))
                    {
                        await UpdateStep(_core.AccountInfo.Steps, "申请调试证书", true, "已申请");
                        Console.WriteLine($"[账号检查] 调试证书已存在");
                    }
                    else
                    {
                        await UpdateStep(_core.AccountInfo.Steps, "申请调试证书", false, "需要申请");
                    }
                    
                    // 检查设备注册状态（简化，实际需要调用 API 验证）
                    if (!string.IsNullOrEmpty(commonInfo.DeviceIp))
                    {
                        await UpdateStep(_core.AccountInfo.Steps, "注册调试设备", true, "设备已连接");
                    }
                    else
                    {
                        await UpdateStep(_core.AccountInfo.Steps, "注册调试设备", false, "需要连接设备");
                    }
                    
                    // 检查 Profile 状态
                    if (!string.IsNullOrEmpty(_ecoConfig.DebugProfile?.Path) && File.Exists(_ecoConfig.DebugProfile.Path))
                    {
                        await UpdateStep(_core.AccountInfo.Steps, "申请调试Profile", true, "已申请");
                        Console.WriteLine($"[账号检查] 调试 Profile 已存在");
                    }
                    else
                    {
                        await UpdateStep(_core.AccountInfo.Steps, "申请调试Profile", false, "需要申请");
                    }
                }
                else
                {
                    Console.WriteLine($"[账号检查] 未找到已保存的认证信息");
                    await UpdateStep(_core.AccountInfo.Steps, "登录华为账号", false, "未登录");
                    await UpdateStep(_core.AccountInfo.Steps, "申请调试证书", false, "需要先登录");
                    await UpdateStep(_core.AccountInfo.Steps, "注册调试设备", false, "需要先登录");
                    await UpdateStep(_core.AccountInfo.Steps, "申请调试Profile", false, "需要先登录");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[账号检查] 检查失败: {ex.Message}");
                await UpdateStep(_core.AccountInfo.Steps, "登录华为账号", false, "检查失败");
            }
        }

        public async Task StartBuild(CommonInfo commonInfo)
        {
            _core.CommonInfo = commonInfo;
            
            // 1. Create Cert if needed
            // 2. Create Profile if needed
            // 3. Sign
            // 4. Install
            
            try 
            {
                await SignAndInstall();
            }
            catch (Exception ex)
            {
                // Handle error, update step status
                Console.WriteLine($"Build failed: {ex.Message}");
                throw; 
            }
        }

        public async Task SignAndInstall()
        {
            // Use default debug cert/profile for now or logic from JS
            // Assuming _ecoConfig has valid paths or we create them.
            
            // Simplified logic for migration prototype:
            // 1. Check if Cert/Profile exists locally
            // 2. If not, call Eco service (omitted for brevity in this step, needs full implementation)
            // 3. Sign
            
            var signConfig = new SignConfig
            {
                KeystoreFile = Path.Combine(_core.Dh.ConfigDir, _ecoConfig.Keystore ?? "xiaobai.p12"),
                KeystorePwd = _ecoConfig.Storepass ?? "xiaobai123",
                KeyAlias = _ecoConfig.KeyAlias ?? "xiaobai",
                CertFile = _ecoConfig.DebugCert?.Path ?? string.Empty,
                ProfileFile = _ecoConfig.DebugProfile?.Path ?? string.Empty,
                InFile = _core.CommonInfo.HapPath,
                OutFile = Path.Combine(_core.Dh.SignedDir, "signed.hap")
            };

            if (string.IsNullOrEmpty(signConfig.CertFile) || !File.Exists(signConfig.CertFile))
            {
                 // In real app, trigger creation flow
                 throw new Exception("Certificate not found. Please configure in settings or login.");
            }

            await _core.Cmd.SignHap(signConfig);
            
            await _core.Cmd.SendAndInstall(signConfig.OutFile, _core.CommonInfo.DeviceIp);
        }

        private async Task UpdateStep(List<StepInfo> steps, string name, bool finish, string? message = null)
        {
            var step = steps.FirstOrDefault(s => s.Name == name);
            if (step != null)
            {
                step.Finish = finish;
                step.Loading = !finish;
                if (message != null)
                {
                    step.Message = message;
                }
            }
            await Task.CompletedTask;
        }
    }
}
