# GitHub Actions 工作流说明

## 📋 概述

本项目配置了自动化构建和发布工作流，支持**版本号自动自增**，可以通过 GitHub Actions 手动触发构建并自动发布到 Release。

## 🚀 使用方法

### 1. 手动触发构建

1. 进入 GitHub 仓库页面
2. 点击顶部菜单栏的 **Actions** 标签
3. 在左侧选择 **Build and Release** 工作流
4. 点击右侧的 **Run workflow** 按钮
5. 填写参数：
   - **版本类型** (version_type): 选择版本递增方式
     - `patch` - Bug 修复 (1.0.0 → 1.0.1)
     - `minor` - 新功能 (1.0.1 → 1.1.0)
     - `major` - 重大更新 (1.1.0 → 2.0.0)
   - **发布说明** (release_notes): 描述本次更新的内容（可选）
6. 点击 **Run workflow** 开始构建

### 2. 版本号自动自增规则

系统会自动从最新的 Git Tag 读取版本号并递增：

| 当前版本 | 选择类型 | 新版本 | 说明                 |
| -------- | -------- | ------ | -------------------- |
| v1.2.3   | patch    | v1.2.4 | Bug 修复、小改进     |
| v1.2.4   | minor    | v1.3.0 | 新功能、向后兼容     |
| v1.3.0   | major    | v2.0.0 | 重大更新、破坏性变更 |

**特殊情况**：

- 如果仓库没有任何 tag，将从 `v0.0.1` 开始
- Minor/Major 自增时，会自动重置较低级别的版本号为 0

### 3. 构建过程

工作流将自动执行以下步骤：

1. ✅ 检出代码（包含所有历史记录和标签）
2. ✅ **计算新版本号**（自动从最新 tag 读取并递增）
3. ✅ 设置 .NET 9.0 环境
4. ✅ 还原 NuGet 依赖
5. ✅ 构建项目（Release 配置）
6. ✅ 发布两个版本：
   - **单文件版本** (~25 MB): 需要系统已安装 .NET 9.0 运行时
   - **完整版本** (~80-100 MB): 包含所有依赖，无需安装运行时
7. ✅ 创建压缩包
8. ✅ 自动创建 GitHub Release 并上传文件

### 4. 发布内容

每次构建会生成两个压缩包：

- `HarmonyOSToolbox-{版本号}-single-file.zip`

  - 体积小（约 5-10 MB）
  - 需要用户系统已安装 .NET 9.0 运行时
  - 适合已安装 .NET 的用户

- `HarmonyOSToolbox-{版本号}-self-contained.zip`
  - 体积较大（约 80-100 MB）
  - 包含完整的 .NET 运行时
  - 无需用户安装任何依赖

## 📦 Release 页面

构建完成后，在仓库的 **Releases** 页面可以看到：

- 📌 Release 标题：`鸿蒙工具箱 {版本号}`
- 📝 自动生成的发布说明
- 📥 可下载的压缩包文件
- 📖 使用说明和注意事项

## ⚙️ 工作流配置

工作流文件位置: `.github/workflows/release.yml`

### 触发条件

```yaml
on:
  workflow_dispatch: # 手动触发
    inputs:
      version: # 版本号参数
      release_notes: # 发布说明参数
```

### 运行环境

- **操作系统**: Windows Latest
- **.NET 版本**: 9.0.x
- **构建配置**: Release

### 发布配置

```yaml
# 单文件版本
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# 完整版本
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 🔍 查看构建状态

1. 在 **Actions** 标签页可以看到所有运行记录
2. 点击具体的运行可以查看详细日志
3. 绿色 ✅ 表示成功，红色 ❌ 表示失败

## 🛠️ 常见问题

### Q: 工作流运行失败怎么办？

A: 点击失败的运行记录，查看具体错误日志，常见问题：

- 依赖包下载失败：重新运行即可
- 版本号格式错误：确保使用 `vX.Y.Z` 格式
- 权限问题：检查仓库的 Actions 权限设置

### Q: 如何删除错误的 Release？

A: 在 **Releases** 页面找到对应版本，点击 **Delete** 删除

### Q: 能否自动构建？

A: 当前配置为手动触发，如需自动构建可以修改触发条件：

```yaml
on:
  push:
    tags:
      - "v*" # 推送标签时自动构建
```

### Q: 如何修改发布说明模板？

A: 编辑 `.github/workflows/release.yml` 文件中的 `body` 部分

## 📝 版本号规范

建议遵循语义化版本规范（Semantic Versioning）：

- **主版本号**: 重大更新，不兼容的 API 修改
- **次版本号**: 新功能，向下兼容
- **修订号**: Bug 修复，向下兼容

示例：

- `v1.0.0` - 初始版本
- `v1.1.0` - 添加新功能
- `v1.1.1` - 修复 Bug
- `v2.0.0` - 重大更新

## 🔐 权限要求

工作流使用 `GITHUB_TOKEN` 自动授权，无需额外配置。

确保仓库设置中：

1. **Settings** → **Actions** → **General**
2. **Workflow permissions** 设置为：
   - ✅ Read and write permissions

## 📚 相关文档

- [GitHub Actions 文档](https://docs.github.com/actions)
- [.NET 发布文档](https://docs.microsoft.com/dotnet/core/tools/dotnet-publish)
- [语义化版本规范](https://semver.org/lang/zh-CN/)

---

**提示**: 首次使用前请确保已正确配置仓库权限和 Actions 设置。
