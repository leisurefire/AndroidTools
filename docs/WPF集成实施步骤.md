# 鸿蒙NEXT应用自动安装功能 - WPF集成实施步骤

## 📋 实施步骤清单

本文档提供给 Code 模式执行的详细步骤清单，每个步骤都是独立可执行的任务。

---

## 第一阶段：基础架构搭建

### 步骤 1.1：添加 NuGet 依赖
**文件**: `HarmonyOSToolbox.csproj`
**任务**: 添加 LibGit2Sharp 包引用
**验收标准**: 项目能正常编译

### 步骤 1.2：创建目录结构
**任务**: 创建以下目录
```
Services/
├── Harmony/
└── Common/
Models/
└── Harmony/
```
**验收标准**: 目录结构存在

### 步骤 1.3：创建数据模型
**目录**: `Models/Harmony/`
**需要创建的类**:

| 文件名 | 类名 | 用途 |
|--------|------|------|
| CommonInfo.cs | CommonInfo | 通用信息（包名、应用名、设备IP等） |
| StepInfo.cs | StepInfo, EnvInfo, AccountInfo, BuildInfo | 步骤状态信息 |
| HapInfo.cs | HapInfo | HAP文件信息 |
| SignConfig.cs | SignConfig | 签名配置 |
| EcoConfig.cs | EcoConfig, CertInfo, ProfileInfo | DevEco配置 |
| ModuleJson.cs | ModuleJson, AppInfo, ModuleInfo, PermissionInfo | module.json结构 |

**属性参考**: 参见 `功能迁移文档-WPF-WebView2集成指南.md` 第9节

### 步骤 1.4：配置工具链文件复制
**文件**: `HarmonyOSToolbox.csproj`
**任务**: 添加 tools/harmony 目录的复制配置
**注意**: 工具链文件需要从 auto-installer 项目的 tools 目录复制

---

## 第二阶段：服务层实现

### 步骤 2.1：实现 HarmonyDownloadHelper
**文件**: `Services/Harmony/HarmonyDownloadHelper.cs`
**职责**: 文件下载与本地存储管理
**主要功能**:
- 初始化配置目录（~/.autoPublisher/config, haps, signeds）
- 下载文件到本地
- 读写 JSON 配置文件
- 读取图片为 Base64

**参考**: `auto-installer/core/downloadHelper.js`

### 步骤 2.2：实现 HarmonyCmdService
**文件**: `Services/Harmony/HarmonyCmdService.cs`
**职责**: 命令行工具封装
**主要功能**:
- 执行命令行命令（ExeCmd）
- HDC 设备操作（DeviceList, ConnectDevice, GetUdid, SendFile, InstallHap）
- Java 工具调用（SignHap, CreateKeystore, CreateCsr）
- HAP 解包打包（UnpackHap, PackHap）
- 读取 module.json（LoadModuleJson）

**工具路径**:
- JavaHome: tools/harmony/jbr
- SdkHome: tools/harmony/toolchains
- HDC: tools/harmony/toolchains/hdc.exe
- SignJar: tools/harmony/toolchains/lib/hap-sign-tool.jar

**参考**: `auto-installer/core/cmdService.js`

### 步骤 2.3：实现 HttpClientService
**文件**: `Services/Common/HttpClientService.cs`
**职责**: HTTP 请求封装
**主要功能**:
- 单例 HttpClient 管理
- 支持自定义 Headers（oauth2Token, teamId, uid）
- GET/POST/DELETE 请求
- JSON 序列化/反序列化

### 步骤 2.4：实现 HarmonyEcoService
**文件**: `Services/Harmony/HarmonyEcoService.cs`
**职责**: DevEco 平台 API 交互
**主要功能**:
- 初始化认证信息（InitCookie）
- 获取团队列表（UserTeamList）
- 证书管理（GetCertList, CreateCert, DeleteCertList）
- Profile管理（CreateProfile）
- 设备管理（DeviceList, CreateDevice）
- 下载对象（DownloadObj）
- ACL权限处理（GetAcl）

**API 端点**: 参见 `功能迁移文档-WPF-WebView2集成指南.md` 附录A

**ACL权限列表**:
```
ohos.permission.READ_AUDIO
ohos.permission.WRITE_AUDIO
ohos.permission.READ_IMAGEVIDEO
ohos.permission.WRITE_IMAGEVIDEO
ohos.permission.SHORT_TERM_WRITE_IMAGEVIDEO
ohos.permission.READ_CONTACTS
ohos.permission.WRITE_CONTACTS
ohos.permission.SYSTEM_FLOAT_WINDOW
ohos.permission.ACCESS_DDK_USB
ohos.permission.ACCESS_DDK_HID
ohos.permission.INPUT_MONITORING
ohos.permission.INTERCEPT_INPUT_EVENT
ohos.permission.READ_PASTEBOARD
```

**参考**: `auto-installer/core/ecoService.js`

### 步骤 2.5：实现 HarmonyBuildService
**文件**: `Services/Harmony/HarmonyBuildService.cs`
**职责**: 构建流程管理
**主要功能**:
- 检查账户状态（CheckEcoAccount）
- 创建并下载调试证书（CreateAndDownloadDebugCert）
- 创建并下载调试Profile（CreateAndDownloadDebugProfile）
- 签名并安装（SignAndInstall）
- 清理证书（ClearCerts）
- 步骤状态管理（StartStep, FinishStep, FailStep, UpdateStep）

**参考**: `auto-installer/core/buildService.js`

### 步骤 2.6：实现 HarmonyCoreService
**文件**: `Services/Harmony/HarmonyCoreService.cs`
**职责**: 核心服务协调器
**主要功能**:
- 初始化所有子服务
- 管理 CommonInfo, EnvInfo, AccountInfo, BuildInfo
- 保存上传的 HAP 文件（SaveFileToLocal）
- 加载大 HAP 文件（LoadBigHap）
- 解析应用图标（ParseIcon）
- 打开登录窗口（OpenLoginWindow）
- 获取 Git 分支（GetGitBranches）

**参考**: `auto-installer/core/services.js`

---

## 第三阶段：MainWindow 消息处理扩展

### 步骤 3.1：添加 HarmonyCoreService 实例
**文件**: `MainWindow.xaml.cs`
**任务**: 
- 添加 `private HarmonyCoreService? harmonyService;` 字段
- 在 InitializeWebViewAsync 中初始化

### 步骤 3.2：扩展消息路由
**文件**: `MainWindow.xaml.cs`
**任务**: 在 CoreWebView2_WebMessageReceived 中添加 harmony_ 前缀的消息处理
**需要处理的 Action**:
- harmony_uploadHap
- harmony_openBigHap
- harmony_getEnvInfo
- harmony_getAccountInfo
- harmony_checkAccount
- harmony_getBuildInfo
- harmony_startBuild
- harmony_openLogin
- harmony_clearCerts
- harmony_getGitBranches

### 步骤 3.3：实现各消息处理方法
**文件**: `MainWindow.xaml.cs`
**任务**: 为每个 harmony_ action 实现对应的处理方法
**注意**: 
- 使用 async/await 处理异步操作
- 统一错误处理格式
- 返回 JSON 格式响应

---

## 第四阶段：前端界面集成

### 步骤 4.1：创建前端目录结构
**任务**: 创建以下目录和文件
```
wwwroot/harmony/
├── harmony-api.js
├── harmony-styles.css
└── pages/
    ├── upload.html
    ├── account.html
    ├── build.html
    └── settings.html
```

### 步骤 4.2：扩展 index.html
**文件**: `wwwroot/index.html`
**任务**: 
- 在侧边栏添加模式切换按钮
- 添加 harmony-api.js 和 harmony-styles.css 引用

### 步骤 4.3：实现 ModeManager 类
**文件**: `wwwroot/app.js`
**任务**: 
- 创建 ModeManager 类管理模式切换
- 实现 switchMode, updateModeUI, updateMenuItems 方法
- 保存模式偏好到 localStorage

### 步骤 4.4：扩展 PageLoader 类
**文件**: `wwwroot/app.js`
**任务**: 
- 添加 harmony 模式页面路径映射
- 扩展 initCurrentPage 方法处理 harmony 页面初始化

### 步骤 4.5：实现 HarmonyAPI 类
**文件**: `wwwroot/harmony/harmony-api.js`
**任务**: 
- 封装所有 harmony_ 前缀的 API 调用
- 实现文件上传的 ArrayBuffer 转换
- 提供 Promise 风格的异步接口

### 步骤 4.6：实现 upload.html 页面
**文件**: `wwwroot/harmony/pages/upload.html`
**功能**:
- 拖拽上传区域
- 文件信息展示卡片
- 大文件选择按钮
- 支持 .hap/.app/.hsp 格式

### 步骤 4.7：实现 account.html 页面
**文件**: `wwwroot/harmony/pages/account.html`
**功能**:
- 登录状态显示
- 登录按钮
- 证书信息展示
- Profile信息展示
- 清理证书按钮

### 步骤 4.8：实现 build.html 页面
**文件**: `wwwroot/harmony/pages/build.html`
**功能**:
- 步骤进度条
- 设备连接输入框
- 开始构建按钮
- 日志输出区域

### 步骤 4.9：实现 harmony-styles.css
**文件**: `wwwroot/harmony/harmony-styles.css`
**任务**: 
- 模式切换按钮样式
- 上传区域样式
- 步骤进度样式
- 卡片组件样式

---

## 第五阶段：测试与优化

### 步骤 5.1：功能测试
**测试项**:
- [ ] 模式切换正常
- [ ] HAP 文件上传解析正常
- [ ] 账户登录流程正常
- [ ] 证书生成下载正常
- [ ] Profile 生成下载正常
- [ ] HAP 签名正常
- [ ] 设备连接正常
- [ ] 应用安装正常

### 步骤 5.2：错误处理优化
**任务**:
- 添加详细的错误提示
- 实现操作重试机制
- 添加日志记录

### 步骤 5.3：性能优化
**任务**:
- 大文件分块处理
- 进度反馈优化
- 内存使用优化

---

## 📝 注意事项

### 工具链准备
在开始实施前，需要从 auto-installer 项目复制以下文件到 tools/harmony 目录：
- jbr/ (Java 运行时)
- toolchains/hdc.exe
- toolchains/lib/hap-sign-tool.jar
- toolchains/lib/app_unpacking_tool.jar
- toolchains/lib/app_packing_tool.jar

### 密钥库文件
项目中已包含预置的密钥库文件：
- auto-installer/store/xiaobai.p12
- auto-installer/store/xiaobai.csr

### 编码注意
- 命令行输出使用 GBK 编码处理中文
- JSON 文件使用 UTF-8 编码
- 路径使用 Path.Combine 处理

### 异步处理
- 所有 I/O 操作使用 async/await
- 避免阻塞 UI 线程
- 长时间操作显示进度

---

## 📊 依赖关系图

```
步骤 1.1 ─┬─→ 步骤 1.3 ─→ 步骤 2.1 ─┬─→ 步骤 2.4 ─┬─→ 步骤 2.6 ─→ 步骤 3.1
          │                         │             │
步骤 1.2 ─┘                         │             │
                                    │             │
步骤 1.4 ─────────────────→ 步骤 2.2 ┘             │
                                                  │
                            步骤 2.3 ─────────────┘
                                                  │
                            步骤 2.5 ─────────────┘
                                                  │
步骤 3.2 ←────────────────────────────────────────┘
    │
    ↓
步骤 3.3 ─→ 步骤 4.1 ─→ 步骤 4.2 ─→ 步骤 4.3 ─→ 步骤 4.4
                                              │
步骤 4.5 ←────────────────────────────────────┘
    │
    ↓
步骤 4.6 ─→ 步骤 4.7 ─→ 步骤 4.8 ─→ 步骤 4.9 ─→ 步骤 5.1
```

---

**文档版本**: v2.0.0  
**更新日期**: 2025-12-06  
**用途**: 提供给 Code 模式执行的实施清单