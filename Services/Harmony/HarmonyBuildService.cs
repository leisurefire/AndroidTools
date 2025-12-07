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

        /// <summary>
        /// 申请调试证书和 Profile
        /// </summary>
        public async Task<object> ApplyCertAndProfile(CommonInfo commonInfo)
        {
            _core.CommonInfo = commonInfo;
            
            try
            {
                // 检查是否已登录
                var authInfo = _core.Dh.ReadFileToObj<UserInfo>("ds-authInfo.json");
                if (authInfo == null || string.IsNullOrEmpty(authInfo.AccessToken))
                {
                    return new { success = false, error = "请先登录华为账号" };
                }
                
                // 初始化 Eco 服务
                _core.Eco.InitCookie(authInfo);
                
                // 加载或创建 EcoConfig
                _ecoConfig = _core.Dh.ReadFileToObj<EcoConfig>("eco_config.json") ?? new EcoConfig();
                
                // 1. 检查/申请调试证书
                await UpdateStep(_core.AccountInfo.Steps, "申请调试证书", false, "正在申请...");
                
                string certId = string.Empty;
                string certPath = string.Empty;
                
                // 检查是否已有证书
                string certName = $"debug_cert_{DateTime.Now:yyyyMMddHHmmss}";
                
                if (!string.IsNullOrEmpty(_ecoConfig.DebugCert?.Id))
                {
                    certId = _ecoConfig.DebugCert.Id;
                    certPath = _ecoConfig.DebugCert.Path ?? string.Empty;
                    certName = _ecoConfig.DebugCert.Name ?? certName;
                    Console.WriteLine($"[证书申请] 使用已有证书: {certId}");
                }
                else
                {
                    // 获取证书列表
                    var certListResponse = await _core.Eco.GetCertList();
                    var existingCert = certListResponse?.CertList?.FirstOrDefault(c => c.CertType == 1); // 调试证书
                    
                    if (existingCert != null)
                    {
                        certId = existingCert.Id;
                        certName = existingCert.CertName;
                        Console.WriteLine($"[证书申请] 发现已有调试证书: {certId}");
                    }
                    else
                {
                    // 读取 CSR 文件
                    string csrPath = Path.Combine(_core.Dh.ConfigDir, "xiaobai.csr");
                    string builtInCsrPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "harmony", "store", "xiaobai.csr");
                    
                    if (!File.Exists(csrPath))
                    {
                        // 尝试从内置位置复制 CSR 文件到配置目录
                        if (File.Exists(builtInCsrPath))
                        {
                            try
                            {
                                // 确保配置目录存在
                                Directory.CreateDirectory(_core.Dh.ConfigDir);
                                File.Copy(builtInCsrPath, csrPath, overwrite: true);
                                Console.WriteLine($"[证书申请] 已复制内置CSR文件到配置目录: {csrPath}");
                            }
                            catch (Exception copyEx)
                            {
                                Console.WriteLine($"[证书申请] 复制CSR文件失败: {copyEx.Message}");
                                csrPath = builtInCsrPath; // 使用内置路径
                            }
                        }
                        else
                        {
                            await UpdateStep(_core.AccountInfo.Steps, "申请调试证书", false, "CSR文件不存在");
                            return new { success = false, error = "CSR 文件不存在，请确保 tools/harmony/store/xiaobai.csr 文件存在，或先生成密钥对" };
                        }
                    }
                    
                    string csrContent = await File.ReadAllTextAsync(csrPath);
                    
                    // 创建新证书
                    var createCertResponse = await _core.Eco.CreateCert(certName, csrContent, 1);
                    
                    if (createCertResponse?.Data == null || string.IsNullOrEmpty(createCertResponse.Data.Id))
                    {
                        await UpdateStep(_core.AccountInfo.Steps, "申请调试证书", false, "申请失败");
                        return new { success = false, error = "创建证书失败" };
                    }
                    
                    certId = createCertResponse.Data.Id;
                    Console.WriteLine($"[证书申请] 新证书已创建: {certId}");
                }
                
                // 下载证书文件
                if (string.IsNullOrEmpty(certPath) || !File.Exists(certPath))
                {
                    try
                    {
                        // 获取证书信息（包含下载链接）
                        var certListResponse2 = await _core.Eco.GetCertList();
                        var cert = certListResponse2?.CertList?.FirstOrDefault(c => c.Id == certId);
                        
                        if (cert != null && !string.IsNullOrEmpty(cert.CertObjectId))
                        {
                            Console.WriteLine($"[证书申请] 正在下载证书文件...");
                            
                            // 获取下载链接
                            var downloadUrl = await _core.Eco.DownloadObj(cert.CertObjectId);
                            
                            if (!string.IsNullOrEmpty(downloadUrl))
                            {
                                // 下载证书文件
                                certPath = Path.Combine(_core.Dh.ConfigDir, "debug_cert.cer");
                                await DownloadFile(downloadUrl, certPath);
                                Console.WriteLine($"[证书申请] 证书文件已下载: {certPath}");
                            }
                            else
                            {
                                Console.WriteLine($"[证书申请] 警告：无法获取证书下载链接");
                                certPath = Path.Combine(_core.Dh.ConfigDir, "debug_cert.cer");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[证书申请] 警告：无法获取证书下载信息");
                            certPath = Path.Combine(_core.Dh.ConfigDir, "debug_cert.cer");
                        }
                    }
                    catch (Exception downloadEx)
                    {
                        Console.WriteLine($"[证书申请] 下载证书失败: {downloadEx.Message}");
                        // 不阻止流程，继续使用配置的路径
                        certPath = Path.Combine(_core.Dh.ConfigDir, "debug_cert.cer");
                    }
                }
                
                // 保存证书配置
                _ecoConfig.DebugCert = new Models.Harmony.CertInfo { Id = certId, Name = certName, Path = certPath };
                }
                
                await UpdateStep(_core.AccountInfo.Steps, "申请调试证书", true, "已申请");
                
                // 2. 检查设备注册
                await UpdateStep(_core.AccountInfo.Steps, "注册调试设备", false, "正在检查...");
                
                var deviceListResponse = await _core.Eco.DeviceList();
                var devices = deviceListResponse?.Data?.List ?? new List<DeviceInfo>();
                
                if (devices.Count == 0)
                {
                    await UpdateStep(_core.AccountInfo.Steps, "注册调试设备", false, "无已注册设备");
                    // 不阻止流程，继续申请 Profile
                }
                else
                {
                    await UpdateStep(_core.AccountInfo.Steps, "注册调试设备", true, $"已注册 {devices.Count} 台设备");
                }
                
                // 3. 申请调试 Profile
                await UpdateStep(_core.AccountInfo.Steps, "申请调试Profile", false, "正在申请...");
                
                string profilePath = string.Empty;
                
                if (!string.IsNullOrEmpty(_ecoConfig.DebugProfile?.Path) && File.Exists(_ecoConfig.DebugProfile.Path))
                {
                    profilePath = _ecoConfig.DebugProfile.Path;
                    Console.WriteLine($"[证书申请] 使用已有 Profile: {profilePath}");
                }
                else
                {
                    // 获取包名（从 HAP 信息或使用默认值）
                    string packageName = commonInfo.PackageName ?? "com.example.app";
                    
                    // 获取设备 ID 列表
                    var deviceIds = devices.Select(d => d.DeviceId).ToList();
                    
                    if (deviceIds.Count == 0)
                    {
                        // 如果没有设备，不允许创建 Profile
                        Console.WriteLine("[证书申请] 错误：没有已注册的设备，无法创建 Profile");
                        await UpdateStep(_core.AccountInfo.Steps, "申请调试Profile", false, "需要先注册设备");
                        return new { success = false, error = "需要先注册调试设备才能创建 Profile" };
                    }
                    
                    // 从 HAP 文件解析权限列表（如果有）
                    var moduleJson = await ParseHapModuleJson(commonInfo.HapPath);
                    
                    // 创建 Profile
                    string profileName = $"debug_profile_{DateTime.Now:yyyyMMddHHmmss}";
                    
                    try
                    {
                        var createProfileResponse = await _core.Eco.CreateProfile(
                            profileName, certId, packageName, deviceIds, moduleJson);
                        
                        if (createProfileResponse?.Data != null && !string.IsNullOrEmpty(createProfileResponse.Data.ProvisionObjectId))
                        {
                            // 下载 Profile 文件
                            try
                            {
                                Console.WriteLine($"[证书申请] 正在下载 Profile 文件...");
                                
                                // 获取下载链接
                                var downloadUrl = await _core.Eco.DownloadObj(createProfileResponse.Data.ProvisionObjectId);
                                
                                if (!string.IsNullOrEmpty(downloadUrl))
                                {
                                    profilePath = Path.Combine(_core.Dh.ConfigDir, "debug_profile.p7b");
                                    await DownloadFile(downloadUrl, profilePath);
                                    Console.WriteLine($"[证书申请] Profile 文件已下载: {profilePath}");
                                }
                                else
                                {
                                    Console.WriteLine($"[证书申请] 警告：无法获取 Profile 下载链接");
                                    profilePath = Path.Combine(_core.Dh.ConfigDir, "debug_profile.p7b");
                                }
                            }
                            catch (Exception downloadEx)
                            {
                                Console.WriteLine($"[证书申请] 下载 Profile 失败: {downloadEx.Message}");
                                profilePath = Path.Combine(_core.Dh.ConfigDir, "debug_profile.p7b");
                            }
                            
                            _ecoConfig.DebugProfile = new ProfileInfo { Name = profileName, Path = profilePath };
                            Console.WriteLine($"[证书申请] Profile 已创建");
                        }
                        else
                        {
                            Console.WriteLine($"[证书申请] Profile 创建失败：响应数据为空");
                            await UpdateStep(_core.AccountInfo.Steps, "申请调试Profile", false, "创建失败");
                            return new { success = false, error = "创建 Profile 失败：响应数据为空" };
                        }
                    }
                    catch (Exception profileEx)
                    {
                        Console.WriteLine($"[证书申请] Profile 创建失败: {profileEx.Message}");
                        await UpdateStep(_core.AccountInfo.Steps, "申请调试Profile", false, $"申请失败: {profileEx.Message}");
                        return new { success = false, error = $"创建 Profile 失败: {profileEx.Message}" };
                    }
                }
                
                await UpdateStep(_core.AccountInfo.Steps, "申请调试Profile", true, "已申请");
                
                // 保存配置
                _core.Dh.WriteObjToFile("eco_config.json", _ecoConfig);
                
                return new {
                    success = true,
                    message = "证书和 Profile 申请完成",
                    certId = certId,
                    accountInfo = _core.AccountInfo
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[证书申请] 失败: {ex.Message}");
                await UpdateStep(_core.AccountInfo.Steps, "申请调试证书", false, $"失败: {ex.Message}");
                return new { success = false, error = ex.Message };
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
        
        /// <summary>
        /// 下载文件到本地
        /// </summary>
        private async Task DownloadFile(string url, string savePath)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
                
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsByteArrayAsync();
                
                // 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                await File.WriteAllBytesAsync(savePath, content);
                Console.WriteLine($"[文件下载] 文件已保存: {savePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[文件下载] 下载失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 从 HAP 文件解析 ModuleJson（权限列表等）
        /// </summary>
        private async Task<ModuleJson> ParseHapModuleJson(string? hapPath)
        {
            var moduleJson = new ModuleJson();
            
            if (string.IsNullOrEmpty(hapPath) || !File.Exists(hapPath))
            {
                Console.WriteLine($"[HAP解析] HAP文件不存在，使用空权限列表");
                return moduleJson;
            }
            
            try
            {
                // HAP 文件是 ZIP 格式，需要解压读取 module.json
                // 这里简化处理，实际应该使用 ZipArchive 解析
                // TODO: 完整实现需要解压 HAP 文件并读取 module.json
                Console.WriteLine($"[HAP解析] HAP文件解析功能待完善，使用默认权限列表");
                
                // 临时方案：返回常用的权限列表
                moduleJson.Module.RequestPermissions = new List<PermissionInfo>
                {
                    new PermissionInfo { Name = "ohos.permission.READ_AUDIO" },
                    new PermissionInfo { Name = "ohos.permission.WRITE_AUDIO" },
                    new PermissionInfo { Name = "ohos.permission.READ_IMAGEVIDEO" },
                    new PermissionInfo { Name = "ohos.permission.WRITE_IMAGEVIDEO" }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HAP解析] 解析失败: {ex.Message}");
            }
            
            await Task.CompletedTask;
            return moduleJson;
        }
    }
}
