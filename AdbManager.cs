using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyOSToolbox
{
    public class AdbManager
    {
        private readonly string adbPath;
        private DateTime _lastAuthAttempt = DateTime.MinValue;

        public AdbManager()
        {
            // Use system ADB from PATH
            adbPath = "adb";
        }

        /// <summary>
        /// 执行ADB命令，带重试机制
        /// </summary>
        private async Task<(bool success, string output)> ExecuteAdbAsync(string arguments, int maxRetries = 1)
        {
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    using var process = Process.Start(processInfo);
                    if (process == null)
                    {
                        return (false, "无法启动ADB进程");
                    }

                    // Set a timeout for the command
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    
                    // Wait for exit with a timeout (e.g., 10 seconds for most commands, longer for install/uninstall)
                    bool exited = await Task.Run(() => process.WaitForExit(30000)); 
                    
                    if (!exited)
                    {
                        process.Kill();
                        return (false, "命令执行超时");
                    }

                    string output = await outputTask;
                    string error = await errorTask;

                    // Some ADB commands output to stderr even on success (like install/push progress)
                    // We only consider it a failure if ExitCode is non-zero OR if it's a critical error string
                    if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                    {
                        // If it's a daemon start message, it's not really an error
                        if (error.Contains("daemon started successfully"))
                        {
                            return (true, output + "\n" + error);
                        }
                        
                        // If connection failed, retry logic might kick in
                        if (i < maxRetries && (error.Contains("device not found") || error.Contains("offline")))
                        {
                             await Task.Delay(1000);
                             continue;
                        }
                        
                        return (false, error);
                    }

                    return (true, output);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return (false, "未找到ADB命令，请确保ADB已添加到环境变量PATH中");
                }
                catch (Exception ex)
                {
                    if (i < maxRetries) 
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    return (false, $"执行失败: {ex.Message}");
                }
            }
            return (false, "执行失败，达到最大重试次数");
        }

        private bool _isAuthorizing = false;

        /// <summary>
        /// 检查设备连接状态，包含授权验证和自动修复
        /// </summary>
        public async Task<object> CheckDeviceAsync()
        {
            // First, try a simple devices command
            var (success, output) = await ExecuteAdbAsync("devices");
            
            if (!success)
            {
                // If basic command fails, try to restart server
                await ExecuteAdbAsync("kill-server");
                await ExecuteAdbAsync("start-server");
                (success, output) = await ExecuteAdbAsync("devices");
            }

            if (!success)
            {
                return new { connected = false, deviceCount = 0, message = $"ADB错误: {output}" };
            }

            // Initial parse
            var status = ParseDevicesOutput(output);
            
            // If we see unauthorized devices and aren't already in an auth loop
            if (status.unauthorizedCount > 0 && !_isAuthorizing)
            {
                _isAuthorizing = true;
                try
                {
                    // Always try lightweight trigger first
                    _ = ExecuteAdbAsync("shell date");

                    // Only perform heavy recovery (restart server) if it's been a while (30s)
                    // This prevents the 5s polling loop from constantly killing the server
                    if ((DateTime.Now - _lastAuthAttempt).TotalSeconds > 30)
                    {
                        _lastAuthAttempt = DateTime.Now;

                        // 1. Try to reconnect specifically
                        await ExecuteAdbAsync("reconnect");
                        
                        // 2. Restart server (The Nuclear Option)
                        await ExecuteAdbAsync("kill-server");
                        await ExecuteAdbAsync("start-server");
                        
                        // 3. Trigger again after restart
                        await Task.Delay(1000);
                        _ = ExecuteAdbAsync("shell date");

                        // Wait a few seconds for user to click "Allow" on phone
                        await Task.Delay(3000);

                        // Check again
                        (success, output) = await ExecuteAdbAsync("devices");
                        if (success)
                        {
                            status = ParseDevicesOutput(output);
                        }
                    }
                }
                finally
                {
                    _isAuthorizing = false;
                }
            }
            else if (status.offlineCount > 0)
            {
                 // Try to recover offline devices
                _ = ExecuteAdbAsync("reconnect offline");
            }

            string message;
            if (status.deviceCount > 0)
            {
                message = $"已连接 {status.deviceCount} 个设备";
            }
            else if (status.unauthorizedCount > 0)
            {
                message = "设备未授权，请在手机弹窗中点击'允许USB调试'";
            }
            else if (status.offlineCount > 0)
            {
                message = "设备离线，请检查USB连接或重启USB调试";
            }
            else
            {
                message = "未检测到设备，请连接USB并开启调试模式";
            }

            return new
            {
                connected = status.deviceCount > 0,
                deviceCount = status.deviceCount,
                unauthorizedCount = status.unauthorizedCount,
                offlineCount = status.offlineCount,
                deviceList = status.deviceList,
                message = message
            };
        }

        private (int deviceCount, int unauthorizedCount, int offlineCount, System.Collections.Generic.List<string> deviceList) ParseDevicesOutput(string output)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int deviceCount = 0;
            int unauthorizedCount = 0;
            int offlineCount = 0;
            var deviceList = new System.Collections.Generic.List<string>();

            foreach (var line in lines)
            {
                if (line.Contains("List of devices")) continue;
                
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string serial = parts[0];
                    string state = parts[1];
                    
                    if (state == "device")
                    {
                        deviceCount++;
                        deviceList.Add(line.Trim());
                    }
                    else if (state == "unauthorized")
                    {
                        unauthorizedCount++;
                    }
                    else if (state == "offline")
                    {
                        offlineCount++;
                    }
                }
            }
            return (deviceCount, unauthorizedCount, offlineCount, deviceList);
        }

        /// <summary>
        /// 卸载应用 (支持单个或逗号分隔的多个包名)
        /// </summary>
        public async Task<object> UninstallAppAsync(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return new { success = false, message = "包名不能为空" };
            }

            var packages = packageName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var results = new System.Collections.Generic.List<string>();
            bool allSuccess = true;

            foreach (var pkg in packages)
            {
                var p = pkg.Trim();
                var (success, output) = await ExecuteAdbAsync($"shell pm uninstall --user 0 {p}");
                
                // "Success" or "not installed" (failure usually implies not installed which is fine for cleanup)
                // Strict check: output.Contains("Success")
                bool isSuccess = output.Contains("Success");
                results.Add(isSuccess ? $"已卸载: {p}" : $"卸载失败({p}): {output}");
                
                // If it failed because it's not installed, we can consider it a "soft success" or just log it.
                // For now, we'll track strict success but the message shows details.
            }

            return new
            {
                success = true, // Always return true to allow the frontend to show the detailed message
                message = string.Join("\n", results),
                output = string.Join("\n", results)
            };
        }

        /// <summary>
        /// 安装应用 (支持单个或逗号分隔的多个包名)
        /// </summary>
        public async Task<object> InstallAppAsync(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return new { success = false, message = "包名不能为空" };
            }

            var packages = packageName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var results = new System.Collections.Generic.List<string>();

            foreach (var pkg in packages)
            {
                var p = pkg.Trim();
                var (success, output) = await ExecuteAdbAsync($"shell pm install-existing --user 0 {p}");
                
                bool installed = output.Contains("Success") || output.Contains("installed");
                results.Add(installed ? $"已安装: {p}" : $"安装失败({p}): {output}");
            }

            return new
            {
                success = true,
                message = string.Join("\n", results),
                output = string.Join("\n", results)
            };
        }

        /// <summary>
        /// 列出应用包
        /// </summary>
        public async Task<object> ListPackagesAsync(string type)
        {
            string flag = type switch
            {
                "system" => "-s",
                "user" => "-3",
                _ => ""
            };

            var (success, output) = await ExecuteAdbAsync($"shell pm list packages {flag}");

            var packages = new System.Collections.Generic.List<string>();
            if (success)
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("package:"))
                    {
                        packages.Add(line.Replace("package:", "").Trim());
                    }
                }
            }

            return new { success = success, packages = packages, count = packages.Count };
        }

        /// <summary>
        /// 获取内存信息
        /// </summary>
        public async Task<object> GetMemoryInfoAsync()
        {
            var (success, output) = await ExecuteAdbAsync("shell top -n 1 -s 6");
            return new { success = success, output = output };
        }

        /// <summary>
        /// 设置动画速度
        /// </summary>
        public async Task<object> SetAnimationAsync(AnimationConfig config)
        {
            var commands = new[]
            {
                $"shell settings put global window_animation_scale {config.Window}",
                $"shell settings put global transition_animation_scale {config.Transition}",
                $"shell settings put global animator_duration_scale {config.Animator}"
            };

            bool allSuccess = true;
            var results = new System.Collections.Generic.List<string>();

            foreach (var cmd in commands)
            {
                var (success, output) = await ExecuteAdbAsync(cmd);
                allSuccess &= success;
                results.Add(output);
            }

            return new
            {
                success = allSuccess,
                message = allSuccess ? "动画速度已设置" : "设置失败",
                details = results
            };
        }

        /// <summary>
        /// 执行自定义ADB命令
        /// </summary>
        public async Task<object> ExecAdbCommandAsync(string command)
        {
            var (success, output) = await ExecuteAdbAsync(command);
            return new { success = success, output = output };
        }

        /// <summary>
        /// 禁用应用
        /// </summary>
        public async Task<object> DisableAppAsync(string packageName)
        {
            var (success, output) = await ExecuteAdbAsync($"shell pm disable-user {packageName}");
            return new
            {
                success = output.Contains("disabled"),
                message = output.Contains("disabled") ? $"已禁用: {packageName}" : $"禁用失败: {output}",
                output = output
            };
        }

        /// <summary>
        /// 启用应用
        /// </summary>
        public async Task<object> EnableAppAsync(string packageName)
        {
            var (success, output) = await ExecuteAdbAsync($"shell pm enable {packageName}");
            return new
            {
                success = output.Contains("enabled"),
                message = output.Contains("enabled") ? $"已启用: {packageName}" : $"启用失败: {output}",
                output = output
            };
        }

        /// <summary>
        /// 安装APK文件到设备
        /// </summary>
        public async Task<object> InstallApkFileAsync(string apkPath)
        {
            if (!File.Exists(apkPath))
            {
                return new { success = false, message = "APK文件不存在" };
            }

            var (success, output) = await ExecuteAdbAsync($"install -r \"{apkPath}\"");
            return new
            {
                success = success,
                message = success ? $"APK安装成功" : $"安装失败: {output}",
                apkPath = apkPath,
                output = output
            };
        }

        /// <summary>
        /// 批量安装文件夹中的所有APK
        /// </summary>
        public async Task<object> InstallApkFolderAsync(string folderPath, IProgress<string>? progress = null)
        {
            if (!Directory.Exists(folderPath))
            {
                return new { success = false, message = "文件夹不存在" };
            }

            var apkFiles = Directory.GetFiles(folderPath, "*.apk");
            if (apkFiles.Length == 0)
            {
                return new { success = false, message = "该文件夹中没有找到APK文件" };
            }

            int successCount = 0;
            int failCount = 0;
            var results = new System.Collections.Generic.List<object>();

            foreach (var apkPath in apkFiles)
            {
                string fileName = Path.GetFileName(apkPath);
                progress?.Report($"正在安装: {fileName}");

                var result = await InstallApkFileAsync(apkPath);
                dynamic dynamicResult = result;
                
                if (dynamicResult.success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
                
                results.Add(result);
            }

            return new
            {
                success = true,
                message = $"批量安装完成: 成功 {successCount} 个，失败 {failCount} 个",
                results = results,
                total = apkFiles.Length
            };
        }

        /// <summary>
        /// 获取系统版本
        /// </summary>
        public async Task<object> GetSystemVersionAsync()
        {
            var (success, output) = await ExecuteAdbAsync("shell getprop ro.build.version.release");
            return new { success = success, version = output.Trim(), output = output };
        }

        /// <summary>
        /// 重启到Recovery
        /// </summary>
        public async Task<object> RebootRecoveryAsync()
        {
            var (success, output) = await ExecuteAdbAsync("reboot recovery");
            return new { success = success, message = "正在重启到Recovery模式...", output = output };
        }

        /// <summary>
        /// 批量卸载应用（主菜单功能）
        /// </summary>
        public async Task<object> BatchUninstallAsync(int code)
        {
            var packages = GetMainMenuPackages(code, true);
            var results = new System.Collections.Generic.List<object>();

            foreach (var pkg in packages)
            {
                var result = await UninstallAppAsync(pkg);
                results.Add(new { package = pkg, result = result });
            }

            return new { success = true, results = results, message = $"已执行 {results.Count} 个卸载操作" };
        }

        /// <summary>
        /// 批量安装应用（主菜单功能）
        /// </summary>
        public async Task<object> BatchInstallAsync(int code)
        {
            var packages = GetMainMenuPackages(code, false);
            var results = new System.Collections.Generic.List<object>();

            foreach (var pkg in packages)
            {
                var result = await InstallAppAsync(pkg);
                results.Add(new { package = pkg, result = result });
            }

            return new { success = true, results = results, message = $"已执行 {results.Count} 个安装操作" };
        }

        /// <summary>
        /// 获取主菜单对应的包名列表（模拟HarmonyOS.bat的逻辑）
        /// </summary>
        private string[] GetMainMenuPackages(int code, bool isUninstall)
        {
            return code switch
            {
                1 => new[] { "com.huawei.search", "com.huawei.searchservice" }, // 智慧搜索
                2 => new[] { "com.huawei.vassistant" }, // 智慧语音
                3 => new[] { "com.huawei.hitouch", "com.huawei.hiaction", "com.huawei.contentsensor" }, // 智慧识屏
                4 => new[] { "com.huawei.ohos.suggestion" }, // 智慧建议
                5 => new[] { "com.huawei.intelligent" }, // 智慧助手-今天
                6 => new[] { "com.android.mediacenter", "com.huawei.music" }, // 华为音乐
                7 => new[] { "com.huawei.ohos.famanager" }, // 服务中心
                8 => new[] { "com.huawei.wallet" }, // 华为钱包
                9 => new[] { "com.huawei.hiskytone", "com.huawei.skytone" }, // 天际通
                11 => new[] { "com.google.android.gms", "com.google.android.gsf" }, // 谷歌服务（禁用）
                12 => new[] { "com.huawei.fastapp" }, // 快应用中心
                13 => new[] { "com.huawei.health", "com.huawei.ohos.health" }, // 运动健康
                14 => new[] { "com.huawei.powergenie", "com.huawei.iaware", "com.huawei.android.hwaps" }, // 巅峰性能模式
                20 => new[] { "com.huawei.android.hwouc" }, // 系统禁更
                _ => Array.Empty<string>()
            };
        }

        /// <summary>
        /// 执行冗余服务清理（代码10的多步骤交互式功能）
        /// </summary>
        public async Task<object> CleanRedundantServicesAsync(bool[] selections)
        {
            var results = new System.Collections.Generic.List<object>();

            // 根据用户选择执行不同的清理
            if (selections.Length > 0 && selections[0]) // 基础冗余
            {
                var packages = new[]
                {
                    "com.android.stk", "com.huawei.hifolder", "com.huawei.bd",
                    "com.huawei.hiview", "com.huawei.hiviewtunnel", "com.huawei.android.UEInfoCheck",
                    "com.android.cellbroadcastreceiver", "com.huawei.spaceservice", "com.huawei.tips",
                    "com.google.android.backuptransport", "com.huawei.pengine"
                };
                foreach (var pkg in packages)
                {
                    var result = await UninstallAppAsync(pkg);
                    results.Add(result);
                }
            }

            if (selections.Length > 1 && selections[1]) // 无障碍服务
            {
                results.Add(await UninstallAppAsync("com.google.android.marvin.talkback"));
            }

            if (selections.Length > 2 && selections[2]) // AR引擎
            {
                results.Add(await UninstallAppAsync("com.huawei.vrservice"));
                results.Add(await UninstallAppAsync("com.huawei.arengine.service"));
            }

            if (selections.Length > 3 && selections[3]) // 骨声纹
            {
                results.Add(await UninstallAppAsync("com.huawei.bonevoiceui"));
            }

            if (selections.Length > 4 && selections[4]) // RCS
            {
                results.Add(await UninstallAppAsync("com.huawei.rcsserviceapplication"));
            }

            if (selections.Length > 5 && selections[5]) // 取词服务
            {
                results.Add(await UninstallAppAsync("com.huawei.contentsensor"));
            }

            if (selections.Length > 6 && selections[6]) // 动态壁纸
            {
                var wallpapers = new[]
                {
                    "com.android.dreams.basic", "com.android.dreams.phototable",
                    "com.huawei.livewallpaper.paradise", "com.huawei.livewallpaper.mountaincloud"
                };
                foreach (var pkg in wallpapers)
                {
                    results.Add(await UninstallAppAsync(pkg));
                }
            }

            return new { success = true, results = results, message = "冗余服务清理完成" };
        }

        /// <summary>
        /// 禁用谷歌服务（代码11）
        /// </summary>
        public async Task<object> DisableGoogleServicesAsync()
        {
            var packages = new[]
            {
                "com.google.android.gms", "com.google.android.gsf", "com.android.vending",
                "com.google.android.onetimeinitializer", "com.google.android.partnersetup",
                "com.google.android.marvin.talkback", "com.android.ext.services",
                "com.google.android.backuptransport", "com.google.android.gsf.login",
                "com.google.android.printservice.recommendation", "com.google.android.feedback",
                "com.google.android.syncadapters.calendar", "com.google.android.syncadapters.contacts"
            };

            var results = new System.Collections.Generic.List<object>();
            foreach (var pkg in packages)
            {
                var result = await DisableAppAsync(pkg);
                results.Add(result);
            }

            return new { success = true, results = results, message = "谷歌服务已禁用" };
        }

        /// <summary>
        /// 启用谷歌服务
        /// </summary>
        public async Task<object> EnableGoogleServicesAsync()
        {
            var packages = new[]
            {
                "com.google.android.gms", "com.google.android.gsf", "com.android.vending",
                "com.google.android.onetimeinitializer", "com.google.android.partnersetup",
                "com.google.android.marvin.talkback", "com.android.ext.services",
                "com.google.android.backuptransport", "com.google.android.gsf.login",
                "com.google.android.printservice.recommendation", "com.google.android.feedback",
                "com.google.android.syncadapters.calendar", "com.google.android.syncadapters.contacts"
            };

            var results = new System.Collections.Generic.List<object>();
            foreach (var pkg in packages)
            {
                var result = await EnableAppAsync(pkg);
                results.Add(result);
            }

            return new { success = true, results = results, message = "谷歌服务已启用" };
        }

        /// <summary>
        /// 设置亮屏时间（G2功能）
        /// </summary>
        public async Task<object> SetScreenTimeoutAsync(int milliseconds)
        {
            var (success, output) = await ExecuteAdbAsync($"shell settings put system screen_off_timeout {milliseconds}");
            return new
            {
                success = success,
                message = success ? $"亮屏时间已设置为 {milliseconds}ms ({milliseconds / 1000}秒)" : "设置失败",
                output = output
            };
        }

        /// <summary>
        /// 隐藏状态栏图标（G8功能）
        /// </summary>
        public async Task<object> HideStatusBarIconsAsync(string[] icons)
        {
            var iconMap = new System.Collections.Generic.Dictionary<string, string>
            {
                { "nfc", "nfc" },
                { "location", "location" },
                { "powersaving", "powersavingmode" },
                { "battery", "battery" },
                { "eyeprotect", "eyes_protect" },
                { "signal", "phone_signal" },
                { "hd", "volte_call" }
            };

            var iconList = new System.Collections.Generic.List<string>();
            foreach (var icon in icons)
            {
                if (iconMap.ContainsKey(icon.ToLower()))
                {
                    iconList.Add(iconMap[icon.ToLower()]);
                }
            }

            string iconString = iconList.Count > 0 ? string.Join(",", iconList) : "null";
            var (success, output) = await ExecuteAdbAsync($"shell settings put secure icon_blacklist {iconString}");

            return new
            {
                success = success,
                message = iconList.Count > 0 ? $"已隐藏 {iconList.Count} 个状态栏图标" : "已恢复默认状态栏",
                output = output
            };
        }

        /// <summary>
        /// 修改后台白名单（G14功能）
        /// </summary>
        public async Task<object> GetWhitelistAsync()
        {
            var (success, output) = await ExecuteAdbAsync("shell dumpsys deviceidle whitelist");
            return new { success = success, output = output, message = "已获取后台白名单" };
        }

        public async Task<object> AddToWhitelistAsync(string packageName)
        {
            var (success, output) = await ExecuteAdbAsync($"shell dumpsys deviceidle whitelist +{packageName}");
            return new
            {
                success = success,
                message = $"已将 {packageName} 添加到白名单",
                output = output
            };
        }

        public async Task<object> RemoveFromWhitelistAsync(string packageName)
        {
            var (success, output) = await ExecuteAdbAsync($"shell dumpsys deviceidle whitelist -{packageName}");
            return new
            {
                success = success,
                message = $"已将 {packageName} 从白名单移除",
                output = output
            };
        }

        /// <summary>
        /// 不杀后台模式（G13功能）
        /// </summary>
        public async Task<object> SetNoKillBackgroundAsync(bool enable)
        {
            if (enable)
            {
                return await UninstallAppAsync("com.huawei.iaware");
            }
            else
            {
                return await InstallAppAsync("com.huawei.iaware");
            }
        }

        /// <summary>
        /// 查看应用内存占用（J6-J10功能）
        /// </summary>
        public async Task<object> GetAppMemoryAsync(string packageName)
        {
            var (success, output) = await ExecuteAdbAsync($"shell dumpsys meminfo {packageName}");
            return new { success = success, output = output, package = packageName };
        }

        /// <summary>
        /// 瘦身模式 - 清理QQ/TIM/微信缓存
        /// </summary>
        public async Task<object> CleanCacheAsync()
        {
            var commands = new[]
            {
                // 微信
                "shell rm -r /sdcard/Android/data/com.tencent.mm/MicroMsg/*/video",
                "shell rm -r /sdcard/Android/data/com.tencent.mm/MicroMsg/CheckResUpdate",
                "shell rm -r /sdcard/Android/data/com.tencent.mm/cache",
                
                // TIM
                "shell rm -r /sdcard/android/data/com.tencent.tim/tencent/tim/shortvideo",
                "shell rm -r /sdcard/Android/data/com.tencent.tim/cache",
                "shell rm -r /sdcard/Android/data/com.tencent.tim/Tencent/TIMfile_recv",
                "shell rm -r /sdcard/Android/data/com.tencent.tim/Tencent/Tim/chatpic",
                
                // QQ
                "shell rm -r /sdcard/android/data/com.tencent.mobileqq/tencent/Mobileqq",
                "shell rm -r /sdcard/Android/data/com.tencent.mobileqq/cache",
                "shell rm -r /sdcard/Android/data/com.tencent.mobileqq/Tencent/QQfile_recv",
                "shell rm -r /sdcard/Android/data/com.tencent.mobileqq/Tencent/MobileQQ/chatpic",
                
                // 图库缓存
                "shell rm -r /sdcard/Android/data/com.android.gallery3d/files/thumbdb",
                
                // 腾讯外部文件
                "shell rm -r /sdcard/tencent/msflogs",
                "shell rm -r /sdcard/tencent/Tim",
                
                // 系统日志
                "shell rm /bugreports/*.*",
                "shell rm /sdcard/*.log"
            };

            var results = new System.Collections.Generic.List<string>();
            foreach (var cmd in commands)
            {
                var (success, output) = await ExecuteAdbAsync(cmd);
                results.Add($"{cmd}: {(success ? "完成" : "跳过")}");
            }

            return new
            {
                success = true,
                message = "缓存清理完成",
                details = results
            };
        }

        /// <summary>
        /// 还原所有操作（代码000）
        /// </summary>
        public async Task<object> RestoreAllAsync()
        {
            var packages = new[]
            {
                // 所有可能被卸载的包
                "com.huawei.search", "com.huawei.vassistant", "com.huawei.hitouch",
                "com.huawei.intelligent", "com.huawei.music", "com.huawei.ohos.famanager",
                "com.huawei.wallet", "com.huawei.fastapp", "com.huawei.health",
                "com.huawei.browser", "com.huawei.hicloud", "com.huawei.meetime",
                "com.huawei.powergenie", "com.huawei.iaware", "com.huawei.android.hwaps"
            };

            var results = new System.Collections.Generic.List<object>();
            foreach (var pkg in packages)
            {
                var result = await InstallAppAsync(pkg);
                results.Add(result);
            }

            // 恢复动画速度
            await SetAnimationAsync(new AnimationConfig { Window = 1, Transition = 1, Animator = 1 });

            // 恢复状态栏
            await HideStatusBarIconsAsync(Array.Empty<string>());

            // 启用谷歌服务
            await EnableGoogleServicesAsync();

            return new
            {
                success = true,
                message = "已还原所有操作，建议重启手机",
                restoredCount = results.Count
            };
        }
    }
}

