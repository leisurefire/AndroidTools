# 鸿蒙 HDC 调试和认证功能实现指南 (WPF C#)

## 📋 目录

- [概述](#概述)
- [已完成的工作](#已完成的工作)
- [架构说明](#架构说明)
- [使用示例](#使用示例)
- [API 参考](#api-参考)
- [注意事项](#注意事项)

---

## 概述

本文档说明如何在 WPF 项目中使用鸿蒙 HDC 调试和华为开发者认证功能。所有功能均从 `auto-installer` (Node.js) 项目正确迁移，确保参数和请求完全一致。

---

## 已完成的工作

### ✅ 核心服务类

1. **HarmonyCmdService** - HDC 命令服务
   - 路径: `Services/Harmony/HarmonyCmdService.cs`
   - 功能: HDC 设备管理、文件传输、应用安装

2. **HarmonyEcoService** - 华为开发者 API 服务
   - 路径: `Services/Harmony/HarmonyEcoService.cs`
   - 功能: 认证、证书管理、Profile 管理、设备管理

3. **HarmonyAuthServer** - 本地认证服务器
   - 路径: `Services/Harmony/HarmonyAuthServer.cs`
   - 功能: 接收华为认证回调、Token 换取

### ✅ HDC 功能

| 功能 | 方法 | HDC 命令 | 状态 |
|------|------|----------|------|
| 设备列表 | `DeviceList()` | `hdc list targets` | ✅ 完成 |
| 无线连接 | `ConnectDevice(device)` | `hdc tconn ip:port` | ✅ 完成 |
| 获取 UDID | `GetUdid(device)` | `hdc shell bm get --udid` | ✅ 完成 |
| 发送文件 | `SendFile(device, filePath)` | `hdc file send` | ✅ 完成 |
| 安装应用 | `InstallHap(device)` | `hdc shell bm install` | ✅ 完成 |
| 签名应用 | `SignHap(config)` | `java -jar hap-sign-tool.jar` | ✅ 完成 |

### ✅ 认证功能

| 功能 | 方法 | API 端点 | 状态 |
|------|------|----------|------|
| Token 换取 | `GetTokenByTempToken(data)` | cn.devecostudio.huawei.com | ✅ 完成 |
| 获取团队列表 | `GetUserTeamList()` | connect-api.cloud.huawei.com | ✅ 完成 |
| 获取证书列表 | `GetCertList()` | connect-api.cloud.huawei.com | ✅ 完成 |
| 创建证书 | `CreateCert()` | connect-api.cloud.huawei.com | ✅ 完成 |
| 创建 Profile | `CreateProfile()` | connect-api.cloud.huawei.com | ✅ 完成 |
| 获取设备列表 | `DeviceList()` | connect-api.cloud.huawei.com | ✅ 完成 |

---

## 架构说明

### 认证流程

```
用户点击登录
    ↓
启动本地 HTTP 服务器 (HarmonyAuthServer)
    ↓
打开浏览器到华为认证页面
    ↓
用户登录华为账号
    ↓
华为服务器 POST 回调到本地服务器 (/callback)
    ↓
提取 tempToken
    ↓
验证 tempToken 获取 JWT Token
https://cn.devecostudio.huawei.com/authrouter/auth/api/temptoken/check
    ↓
使用 JWT Token 获取用户信息
https://cn.devecostudio.huawei.com/authrouter/auth/api/jwToken/check
    ↓
提取 accessToken 作为 OAuth2Token
    ↓
保存认证信息，触发成功事件
```

### HDC 调试流程

```
应用启动
    ↓
初始化 HarmonyCmdService
    ↓
检查 HDC 工具路径
    ↓
获取设备列表
    ↓
连接设备 (USB 或无线)
    ↓
获取设备 UDID
    ↓
签名 HAP 文件
    ↓
传输文件到设备
    ↓
安装应用
```

---

## 使用示例

### 1. 华为开发者认证

```csharp
// 在 MainWindow.xaml.cs 或其他地方

using HarmonyOSToolbox.Services.Harmony;

public partial class MainWindow : Window
{
    private HarmonyEcoService _ecoService;
    private HarmonyAuthServer? _authServer;

    public MainWindow()
    {
        InitializeComponent();
        _ecoService = new HarmonyEcoService();
    }

    // 用户点击登录按钮
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. 创建认证服务器
            _authServer = new HarmonyAuthServer(_ecoService);

            // 2. 订阅事件
            _authServer.OnAuthSuccess += AuthServer_OnAuthSuccess;
            _authServer.OnAuthError += AuthServer_OnAuthError;

            // 3. 启动服务器
            var port = await _authServer.StartAsync();
            Console.WriteLine($"认证服务器已启动在端口: {port}");

            // 4. 打开浏览器进行认证
            _authServer.OpenAuthPage();

            MessageBox.Show("请在浏览器中完成登录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动认证失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 认证成功回调
    private void AuthServer_OnAuthSuccess(object? sender, UserInfo userInfo)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show($"登录成功！\n用户: {userInfo.NickName}\nUserID: {userInfo.UserId}",
                "成功", MessageBoxButton.OK, MessageBoxImage.Information);

            // 保存认证信息到文件（可选）
            SaveAuthInfo(userInfo);

            // 关闭认证服务器
            _authServer?.Stop();
        });
    }

    // 认证失败回调
    private void AuthServer_OnAuthError(object? sender, string error)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show($"登录失败: {error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _authServer?.Stop();
        });
    }

    // 保存认证信息
    private void SaveAuthInfo(UserInfo userInfo)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(userInfo);
        File.WriteAllText("ds-authInfo.json", json);
    }

    // 加载认证信息
    private async Task<bool> LoadAuthInfo()
    {
        try
        {
            if (File.Exists("ds-authInfo.json"))
            {
                var json = File.ReadAllText("ds-authInfo.json");
                var userInfo = System.Text.Json.JsonSerializer.Deserialize<UserInfo>(json);
                if (userInfo != null)
                {
                    _ecoService.InitCookie(userInfo);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载认证信息失败: {ex.Message}");
        }
        return false;
    }
}
```

### 2. HDC 设备管理和应用安装

```csharp
using HarmonyOSToolbox.Services.Harmony;

public class HarmonyDebugService
{
    private HarmonyCmdService _cmdService;
    private HarmonyEcoService _ecoService;

    public HarmonyDebugService()
    {
        _cmdService = new HarmonyCmdService();
        _ecoService = new HarmonyEcoService();
    }

    // 检查设备连接
    public async Task<List<string>> GetDevices()
    {
        Console.WriteLine("[HDC] 正在获取设备列表...");
        var devices = await _cmdService.DeviceList();
        Console.WriteLine($"[HDC] 发现 {devices.Count} 个设备");
        return devices;
    }

    // 无线连接设备
    public async Task ConnectWirelessDevice(string ip, int port = 5555)
    {
        var device = $"{ip}:{port}";
        Console.WriteLine($"[HDC] 正在连接设备: {device}");
        await _cmdService.ConnectDevice(device);
        Console.WriteLine($"[HDC] 设备连接成功");
    }

    // 获取设备 UDID
    public async Task<string> GetDeviceUdid(string? device = null)
    {
        Console.WriteLine("[HDC] 正在获取设备 UDID...");
        var udid = await _cmdService.GetUdid(device ?? "");
        Console.WriteLine($"[HDC] UDID: {udid}");
        return udid;
    }

    // 签名并安装 HAP
    public async Task SignAndInstallHap(string hapPath, string? deviceIp = null)
    {
        try
        {
            // 1. 签名 HAP
            Console.WriteLine("[签名] 开始签名应用...");
            var signConfig = new SignConfig
            {
                KeystoreFile = "path/to/keystore.p12",
                KeystorePwd = "xiaobai123",
                KeyAlias = "xiaobai",
                CertFile = "path/to/cert.cer",
                ProfileFile = "path/to/profile.p7b",
                InFile = hapPath,
                OutFile = hapPath.Replace(".hap", "_signed.hap")
            };
            await _cmdService.SignHap(signConfig);
            Console.WriteLine("[签名] 签名完成");

            // 2. 安装到设备
            Console.WriteLine("[安装] 开始安装应用...");
            await _cmdService.SendAndInstall(signConfig.OutFile, deviceIp ?? "");
            Console.WriteLine("[安装] 安装完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] {ex.Message}");
            throw;
        }
    }

    // 创建调试证书
    public async Task<string> CreateDebugCert(string name, string csr)
    {
        Console.WriteLine($"[证书] 正在创建调试证书: {name}");
        var response = await _ecoService.CreateCert(name, csr, type: 1);
        Console.WriteLine($"[证书] 证书创建成功，ID: {response.Data.Id}");
        return response.Data.Id;
    }

    // 创建 Profile
    public async Task<string> CreateDebugProfile(string name, string certId, 
        string packageName, List<string> deviceIds)
    {
        Console.WriteLine($"[Profile] 正在创建 Profile: {name}");
        var moduleJson = new ModuleJson(); // 从 HAP 中读取
        var response = await _ecoService.CreateProfile(name, certId, packageName, deviceIds, moduleJson);
        Console.WriteLine($"[Profile] Profile 创建成功");
        return response.Data.ProvisionFileUrl;
    }
}
```

### 3. 完整的安装流程示例

```csharp
public async Task InstallHarmonyApp(string hapPath, string? deviceIp = null)
{
    try
    {
        // 1. 检查认证状态
        if (string.IsNullOrEmpty(_ecoService.OAuth2Token))
        {
            MessageBox.Show("请先登录华为开发者账号", "提示");
            return;
        }

        // 2. 检查设备
        var devices = await _cmdService.DeviceList();
        string targetDevice;

        if (!string.IsNullOrEmpty(deviceIp))
        {
            // 无线连接
            await _cmdService.ConnectDevice(deviceIp);
            targetDevice = deviceIp;
        }
        else if (devices.Count > 0)
        {
            // USB 连接
            targetDevice = devices[0];
        }
        else
        {
            MessageBox.Show("未发现连接的设备", "错误");
            return;
        }

        // 3. 获取 UDID
        var udid = await _cmdService.GetUdid(targetDevice);

        // 4. 检查或创建证书
        var certList = await _ecoService.GetCertList();
        var debugCert = certList.CertList.FirstOrDefault(c => c.CertType == 1);
        
        if (debugCert == null)
        {
            // 创建 Keystore 和 CSR
            var keystorePath = "xiaobai.p12";
            await _cmdService.CreateKeystore(keystorePath);
            var csrPath = await _cmdService.CreateCsr(keystorePath, "xiaobai.csr");
            var csrContent = File.ReadAllText(csrPath);
            
            // 创建证书
            var certResponse = await _ecoService.CreateCert("xiaobai-debug", csrContent, 1);
            debugCert = certResponse.Data;

            // 下载证书
            var certUrl = await _ecoService.DownloadObj(debugCert.CertObjectId);
            // ... 下载并保存证书文件
        }

        // 5. 创建或获取 Profile
        var moduleJson = _cmdService.LoadModuleJson(hapPath);
        var packageName = moduleJson?.App?.BundleName ?? "com.example.app";

        // 注册设备
        var deviceList = await _ecoService.DeviceList();
        var existingDevice = deviceList.Data.List.FirstOrDefault(d => d.Udid == udid);
        // ... 如果不存在则注册设备

        // 创建 Profile
        var deviceIds = deviceList.Data.List.Select(d => d.DeviceId).ToList();
        var profileResponse = await _ecoService.CreateProfile(
            "xiaobai-debug", debugCert.Id, packageName, deviceIds, moduleJson);
        
        // 下载 Profile
        // ... 下载并保存 Profile 文件

        // 6. 签名 HAP
        var signConfig = new SignConfig
        {
            KeystoreFile = keystorePath,
            KeystorePwd = "xiaobai123",
            KeyAlias = "xiaobai",
            CertFile = "xiaobai-debug.cer",
            ProfileFile = "xiaobai-debug.p7b",
            InFile = hapPath,
            OutFile = hapPath.Replace(".hap", "_signed.hap")
        };
        await _cmdService.SignHap(signConfig);

        // 7. 安装到设备
        await _cmdService.SendAndInstall(signConfig.OutFile, targetDevice);

        MessageBox.Show("应用安装成功！", "成功");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"安装失败: {ex.Message}", "错误");
        Console.WriteLine($"[错误] 完整错误: {ex}");
    }
}
```

---

## API 参考

### HarmonyAuthServer

```csharp
// 启动认证服务器
var authServer = new HarmonyAuthServer(ecoService);
await authServer.StartAsync();

// 打开认证页面
authServer.OpenAuthPage();

// 订阅事件
authServer.OnAuthSuccess += (sender, userInfo) => { /* 处理成功 */ };
authServer.OnAuthError += (sender, error) => { /* 处理失败 */ };

// 停止服务器
authServer.Stop();
```

### HarmonyEcoService

```csharp
var ecoService = new HarmonyEcoService();

// Token 换取
var userInfo = await ecoService.GetTokenByTempToken(tempTokenData);

// 初始化认证
ecoService.InitCookie(userInfo);

// 获取团队列表
var teams = await ecoService.GetUserTeamList();

// 获取证书列表
var certs = await ecoService.GetCertList();

// 创建证书
var cert = await ecoService.CreateCert("name", csr, type: 1);

// 创建 Profile
var profile = await ecoService.CreateProfile(name, certId, packageName, deviceIds, moduleJson);

// 获取设备列表
var devices = await ecoService.DeviceList();
```

### HarmonyCmdService

```csharp
var cmdService = new HarmonyCmdService();

// 检查工具
var tools = await cmdService.CheckTools();

// 设备列表
var devices = await cmdService.DeviceList();

// 连接设备
await cmdService.ConnectDevice("192.168.1.100:5555");

// 获取 UDID
var udid = await cmdService.GetUdid(device);

// 签名 HAP
await cmdService.SignHap(signConfig);

// 发送并安装
await cmdService.SendAndInstall(filePath, deviceIp);

// 创建 Keystore
await cmdService.CreateKeystore(keystorePath);

// 创建 CSR
var csrPath = await cmdService.CreateCsr(keystorePath, csrPath);

// 读取 module.json
var moduleJson = cmdService.LoadModuleJson(hapPath);
```

---

## 注意事项

### ⚠️ 重要

1. **正确的认证端点**
   - ✅ 正确: `https://cn.devecostudio.huawei.com/console/DevEcoIDE/apply`
   - ❌ 错误: `https://oauth-login.cloud.huawei.com/oauth2/v3/authorize`

2. **HDC 工具路径**
   - 确保 `tools/harmony/toolchains/hdc.exe` 存在
   - Windows: `hdc.exe`
   - macOS/Linux: `hdc`

3. **Java 环境**
   - 签名需要 Java 环境
   - 路径: `tools/harmony/jbr/bin/java.exe`

4. **端口占用**
   - 认证服务器使用随机端口
   - 确保防火墙允许本地连接

5. **设备连接**
   - USB 调试需要在手机上授权
   - 无线调试需要同一局域网
   - 获取 IP 和端口: 设置 → 开发者选项 → 无线调试

6. **证书和 Profile**
   - 调试证书类型: `type=1`
   - 生产证书类型: `type=2`
   - Profile 必须绑定设备 UDID
   - Profile 必须包含应用所需权限

### 🐛 常见问题

**Q: 认证后没有收到回调？**
A: 检查本地服务器是否正常启动，端口是否被防火墙阻止。

**Q: HDC 命令执行失败？**
A: 确认 HDC 工具路径正确，设备已连接且授权。

**Q: 签名失败？**
A: 检查 Java 环境、证书文件、Profile 文件是否存在。

**Q: 安装失败？**
A: 确认设备 UDID 在 Profile 白名单中，应用签名正确。

---

## 下一步工作

### 🔨 建议改进

1. **持久化存储**
   - 实现认证信息的加密存储
   - 自动 Token 刷新机制

2. **错误处理**
   - 更详细的错误信息
   - 自动重试机制

3. **UI 集成**
   - 添加进度条显示
   - 实时日志输出
   - 设备列表刷新

4. **配置管理**
   - 证书和 Profile 管理界面
   - 设备管理界面
   - 配置文件导入导出

---

## 参考资料

- **原始实现**: `auto-installer/core/`
- **华为开发者文档**: https://developer.huawei.com/
- **HDC 工具指南**: https://developer.harmonyos.com/cn/docs/documentation/doc-guides/ohos-debugging-tools-0000001215769697

---

**文档版本**: 1.0  
**最后更新**: 2024年  
**维护者**: HarmonyOS Toolbox Team
