# 项目结构

```
鸿蒙工具箱桌面版/
├── legacy/                     # 原始批处理文件（已迁移）
│   ├── HarmonyOS.bat          # 原主程序
│   ├── trans.bat              # 翻译脚本
│   ├── auto_install.bat       # 自动安装
│   ├── databank.ini           # 配置文件
│   └── ...其他.bat文件
│
├── tools/                      # 工具文件夹
│   └── adb/                   # ADB工具专用目录
│       ├── adb.exe            # ADB主程序
│       ├── AdbWinApi.dll      # ADB依赖库
│       ├── AdbWinDevice.dll   # 设备驱动
│       ├── AdbWinUsbApi.dll   # USB接口
│       ├── fastboot.exe       # Fastboot工具
│       └── MAF32.exe          # 其他工具
│
├── wwwroot/                    # 前端资源
│   ├── index.html             # 主页面
│   ├── styles.css             # 样式表
│   └── app.js                 # JavaScript逻辑
│
├── HarmonyOSToolbox.csproj    # 项目配置
├── App.xaml                    # WPF应用定义
├── App.xaml.cs                 # 应用逻辑
├── MainWindow.xaml             # 主窗口UI
├── MainWindow.xaml.cs          # 主窗口逻辑
├── AdbManager.cs               # ADB命令管理器
├── AdbUpdater.cs               # ADB在线更新器
├── README.md                   # 项目说明
└── ENCODING.md                 # 编码说明
```

## 📁 目录说明

### legacy/ - 原始文件
存放所有原始的批处理脚本文件，作为参考和备份。

### tools/adb/ - ADB工具
- 所有ADB相关的可执行文件和DLL
- 支持在线更新
- 程序会自动从此目录加载ADB工具

### wwwroot/ - 前端资源
- HTML/CSS/JS文件
- 被嵌入到WebView2中显示

## 🔄 ADB更新功能

### 自动检测
- 程序启动时自动检测ADB是否存在
- 如未安装，提示用户下载

### 在线更新
- 从Google官方服务器下载最新版ADB工具
- 下载地址：https://dl.google.com/android/repository/platform-tools-latest-windows.zip
- 自动解压并替换到 `tools/adb/` 目录
- 自动备份旧版本到 `tools/adb/backup_yyyyMMddHHmmss/`

### 手动更新
- 点击顶部"更新ADB"按钮
- 或点击ADB版本号文字
- 显示下载进度

### 版本显示
- 顶部状态栏实时显示当前ADB版本
- 鼠标悬停显示完整版本信息

## 🛠️ 依赖处理

### 自动复制
项目配置自动复制以下文件到输出目录：
- `wwwroot/**/*` - 所有前端文件
- `tools/adb/**/*` - 所有ADB工具文件
- `assets/**/*` - 资源文件（如有）

### NuGet包
```xml
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2210.55" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## 📦 发布说明

### 构建命令
```cmd
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

### 输出结构
```
bin/Release/net6.0-windows/win-x64/publish/
├── HarmonyOSToolbox.exe       # 主程序
├── wwwroot/                    # 前端资源
│   ├── index.html
│   ├── styles.css
│   └── app.js
└── tools/                      # 工具目录
    └── adb/                    # ADB工具
        ├── adb.exe
        └── *.dll
```

### 注意事项
1. **不要**将`legacy/`文件夹包含在发布包中
2. **必须**保留`tools/adb/`目录结构
3. 首次运行如果无ADB，会自动提示下载
4. 确保网络通畅以便下载ADB工具

## 🔐 编码处理

- **原.bat文件**: GBK编码（位于legacy/）
- **新代码文件**: UTF-8编码
- **ADB输出**: GBK编码（已自动处理）
- **前端显示**: UTF-8编码
