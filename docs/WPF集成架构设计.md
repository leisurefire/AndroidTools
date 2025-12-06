# 鸿蒙NEXT应用自动安装功能 - WPF集成架构设计

## 📋 项目概述

### 目标
将基于 Electron + Vue3 的鸿蒙应用自动安装工具迁移到现有的 WPF + WebView2 项目中，实现双模式切换功能。

### 核心需求
- 保留现有 ADB 工具功能
- 新增鸿蒙应用安装模式
- 支持模式间无缝切换
- 复用现有 WebView2 通信机制

### 技术约束
- 基于 .NET 9.0 框架
- 使用现有 WebView2 架构
- 保持前端技术栈一致性（原生 JS + HTML）
- 工具链需内置（不依赖外部安装）

---

## 🏗️ 系统架构设计

### 分层架构

```
┌─────────────────────────────────────────┐
│           表现层 (Presentation)          │
│  ┌─────────────┬─────────────────────┐  │
│  │  WPF 窗体   │   WebView2 前端     │  │
│  │  (主窗口)   │  (HTML/JS/CSS)      │  │
│  └─────────────┴─────────────────────┘  │
├─────────────────────────────────────────┤
│           应用层 (Application)           │
│  ┌─────────────┬─────────────────────┐  │
│  │ ADB Manager │  Harmony Service    │  │
│  │   (现有)    │      (新增)         │  │
│  └─────────────┴─────────────────────┘  │
├─────────────────────────────────────────┤
│            领域层 (Domain)               │
│  ┌──────────────────────────────────┐   │
│  │  Models / Entities / ValueObjects│   │
│  └──────────────────────────────────┘   │
├─────────────────────────────────────────┤
│         基础设施层 (Infrastructure)      │
│  ┌─────────────┬─────────────────────┐  │
│  │  文件系统   │   外部工具调用       │  │
│  │  HTTP客户端 │   进程管理           │  │
│  └─────────────┴─────────────────────┘  │
└─────────────────────────────────────────┘
```

### 模块划分

#### 1. **核心模块**
- `HarmonyCoreService` - 服务协调器，管理所有子服务
- `HarmonyBuildService` - 构建流程管理
- `HarmonyEcoService` - DevEco平台交互
- `HarmonyCmdService` - 命令行工具封装

#### 2. **辅助模块**
- `HarmonyDownloadHelper` - 文件下载与管理
- `HttpClientService` - HTTP请求封装
- `GitService` - Git操作封装（使用LibGit2Sharp）

#### 3. **数据模型**
- 通用模型：`CommonInfo`, `StepInfo`
- 业务模型：`HapInfo`, `SignConfig`, `EcoConfig`
- 传输模型：`ModuleJson`, `CertInfo`, `ProfileInfo`

---

## 🔄 通信机制设计

### IPC通信流程

```
前端 (JavaScript)
    ↓ [postMessage]
WebView2 Bridge
    ↓ [WebMessageReceived]
MainWindow Handler
    ↓ [路由分发]
Service Layer
    ↓ [业务处理]
External Tools
```

### 消息协议

#### 请求格式
```json
{
  "requestId": "unique_id",
  "action": "harmony_actionName",
  "data": { /* 参数对象 */ }
}
```

#### 响应格式
```json
{
  "requestId": "unique_id",
  "result": {
    "success": true/false,
    "data": { /* 返回数据 */ },
    "error": "错误信息"
  }
}
```

### API 命名规范
- ADB 相关：`checkDevice`, `installApp`, `uninstallApp`
- 鸿蒙相关：`harmony_uploadHap`, `harmony_checkAccount`, `harmony_startBuild`

---

## 🎨 前端架构设计

### 模式管理策略

#### 模式定义
```javascript
modes = {
  adb: {
    name: 'ADB 工具模式',
    menuItems: [...],
    defaultPage: 'function'
  },
  harmony: {
    name: '鸿蒙应用安装模式',
    menuItems: [...],
    defaultPage: 'harmony-upload'
  }
}
```

#### 切换机制
1. 用户点击模式切换按钮
2. 更新 localStorage 保存偏好
3. 动态加载对应菜单项
4. 加载默认页面
5. 应用对应样式类

### 目录结构设计

```
wwwroot/
├── index.html              # 主入口（扩展模式切换UI）
├── app.js                  # 主控制器（扩展模式管理）
├── styles.css              # 全局样式
├── pages/                  # ADB模式页面
└── harmony/                # 鸿蒙模式资源
    ├── harmony-api.js      # API封装
    ├── harmony-styles.css  # 模式样式
    └── pages/              # 鸿蒙页面
        ├── upload.html     # 文件上传
        ├── account.html    # 账户管理
        ├── build.html      # 构建安装
        └── settings.html   # 设置
```

### 页面组件设计

#### upload.html - 文件上传页面
- 拖拽上传区域
- 文件信息展示（包名、版本、图标）
- 支持 .hap/.app/.hsp 格式
- 大文件选择器按钮

#### account.html - 账户管理页面
- 登录状态显示
- 证书列表管理
- Profile列表管理
- 一键清理功能

#### build.html - 构建安装页面
- 步骤进度展示
- 设备连接管理
- 签名参数配置
- 安装日志输出

---

## 💾 数据存储设计

### 配置文件位置
```
%USERPROFILE%/.autoPublisher/
├── config/
│   ├── eco_config.json      # DevEco配置
│   ├── ds-authInfo.json     # 认证信息
│   └── hw_cookies.json      # Cookie存储
├── haps/                     # HAP文件缓存
├── signeds/                  # 签名文件输出
└── code/                     # Git代码目录
```

### 密钥存储策略
- 密钥库文件：使用 DPAPI 加密存储
- OAuth Token：存储在 JSON 文件，定期刷新
- 密码：不存储明文，使用 Windows 凭据管理器

---

## 🔧 工具链集成

### 工具目录结构
```
tools/
├── adb/                    # 现有ADB工具
└── harmony/                # 鸿蒙工具链
    ├── jbr/                # Java运行时
    └── toolchains/
        ├── hdc.exe         # 设备连接工具
        └── lib/
            ├── hap-sign-tool.jar
            ├── app_unpacking_tool.jar
            └── app_packing_tool.jar
```

### 工具调用策略
- 使用 `Process.Start()` 异步调用
- 捕获标准输出和错误输出
- 设置合理的超时时间
- 提供取消操作支持

---

## 🔐 安全设计

### 认证安全
- Token 加密存储
- 自动刷新机制
- 会话超时处理

### 文件安全
- 临时文件及时清理
- 敏感文件权限控制
- 上传文件大小限制

### 通信安全
- HTTPS 强制使用
- 证书验证
- 请求签名验证

---

## 📊 性能设计

### 异步处理
- 所有 I/O 操作使用 async/await
- 长时间操作显示进度
- 支持操作取消

### 资源管理
- HttpClient 实例复用
- 文件流式处理
- 内存及时释放

### 缓存策略
- 证书信息缓存
- Profile 信息缓存
- HAP 解析结果缓存

---

## 🚦 错误处理策略

### 错误分类
1. **网络错误**：重试机制 + 用户提示
2. **文件错误**：详细错误信息 + 恢复建议
3. **认证错误**：自动刷新 + 重新登录引导
4. **工具错误**：错误日志 + 降级方案

### 日志策略
- 分级日志（Debug/Info/Warning/Error）
- 日志文件轮转
- 敏感信息脱敏

---

## 📈 扩展性设计

### 插件化架构
- 服务接口定义
- 依赖注入容器
- 动态加载机制

### 版本兼容
- API 版本管理
- 向后兼容保证
- 平滑升级路径

---

## 🎯 质量保证

### 测试策略
- 单元测试覆盖核心逻辑
- 集成测试验证流程
- UI 自动化测试

### 监控指标
- 操作成功率
- 响应时间
- 错误率统计

---

## 📝 接口契约

### 前端需要的 API

| API | 用途 | 参数 | 返回值 |
|-----|------|------|--------|
| `harmony_uploadHap` | 上传HAP文件 | buffer, fileName | hapInfos[] |
| `harmony_openBigHap` | 选择大文件 | - | hapInfo |
| `harmony_getEnvInfo` | 获取环境信息 | - | envInfo |
| `harmony_getAccountInfo` | 获取账户信息 | - | accountInfo |
| `harmony_checkAccount` | 检查账户 | commonInfo | accountInfo |
| `harmony_getBuildInfo` | 获取构建信息 | - | buildInfo |
| `harmony_startBuild` | 开始构建 | commonInfo | buildInfo |
| `harmony_openLogin` | 打开登录 | - | - |
| `harmony_clearCerts` | 清理证书 | - | - |
| `harmony_getGitBranches` | 获取Git分支 | url | branches[] |

### 后端服务接口

| 服务 | 职责 | 主要方法 |
|------|------|----------|
| HarmonyCoreService | 协调管理 | SaveFileToLocal, LoadBigHap, GetGitBranches |
| HarmonyEcoService | DevEco交互 | GetCertList, CreateCert, CreateProfile |
| HarmonyBuildService | 构建流程 | CheckEcoAccount, StartBuild, ClearCerts |
| HarmonyCmdService | 命令执行 | SignHap, CreateKeystore, DeviceList |

---

## 📅 实施路线图

### Phase 1: 基础设施（3小时）
- 创建项目结构
- 定义数据模型
- 配置工具链

### Phase 2: 服务层（5小时）
- 实现核心服务
- 集成外部API
- 命令行封装

### Phase 3: 前端集成（5小时）
- 模式切换UI
- 页面开发
- API对接

### Phase 4: 测试优化（3小时）
- 功能测试
- 性能优化
- 错误处理

### Phase 5: 文档部署（2.5小时）
- 用户文档
- 部署脚本
- 发布测试

---

**文档版本**: v2.0.0  
**更新日期**: 2025-12-06  
**设计原则**: 模块化、可扩展、安全可靠