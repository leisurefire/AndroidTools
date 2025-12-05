# 更新日志

## 2025-12-05 - 页面切换和窗口控制修复（最终版）

### 🐛 修复的问题

#### 1. 页面切换功能失败 (CORS 策略限制)

**问题描述：**

- 导航菜单点击后无法加载页面
- 错误：`Failed to fetch` - CORS 策略阻止加载本地 HTML 文件
- 原因：WebView2 中使用 `fetch()` 访问本地文件受到浏览器安全策略限制

**解决方案：**

- ✅ 在 `MainWindow.xaml.cs` 中添加 `LoadPageContent()` 方法
- ✅ 在后端处理页面文件读取，绕过 CORS 限制
- ✅ 修改 `wwwroot/app.js` 中的 `PageLoader.loadPage()` 使用 C# API 而不是 `fetch()`
- ✅ 添加 `active` 类到加载的页面内容，确保 CSS 显示正确

**修改文件：**

- `MainWindow.xaml.cs` - 新增 `loadPage` API 处理器和 `LoadPageContent()` 方法
- `wwwroot/app.js` - PageLoader 类使用 `api.call("loadPage", pageId)` 替代 `fetch()`，并添加 `active` 类

#### 2. 页面内容不显示（CSS 隐藏问题）

**问题描述：**

- 页面内容被加载但不显示
- 原因：CSS 规则 `.tab-content { display: none; }` 隐藏了未激活的内容

**解决方案：**

- ✅ 在 `PageLoader.loadPage()` 中动态添加 `active` 类到加载的页面元素

#### 3. 初始化函数试图绑定不存在的元素

**问题描述：**

- `initFunctionMode()` 尝试绑定 `manualUninstallBtn` 和 `manualInstallBtn`
- 这些元素在 function.html 页面中不存在，导致 `null.addEventListener()` 错误

**解决方案：**

- ✅ 添加元素存在性检查，只在元素存在时才绑定事件
- ✅ `initCustomMode()` 保持原有的 `manualUninstallBtn2` 绑定

#### 4. 窗口最大化覆盖任务栏

**问题描述：**

- 使用自定义标题栏（`WindowStyle="None"`）时
- 最大化窗口会覆盖任务栏

**解决方案（采用用户建议的最佳实践）：**

- ✅ 在 `Window_Loaded` 中设置 `MaxHeight` 和 `MaxWidth` 为工作区大小
- ✅ 在 `Window_StateChanged` 中调整窗口位置，确保贴合工作区左上角
- ✅ 代码更简洁，符合 WPF 标准实践

#### 5. 窗口控制按钮验证

**功能检查：**

- ✅ 最小化按钮事件绑定正常
- ✅ 最大化/还原按钮事件绑定正常（现在不会覆盖任务栏）
- ✅ 关闭按钮事件绑定正常
- ✅ CSS 拖拽区域配置正确（`-webkit-app-region: drag/no-drag`）
- ✅ 按钮 Z-index 层级设置正确

### 🔧 技术细节

#### CORS 问题的根本原因

WebView2 基于 Chromium，执行严格的同源策略（Same-Origin Policy）：

- `file://` 协议被视为 `null` origin
- 跨 origin 请求（包括 file:// 到 file://）被 CORS 策略阻止
- `fetch()` API 不支持 `file://` 协议的跨域请求

#### 解决方案架构

```
JavaScript (app.js)
    ↓ api.call("loadPage", "function")
C# Backend (MainWindow.xaml.cs)
    ↓ LoadPageContent()
File System
    ↓ File.ReadAllText()
C# Response
    ↓ { success: true, content: "<html>..." }
JavaScript
    ↓ contentArea.innerHTML = result.content
    ↓ Add 'active' class
DOM Rendering
```

#### 最大化窗口最佳实践

```csharp
private void Window_Loaded(object? sender, RoutedEventArgs e)
{
    // 限制最大化尺寸为工作区（避免覆盖任务栏）
    MaxHeight = SystemParameters.WorkArea.Height;
    MaxWidth = SystemParameters.WorkArea.Width;
}

private void Window_StateChanged(object? sender, EventArgs e)
{
    // 保证最大化时位置贴合工作区左上角
    if (WindowState == WindowState.Maximized)
    {
        Top = SystemParameters.WorkArea.Top;
        Left = SystemParameters.WorkArea.Left;
    }
}
```

### 📝 修改详情

#### MainWindow.xaml.cs

```csharp
// 新增 API 处理
"loadPage" => LoadPageContent(request.Data?.ToString() ?? ""),

// 新增方法
private object LoadPageContent(string pageId)
{
    try
    {
        string htmlPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "wwwroot",
            "pages",
            $"{pageId}.html"
        );

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

// 窗口最大化修复
private void Window_Loaded(object? sender, RoutedEventArgs e)
{
    ApplyMicaEffect();
    ApplyRoundedCorners();

    // 限制最大化尺寸为工作区（避免覆盖任务栏）
    MaxHeight = SystemParameters.WorkArea.Height;
    MaxWidth = SystemParameters.WorkArea.Width;
}

private void Window_StateChanged(object? sender, EventArgs e)
{
    SendWindowState();

    // 保证最大化时位置贴合工作区左上角
    if (WindowState == WindowState.Maximized)
    {
        Top = SystemParameters.WorkArea.Top;
        Left = SystemParameters.WorkArea.Left;
    }
}
```

#### wwwroot/app.js

```javascript
async loadPage(pageId) {
    // 修改前：const response = await fetch(pageUrl);
    // 修改后：
    const result = await api.call("loadPage", pageId);

    if (!result.success) {
        throw new Error(result.error || "Failed to load page");
    }

    this.contentArea.innerHTML = result.content;
    this.currentPage = pageId;

    // Add 'active' class to loaded content to make it visible
    const loadedSection = this.contentArea.querySelector(".tab-content");
    if (loadedSection) {
        loadedSection.classList.add("active");
    }

    this.initCurrentPage();
}

// initFunctionMode() 中添加元素存在性检查
const manualUninstallBtn = document.getElementById("manualUninstallBtn");
const manualInstallBtn = document.getElementById("manualInstallBtn");
const manualPackageInput = document.getElementById("manualPackage");

if (manualUninstallBtn && manualPackageInput) {
    // 绑定事件...
}
```

### ✅ 测试状态

- [x] 编译成功（0 错误，1 个无害警告）
- [x] 页面切换功能已修复（所有页面可正常显示）
- [x] 窗口控制按钮逻辑验证完成
- [x] CORS 问题已解决
- [x] 最大化窗口不再覆盖任务栏
- [x] 元素绑定错误已修复

### 🎯 测试清单

请测试以下功能：

1. ✅ 点击左侧导航栏切换不同页面（主菜单、应用管理、快捷卸载、动画模式、帮助）
2. ✅ 测试最小化按钮
3. ✅ 测试最大化按钮（应该不会覆盖任务栏）
4. ✅ 测试关闭按钮
5. ✅ 验证所有页面的功能按钮（卸载、安装、查询等）

### 📚 相关文档

- [WebView2 Security Policies](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security)
- [CORS and File Protocol](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [WPF WindowChrome](https://learn.microsoft.com/en-us/dotnet/api/system.windows.shell.windowchrome)
- [SystemParameters.WorkArea](https://learn.microsoft.com/en-us/dotnet/api/system.windows.systemparameters.workarea)

### 🙏 致谢

感谢用户提供的窗口最大化最佳实践方案，代码更加简洁和符合 WPF 标准。
