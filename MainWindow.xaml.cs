using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;

namespace HarmonyOSToolbox
{
    public partial class MainWindow : Window
    {
        private AdbManager? adbManager;

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
                // Light: rgba(255, 255, 255, 0.4) -> #66FFFFFF
                // Dark: rgba(0, 0, 0, 0.2) -> #33000000
                if (TopBarExtension != null)
                {
                     TopBarExtension.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isDark ? "#33000000" : "#66FFFFFF"));
                }
                
                // Get Windows accent color and send to frontend
                string accentColor = GetSystemAccentColor();
                SendAccentColorToFrontend(accentColor);
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

        private bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    object? val = key.GetValue("AppsUseLightTheme");
                    if (val is int i)
                    {
                        return i == 0;
                    }
                }
            }
            catch { }
            return false;
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
                // Initialize ADB Manager directly (assumes 'adb' is in PATH)
                try
                {
                    adbManager = new AdbManager();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ADB Initialization Warning: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Wait for WebView2 initialization
                await webView.EnsureCoreWebView2Async(null);

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

                // Check ADB Manager initialization
                if (adbManager == null && request.Action != "windowControl")
                {
                    var errorResult = new { success = false, error = "ADB tool not initialized" };
                    SendResponse(request.RequestId, errorResult);
                    return;
                }

                object? result = request.Action switch
                {
                    // Basic Features
                    "checkDevice" => await adbManager!.CheckDeviceAsync(),
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
                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "pages", $"{pageId}.html");
                
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
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select APK File",
                    Filter = "Android Package (*.apk)|*.apk|All Files (*.*)|*.*",
                    Multiselect = false
                };

                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    string apkPath = openFileDialog.FileName;
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
                var openFolderDialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Select APK Folder",
                    Multiselect = false
                };

                bool? result = openFolderDialog.ShowDialog();
                if (result == true)
                {
                    string folderPath = openFolderDialog.FolderName;
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