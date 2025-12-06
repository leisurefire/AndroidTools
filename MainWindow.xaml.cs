using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using HarmonyOSToolbox.Services.Harmony;
using HarmonyOSToolbox.Models.Harmony;

namespace HarmonyOSToolbox
{
    public partial class MainWindow : Window
    {
        private AdbManager? adbManager;
        private HarmonyCoreService? harmonyService;

        // Windows 11 Mica P/Invoke
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38; // Windows 11 22H2+
        private const int DWMSBT_MAINWINDOW = 2; // Mica

        public MainWindow()
        {
            InitializeComponent();
            
            // Apply Windows 11 Mica effect and set work area constraints
            Loaded += Window_Loaded;
            
            // Monitor window state changes to update UI and fix maximize behavior
            StateChanged += Window_StateChanged;
            
            // Safe async initialization (fire-and-forget with protection)
            InitializeWebViewAsync();
        }
        
        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            ApplyMicaEffect();
            ApplyRoundedCorners();
            
            // Listen for system theme changes
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_SETTINGCHANGE = 0x001A;
            if (msg == WM_SETTINGCHANGE)
            {
                 // Schedule UI update on dispatcher to allow registry to propagate
                 Dispatcher.InvokeAsync(async () => 
                 {
                     await Task.Delay(200);
                     ApplyMicaEffect();
                     UpdateFrontendTheme();
                 });
            }
            return IntPtr.Zero;
        }

        private void UpdateFrontendTheme()
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                bool isDark = IsSystemDarkTheme();
                string themeMode = isDark ? "dark" : "light";
                
                // We set the attribute on documentElement to force the CSS variables to update
                string themeScript = $@"
                    document.documentElement.setAttribute('data-theme', '{themeMode}');
                    console.log('[Theme] System changed, updated to: {themeMode}');
                ";
                webView.CoreWebView2.ExecuteScriptAsync(themeScript);
                
                // Update accent color too
                string accentColor = GetSystemAccentColor();
                SendAccentColorToFrontend(accentColor);
            }
        }

        private bool IsSystemDarkTheme()
        {
            try
            {
                // 1. Check AppsUseLightTheme (for apps)
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    object? val = key.GetValue("AppsUseLightTheme");
                    if (val is int i)
                    {
                        return i == 0;
                    }
                    
                    // 2. Fallback to SystemUsesLightTheme (for system taskbar/start menu)
                    object? valSys = key.GetValue("SystemUsesLightTheme");
                    if (valSys is int j)
                    {
                        return j == 0;
                    }
                }
            }
            catch { }
            
            // Default to light if detection fails, or check high contrast if needed
            return false;
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            // Send state to frontend
            SendWindowState();
        }
        
        private void SendWindowState()
        {
            try
            {
                if (webView != null && webView.CoreWebView2 != null)
                {
                    string state = WindowState == WindowState.Maximized ? "maximized" : "normal";
                    SendResponse("windowState", new { state = state });
                }
            }
            catch
            {
                // Ignore errors if WebView2 is not ready
            }
        }

        private void ApplyMicaEffect()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Enable Mica effect (Windows 11 22H2+)
                int backdropType = DWMSBT_MAINWINDOW;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                
                // Detect system theme and apply to window frame
                bool isDark = IsSystemDarkTheme();
                int useDarkMode = isDark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
                
                // Sync Top Bar Extension color with CSS
                // Light: rgba(255, 255, 255, 0.4) -> #66FFFFFF (matches --sidebar-bg light)
                // Dark: rgba(32, 32, 32, 0.6) -> #99202020 (matches --sidebar-bg dark)
                if (TopBarExtension != null)
                {
                    // Convert rgba(32, 32, 32, 0.6) to ARGB: 0.6 * 255 = 153 (0x99)
                    // Color: #99202020
                     TopBarExtension.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#99202020" : "#66FFFFFF"));
                }
                
                // Force theme on frontend
                UpdateFrontendTheme();
            }
            catch
            {
                // Windows 10 or older doesn't support Mica
            }
        }
        
        private string GetSystemAccentColor()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                if (key != null)
                {
                    object? accentColorObj = key.GetValue("AccentColor");
                    if (accentColorObj is int accentColorDword)
                    {
                        // Windows stores color as 0xAABBGGRR, convert to #RRGGBB
                        byte r = (byte)(accentColorDword & 0xFF);
                        byte g = (byte)((accentColorDword >> 8) & 0xFF);
                        byte b = (byte)((accentColorDword >> 16) & 0xFF);
                        return $"#{r:X2}{g:X2}{b:X2}";
                    }
                }
            }
            catch { }
            return "#0067c0"; // Fallback to default blue
        }
        
        private void SendAccentColorToFrontend(string color)
        {
            try
            {
                if (webView != null && webView.CoreWebView2 != null)
                {
                    string script = $@"
                        if (document.documentElement) {{
                            document.documentElement.style.setProperty('--primary-color', '{color}');
                            console.log('[Theme] Accent color set to: {color}');
                        }}
                    ";
                    webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch { }
        }

        private void ApplyRoundedCorners()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Windows 11 Rounded Corners
                const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
                const int DWMWCP_ROUND = 2; // Round

                int cornerPreference = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
            }
            catch
            {
                // Fallback for older systems
                System.Diagnostics.Debug.WriteLine("System doesn't support DWM rounded corners, using XAML Border");
            }
        }

        private async void InitializeWebViewAsync()
        {
            try
            {
                // Initialize services
                try
                {
                    // Harmony Service (Independent of ADB)
                    harmonyService = new HarmonyCoreService();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Harmony Service Init Failed: {ex.Message}");
                }

                try
                {
                    // ADB Manager (Might fail if tools are missing)
                    adbManager = new AdbManager();
                }
                catch (Exception ex)
                {
                    // Log but don't show intrusive messagebox every time, or show warning once
                    System.Diagnostics.Debug.WriteLine($"ADB Init Warning: {ex.Message}");
                }

                // Wait for WebView2 initialization
                await webView.EnsureCoreWebView2Async(null);

                // Set PreferredColorScheme to Auto (follows system)
                try {
                    webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
                } catch { /* Ignore if not supported */ }

                // Enable non-client region support (allows CSS drag)
                webView.CoreWebView2.Settings.IsNonClientRegionSupportEnabled = true;

                // Set WebView2 background color (match window background)
                webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                // Enable DevTools for debugging
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                
                // Disable cache for development (comment out in production)
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;

                // Set WebView2 message handling
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // Navigation completed handler
                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine("[WebView2] Page loaded");
                        // Inject initialization script
                        webView.CoreWebView2.ExecuteScriptAsync(@" 
                            console.log('[WebView2] Page loaded, initializing...');
                        ");
                        // Send initial window state
                        SendWindowState();
                        // Send Windows accent color
                        string accentColor = GetSystemAccentColor();
                        SendAccentColorToFrontend(accentColor);
                        // Apply initial theme
                        UpdateFrontendTheme();
                    }
                };

                // Load HTML page with cache busting
                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
                string url = new Uri(htmlPath).AbsoluteUri + "?v=" + DateTime.Now.Ticks;
                webView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application Initialization Failed: {ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // The frontend sends JSON.stringify(message), so the payload is a string.
                // WebMessageAsJson would return that string as a JSON string (double encoded).
                // TryGetWebMessageAsString gives us the raw string payload (the JSON object).
                string message = e.TryGetWebMessageAsString();
                
                // Fallback if it wasn't a string (though our JS always sends strings)
                if (string.IsNullOrEmpty(message))
                {
                    message = e.WebMessageAsJson;
                }

                // Debug log (fire and forget to avoid blocking)
                _ = webView.CoreWebView2.ExecuteScriptAsync($"console.log('[C#] Raw: {message.Replace("'", "\\'").Replace("\n", "").Replace("\r", "")}')");

                var request = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRequest>(message);

                if (request == null || string.IsNullOrEmpty(request.RequestId) || string.IsNullOrEmpty(request.Action))
                {
                    _ = webView.CoreWebView2.ExecuteScriptAsync("console.error('[C#] Invalid request')");
                    SendResponse("", new { success = false, error = "Invalid request" });
                    return;
                }
                
                 _ = webView.CoreWebView2.ExecuteScriptAsync($"console.log('[C#] Action: {request.Action}')");

                // Identify actions that don't require ADB
                bool isSafeAction = request.Action == "windowControl" || 
                                    request.Action == "loadPage" || 
                                    request.Action.StartsWith("harmony_") ||
                                    request.Action == "getAdbVersion";

                // Check ADB Manager initialization for ADB-dependent actions
                if (adbManager == null && !isSafeAction)
                {
                    var errorResult = new { success = false, error = "ADB tool not initialized. Please check environment." };
                    SendResponse(request.RequestId, errorResult);
                    return;
                }

                object? result = request.Action switch
                {
                    // Basic Features
                    "checkDevice" => adbManager != null ? await adbManager.CheckDeviceAsync() : new { connected = false, deviceCount = 0, message = "ADB not initialized" },
                    "uninstallApp" => await adbManager!.UninstallAppAsync(request.Data?.ToString() ?? ""),
                    "installApp" => await adbManager!.InstallAppAsync(request.Data?.ToString() ?? ""),
                    "installApkFile" => await SelectAndInstallApkAsync(),
                    "installApkFolder" => await SelectAndInstallFolderAsync(),
                    "listPackages" => await adbManager!.ListPackagesAsync(request.Data?.ToString() ?? "all"),
                    "getMemory" => await adbManager!.GetMemoryInfoAsync(),
                    "getSystemVersion" => await adbManager!.GetSystemVersionAsync(),
                    "execAdb" => await adbManager!.ExecAdbCommandAsync(request.Data?.ToString() ?? ""),
                    
                    // Animation & Display
                    "setAnimation" => await adbManager!.SetAnimationAsync(
                        Newtonsoft.Json.JsonConvert.DeserializeObject<AnimationConfig>(request.Data?.ToString() ?? "{}") ?? new AnimationConfig()),
                    "setScreenTimeout" => await adbManager!.SetScreenTimeoutAsync(
                        int.TryParse(request.Data?.ToString(), out int timeout) ? timeout : 30000),
                    "hideStatusBarIcons" => await adbManager!.HideStatusBarIconsAsync(
                        Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(request.Data?.ToString() ?? "[]") ?? Array.Empty<string>()),
                    
                    // Batch Operations
                    "batchUninstall" => await adbManager!.BatchUninstallAsync(
                        int.TryParse(request.Data?.ToString(), out int code1) ? code1 : 0),
                    "batchInstall" => await adbManager!.BatchInstallAsync(
                        int.TryParse(request.Data?.ToString(), out int code2) ? code2 : 0),
                    "cleanRedundantServices" => await adbManager!.CleanRedundantServicesAsync(
                        Newtonsoft.Json.JsonConvert.DeserializeObject<bool[]>(request.Data?.ToString() ?? "[]") ?? Array.Empty<bool>()),
                    "restoreAll" => await adbManager!.RestoreAllAsync(),
                    
                    // Google Services
                    "disableGoogleServices" => await adbManager!.DisableGoogleServicesAsync(),
                    "enableGoogleServices" => await adbManager!.EnableGoogleServicesAsync(),
                    
                    // App Management
                    "disableApp" => await adbManager!.DisableAppAsync(request.Data?.ToString() ?? ""),
                    "enableApp" => await adbManager!.EnableAppAsync(request.Data?.ToString() ?? ""),
                    
                    // Whitelist
                    "getWhitelist" => await adbManager!.GetWhitelistAsync(),
                    "addToWhitelist" => await adbManager!.AddToWhitelistAsync(request.Data?.ToString() ?? ""),
                    "removeFromWhitelist" => await adbManager!.RemoveFromWhitelistAsync(request.Data?.ToString() ?? ""),
                    
                    // Advanced
                    "setNoKillBackground" => await adbManager!.SetNoKillBackgroundAsync(
                        bool.TryParse(request.Data?.ToString(), out bool enable) && enable),
                    "getAppMemory" => await adbManager!.GetAppMemoryAsync(request.Data?.ToString() ?? ""),
                    "cleanCache" => await adbManager!.CleanCacheAsync(),
                    "rebootRecovery" => await adbManager!.RebootRecoveryAsync(),
                    
                    // ADB Info
                    "getAdbVersion" => new { version = "System ADB" },
                    
                    // Window Control
                    "windowControl" => HandleWindowControl(request.Data?.ToString() ?? ""),
                    
                    // Page Loading (fix CORS issue)
                    "loadPage" => LoadPageContent(request.Data?.ToString() ?? ""),

                    // Harmony OS
                    _ when request.Action != null && request.Action.StartsWith("harmony_") => await HandleHarmonyRequest(request),

                    _ => new { success = false, error = "Unknown action" }
                };

                SendResponse(request.RequestId, result);
            }
            catch (Exception ex)
            {
                await webView.CoreWebView2.ExecuteScriptAsync($"console.error('[C#] Exception: {ex.Message}')");
                SendResponse("", new { success = false, error = ex.Message });
            }
        }

        private object HandleWindowControl(string command)
        {
            try
            {
                // Use InvokeAsync to avoid blocking the WebView2 event handler
                Dispatcher.InvokeAsync(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[WindowControl] Execute: {command}");
                    switch (command)
                    {
                        case "minimize":
                            WindowState = WindowState.Minimized;
                            break;
                        case "toggleMaximize":
                            WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                            break;
                        case "close":
                            Close();
                            break;
                    }
                });
                
                return new { success = true, command = command };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
        
        private object LoadPageContent(string pageId)
        {
            try
            {
                string relativePath;
                if (pageId.StartsWith("harmony-"))
                {
                    var name = pageId.Substring(8); // remove harmony-
                    relativePath = Path.Combine("harmony", "pages", $"{name}.html");
                }
                else
                {
                    relativePath = Path.Combine("pages", $"{pageId}.html");
                }

                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", relativePath);
                
                if (!File.Exists(htmlPath))
                {
                    return new { success = false, error = $"Page not found: {pageId}" };
                }
                
                string content = File.ReadAllText(htmlPath);
                return new { success = true, content = content };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }


        private async Task<object> HandleHarmonyRequest(ApiRequest request)
        {
            if (harmonyService == null) return new { success = false, error = "Harmony service not initialized" };
            
            var dataStr = request.Data?.ToString() ?? "{}";

            switch (request.Action)
            {
                case "harmony_getEnvInfo":
                    return await harmonyService.GetEnvInfo();
                    
                case "harmony_getAccountInfo":
                    return harmonyService.GetAccountInfo();
                    
                case "harmony_getBuildInfo":
                    return harmonyService.GetBuildInfo();
                    
                case "harmony_uploadHap":
                    var uploadData = Newtonsoft.Json.JsonConvert.DeserializeObject<UploadHapDto>(dataStr);
                    if (uploadData == null) return new { success = false, error = "Invalid data" };
                    return await harmonyService.SaveFileToLocal(uploadData.Buffer, uploadData.FileName);
                    
                case "harmony_checkAccount":
                    var commonInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<CommonInfo>(dataStr);
                    if (commonInfo != null) await harmonyService.Build.CheckEcoAccount(commonInfo);
                    return harmonyService.GetAccountInfo();
                    
                case "harmony_loginHuawei":
                    var loginInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<CommonInfo>(dataStr);
                    await OpenHuaweiOAuthWindow(loginInfo);
                    return harmonyService.GetAccountInfo();
                    
                case "harmony_startBuild":
                    var buildCommon = Newtonsoft.Json.JsonConvert.DeserializeObject<CommonInfo>(dataStr);
                    if (buildCommon != null) await harmonyService.Build.StartBuild(buildCommon);
                    return harmonyService.GetBuildInfo();

                case "harmony_openBigHap":
                    var result = await SelectBigHapAsync();
                    return result ?? new { success = false, error = "No file selected" };

                case "harmony_getGitBranches":
                    dynamic? urlObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(dataStr);
                    string url = urlObj?.url?.ToString() ?? "";
                    return await harmonyService.GetGitBranches(url);
                    
                default:
                     return new { success = false, error = "Unknown harmony action" };
            }
        }

        private async Task OpenHuaweiOAuthWindow(CommonInfo? info)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                HarmonyAuthServer? authServer = null;
                try
                {
                    Console.WriteLine("[华为认证] 开始认证流程");
                    
                    // 使用 HarmonyAuthServer 进行正确的认证
                    authServer = new HarmonyAuthServer(harmonyService!.Eco);
                    
                    // 订阅认证成功事件
                    authServer.OnAuthSuccess += async (sender, userInfo) =>
                    {
                        // 延迟停止服务器，确保响应已发送
                        await Task.Delay(500);
                        
                        Dispatcher.Invoke(() =>
                        {
                            Console.WriteLine($"[华为认证] 登录成功: {userInfo.NickName}");
                            MessageBox.Show($"登录成功！\n用户: {userInfo.NickName}\nUserID: {userInfo.UserId}",
                                "认证成功", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // 保存认证信息
                            SaveAuthInfo(userInfo);
                            
                            // 延迟停止认证服务器
                            try
                            {
                                authServer?.Stop();
                                authServer?.Dispose();
                            }
                            catch (Exception stopEx)
                            {
                                Console.WriteLine($"[华为认证] 停止服务器时出错: {stopEx.Message}");
                            }
                            
                            // 更新账号信息
                            if (harmonyService != null)
                            {
                                _ = harmonyService.Build.CheckEcoAccount(info ?? new CommonInfo());
                            }
                        });
                    };
                    
                    // 订阅认证失败事件
                    authServer.OnAuthError += async (sender, error) =>
                    {
                        // 延迟停止服务器，确保响应已发送
                        await Task.Delay(500);
                        
                        Dispatcher.Invoke(() =>
                        {
                            Console.WriteLine($"[华为认证] 登录失败: {error}");
                            MessageBox.Show($"登录失败: {error}", "认证失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            
                            try
                            {
                                authServer?.Stop();
                                authServer?.Dispose();
                            }
                            catch (Exception stopEx)
                            {
                                Console.WriteLine($"[华为认证] 停止服务器时出错: {stopEx.Message}");
                            }
                        });
                    };
                    
                    // 启动认证服务器
                    var port = await authServer.StartAsync();
                    Console.WriteLine($"[华为认证] 认证服务器已启动在端口: {port}");
                    
                    // 打开浏览器进行认证
                    authServer.OpenAuthPage();
                    
                    MessageBox.Show("请在浏览器中完成登录授权", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[华为认证] 启动失败: {ex.Message}");
                    Console.WriteLine($"[华为认证] 错误堆栈: {ex.StackTrace}");
                    MessageBox.Show($"启动认证失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // 清理资源
                    try
                    {
                        authServer?.Stop();
                        authServer?.Dispose();
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"[华为认证] 清理资源时出错: {cleanupEx.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// 保存认证信息到文件
        /// </summary>
        private void SaveAuthInfo(UserInfo userInfo)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(userInfo);
                string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                string filePath = Path.Combine(configDir, "ds-authInfo.json");
                File.WriteAllText(filePath, json);
                Console.WriteLine($"[华为认证] 认证信息已保存: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[华为认证] 保存认证信息失败: {ex.Message}");
            }
        }

        private async Task<object?> SelectBigHapAsync()
        {
            string? selectedFile = null;
            
            // Run dialog on UI thread
            bool? dialogResult = Dispatcher.Invoke(() => 
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select HAP/APP/HSP File",
                    Filter = "HarmonyOS Package (*.hap;*.app;*.hsp)|*.hap;*.app;*.hsp",
                    Multiselect = false
                };
                
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    selectedFile = dialog.FileName;
                }
                return result;
            });

            if (dialogResult == true && !string.IsNullOrEmpty(selectedFile))
            {
                 // Perform file I/O off the UI thread
                 try 
                 {
                     var bytes = await File.ReadAllBytesAsync(selectedFile);
                     return await harmonyService!.SaveFileToLocal(bytes, Path.GetFileName(selectedFile));
                 }
                 catch (Exception ex)
                 {
                     return new { success = false, error = $"File read error: {ex.Message}" };
                 }
            }
            return null;
        }

        public class UploadHapDto
        {
            public byte[] Buffer { get; set; } = Array.Empty<byte>();
            public string FileName { get; set; } = string.Empty;
        }

        private void SendResponse(string requestId, object result)
        {
            var response = new
            {
                requestId = requestId,
                result = result
            };
            string jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            webView.CoreWebView2.PostWebMessageAsString(jsonResponse);
        }

        private async Task<object> SelectAndInstallApkAsync()
        {
            try
            {
                string? apkPath = null;
                
                bool? result = Dispatcher.Invoke(() => 
                {
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = "Select APK File",
                        Filter = "Android Package (*.apk)|*.apk|All Files (*.*)|*.*",
                        Multiselect = false
                    };
                    bool? res = openFileDialog.ShowDialog();
                    if (res == true) apkPath = openFileDialog.FileName;
                    return res;
                });

                if (result == true && !string.IsNullOrEmpty(apkPath))
                {
                    if (adbManager == null)
                    {
                        return new { success = false, message = "ADB Manager not initialized" };
                    }

                    var installResult = await adbManager.InstallApkFileAsync(apkPath);
                    return installResult;
                }

                return new { success = false, message = "No file selected" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = $"Install failed: {ex.Message}" };
            }
        }
        private async Task<object> SelectAndInstallFolderAsync()
        {
            try
            {
                string? folderPath = null;
                
                bool? result = Dispatcher.Invoke(() => {
                    var openFolderDialog = new Microsoft.Win32.OpenFolderDialog
                    {
                        Title = "Select APK Folder",
                        Multiselect = false
                    };
                    bool? res = openFolderDialog.ShowDialog();
                    if (res == true) folderPath = openFolderDialog.FolderName;
                    return res;
                });

                if (result == true && !string.IsNullOrEmpty(folderPath))
                {
                    if (adbManager == null)
                    {
                        return new { success = false, message = "ADB Manager not initialized" };
                    }

                    var progress = new Progress<string>(msg =>
                    {
                        SendResponse("log", new { message = msg, type = "info" });
                    });

                    return await adbManager.InstallApkFolderAsync(folderPath, progress);
                }

                return new { success = false, message = "No folder selected" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = $"Batch install failed: {ex.Message}" };
            }
        }
    }

    public class ApiRequest
    {
        [Newtonsoft.Json.JsonProperty("requestId")]
        public string? RequestId { get; set; }
        
        [Newtonsoft.Json.JsonProperty("action")]
        public string? Action { get; set; }
        
        [Newtonsoft.Json.JsonProperty("data")]
        public object? Data { get; set; }
    }

    public class AnimationConfig
    {
        public double Window { get; set; }
        public double Transition { get; set; }
        public double Animator { get; set; }
    }
}