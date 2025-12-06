# 鸿蒙应用调试迁移工作报告

## 1. 工作概述
本次迁移工作已完成核心架构搭建、服务层实现、WPF集成及前端基础界面的开发。项目已成功编译。

## 2. 已完成工作 (Completed)

### 2.1 基础架构
- [x] 添加 `LibGit2Sharp` 依赖
- [x] 创建 `Services/Harmony`, `Models/Harmony` 等目录结构
- [x] 更新 `.csproj` 配置工具链输出

### 2.2 核心服务 (C#)
- [x] **数据模型**: 完成 `CommonInfo`, `StepInfo`, `HapInfo`, `ModuleJson` 等模型定义
- [x] **HarmonyDownloadHelper**: 实现文件下载、配置读写、Git克隆逻辑
- [x] **HarmonyCmdService**: 实现 HDC 命令封装、Java 签名/解包工具调用
- [x] **HarmonyEcoService**: 实现 DevEco Studio API 接口 (证书/Profile/设备管理)
- [x] **HarmonyBuildService**: 实现构建流程控制 (检查/签名/安装)
- [x] **HarmonyCoreService**: 实现服务协调与状态管理

### 2.3 WPF 集成
- [x] **MainWindow**: 集成 `HarmonyCoreService`
- [x] **IPC通信**: 实现 `harmony_` 系列消息处理 (上传、账户检查、构建等)
- [x] **页面加载**: 扩展 `LoadPageContent` 支持鸿蒙模块页面路径

### 2.4 前端实现
- [x] **API 桥接**: 创建 `harmony-api.js` 封装 WebView2 通信
- [x] **模式切换**: 修改 `index.html` 和 `app.js` 支持 ADB/鸿蒙 双模式切换
- [x] **页面开发**:
    - `upload.html`: HAP 文件拖拽上传与解析信息展示
    - `account.html`: 环境检查与账户状态展示
    - `build.html`: 构建步骤进度监控

## 3. 待完成工作 (Pending)

### 3.1 关键依赖缺失
- [ ] **工具链文件**: `tools/harmony/jbr` 和 `tools/harmony/toolchains` 目录目前为空。
    - **行动**: 请从原 `auto-installer` 项目中复制这些文件夹到 `ADBTools/tools/harmony/` 目录下。

### 3.2 功能完善
- [ ] **登录流程**: `HarmonyBuildService` 目前尝试读取 `ds-authInfo.json`，但未实现完整的 OAuth2 登录 UI 流程。需要实现登录窗口以获取 Token。
- [ ] **证书申请**: 代码已包含 API 调用逻辑，但需在真实环境下验证参数和响应格式。
- [ ] **Git 功能**: `GetGitBranches` 目前为模拟数据，需完善 LibGit2Sharp 调用逻辑。

### 3.3 测试
- [ ] **真机测试**: 需要连接鸿蒙设备测试 HDC 连接和应用安装。
- [ ] **API 测试**: 需要验证华为开发者联盟 API 的连通性。

## 4. 如何运行
1. 确保已复制工具链文件到 `tools/harmony`。
2. 在 Visual Studio 中运行 `HarmonyOSToolbox`。
3. 启动后，点击左侧边栏顶部的 "鸿蒙" 按钮切换模式。
4. 拖拽 HAP 文件进行测试。
