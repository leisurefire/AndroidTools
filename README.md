# 鸿蒙工具箱 - GUI版

基于 **C# + WebView2** 的现代化桌面应用，提供图形化界面管理华为/鸿蒙手机。

## 📋 功能特性

### 主菜单
- ✅ 智慧搜索卸载/安装
- ✅ 智慧语音管理
- ✅ 智慧识屏控制
- ✅ 华为音乐卸载
- ✅ 服务中心管理
- ✅ 快应用中心控制
- ✅ 运动健康管理

### 定制模式
- ✅ 华为分享
- ✅ 华为浏览器
- ✅ 游戏空间
- ✅ 应用商店
- ✅ 相机
- ✅ 畅连

### 监视模式
- 📊 查看内存信息
- 📦 查看系统/用户软件包
- 💾 查看应用内存占用

### 动画模式
- 🎬 窗口动画速度调节
- 🔄 过渡动画速度调节
- ⚡ 动画程序速度调节

### 功能模式
- 📱 查询已连接设备
- 🔢 查看AOSP版本
- 🛠️ 手动卸载/安装应用
- 🎯 白名单管理
- ⚙️ 不杀后台模式

## 🚀 快速开始

### 环境要求

- **操作系统**: Windows 10/11
- **.NET**: .NET 6.0 SDK 或更高版本
- **WebView2**: Windows 10/11 自带（自动安装）
- **ADB工具**: 已包含在项目中

### 安装 .NET SDK

1. 访问 [.NET 下载页面](https://dotnet.microsoft.com/download)
2. 下载并安装 .NET 6.0 SDK 或更高版本
3. 验证安装：
```cmd
dotnet --version
```

### 构建项目

```cmd
# 1. 还原依赖
dotnet restore

# 2. 构建项目
dotnet build

# 3. 运行项目
dotnet run
```

### 发布为可执行文件

```cmd
# 发布为单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# 输出文件位置
# bin\Release\net6.0-windows\win-x64\publish\HarmonyOSToolbox.exe
```

## 📂 项目结构

```
鸿蒙工具箱桌面版/
├── HarmonyOSToolbox.csproj    # 项目文件
├── App.xaml                    # 应用程序定义
├── App.xaml.cs                 # 应用程序逻辑
├── MainWindow.xaml             # 主窗口UI
├── MainWindow.xaml.cs          # 主窗口逻辑
├── AdbManager.cs               # ADB命令管理器
├── wwwroot/                    # 前端资源
│   ├── index.html              # 主页面
│   ├── styles.css              # 样式文件
│   └── app.js                  # JavaScript逻辑
├── adb.exe                     # ADB工具
├── AdbWinApi.dll              # ADB依赖库
├── AdbWinDevice.dll           # ADB依赖库
├── AdbWinUsbApi.dll           # ADB依赖库
└── fastboot.exe               # Fastboot工具
```

## 🔧 使用说明

### 1. 连接设备

1. 开启开发者选项：设置 → 关于手机 → 连续点击版本号
2. 打开USB调试：设置 → 系统和更新 → 开发者选项 → USB调试
3. 用数据线连接手机到电脑（支持数据传输的线）
4. 拉下通知栏，将"仅充电"改为"传输文件"
5. 弹出"是否允许USB调试"点击确定

### 2. 使用功能

- **主菜单**: 快速卸载/安装常用应用
- **定制模式**: 高级应用管理
- **监视模式**: 查看设备信息和内存占用
- **动画模式**: 调整系统动画速度
- **功能模式**: 执行高级系统操作

### 3. 注意事项

⚠️ **重要提示**：
- 使用完毕后建议重启手机
- 谨慎卸载系统应用，必要时做好数据备份
- 手机ROM中本就没有的应用无法通过工具装回
- 卸载前请确认该应用不影响其他功能

## 🛠️ 技术栈

- **前端**: HTML5 + CSS3 + JavaScript
- **后端**: C# .NET 6.0
- **UI框架**: WPF + WebView2
- **ADB通信**: Process + GBK编码
- **数据交换**: JSON

## 📦 依赖包

```xml
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2210.55" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## 🎨 界面预览

- **现代化设计**: 渐变色彩，圆角卡片
- **实时日志**: 操作记录实时显示
- **设备状态**: 自动检测连接状态
- **响应式布局**: 适配不同屏幕尺寸

## 🔒 编码处理

项目已处理中文编码问题：
- ADB输出使用 **GBK编码** 解析
- 支持显示中文包名和系统信息
- 日志输出正确显示中文

## 🐛 常见问题

### Q: 提示"device not found"
A: USB调试未连接成功，请检查连接步骤

### Q: 提示"Failure [not installed for 0]"
A: 手机未安装此包名的APP，属于正常提示

### Q: 卸载后无法装回
A: 只能装回手机ROM中原本就有的应用

### Q: WebView2未安装
A: Windows 10/11默认包含，如缺失会自动下载安装

## 📄 许可证

MIT License

## 🙏 致谢

本项目重写自原批处理脚本工具，提供更友好的图形界面。

---

**注意**: 本工具仅供学习交流使用，使用本工具造成的任何损失由使用者自行承担。
