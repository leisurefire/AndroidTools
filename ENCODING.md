# 编码说明

## 批处理文件编码问题

原项目中的 `.bat` 文件使用 **GBK/GB2312** 编码（Windows简体中文默认编码），而非 UTF-8。

### 已处理的编码问题

1. **ADB输出编码**
   ```csharp
   StandardOutputEncoding = Encoding.GetEncoding("GBK")
   StandardErrorEncoding = Encoding.GetEncoding("GBK")
   ```

2. **文件读取编码**
   - 如果需要读取 `.bat` 文件内容，使用：
   ```csharp
   File.ReadAllText(filePath, Encoding.GetEncoding("GBK"))
   ```

3. **前端显示**
   - HTML使用 UTF-8 编码
   - C# 与 JavaScript 通信使用 UTF-8
   - ADB输出转换为UTF-8后传递给前端

### 编码转换示例

```csharp
// 读取GBK编码的批处理文件
var batContent = File.ReadAllText("HarmonyOS.bat", Encoding.GetEncoding("GBK"));

// 执行ADB命令并正确解析中文输出
var output = iconv.decode(stdout, 'gbk');
```

## 支持的中文内容

- ✅ 应用包名（中文）
- ✅ 系统信息（中文）
- ✅ 日志输出（中文）
- ✅ 错误信息（中文）
- ✅ 命令执行结果（中文）

## 注意事项

- 原 `.bat` 文件保持 GBK 编码
- 新创建的文件使用 UTF-8 编码
- C# 项目文件使用 UTF-8 编码
- HTML/CSS/JS 使用 UTF-8 编码
