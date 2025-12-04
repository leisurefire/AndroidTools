using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace HarmonyOSToolbox
{
    public class AdbUpdater
    {
        private readonly string adbToolsPath;
        private readonly string adbExePath;
        private const string ADB_DOWNLOAD_URL = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";
        
        public AdbUpdater()
        {
            adbToolsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "adb");
            adbExePath = Path.Combine(adbToolsPath, "adb.exe");
        }

        /// <summary>
        /// 检查ADB是否已安装
        /// </summary>
        public bool IsAdbInstalled()
        {
            return File.Exists(adbExePath);
        }

        /// <summary>
        /// 获取当前ADB版本
        /// </summary>
        public async Task<string> GetCurrentVersionAsync()
        {
            if (!IsAdbInstalled())
            {
                return "未安装";
            }

            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = adbExePath,
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process == null)
                {
                    return "未知版本";
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                // 解析版本号（例如：Android Debug Bridge version 1.0.41）
                var lines = output.Split('\n');
                if (lines.Length > 0)
                {
                    var versionLine = lines[0].Trim();
                    return versionLine;
                }

                return "未知版本";
            }
            catch
            {
                return "未知版本";
            }
        }

        /// <summary>
        /// 下载并更新ADB工具
        /// </summary>
        public async Task<(bool success, string message)> DownloadAndUpdateAsync(IProgress<int>? progress = null)
        {
            try
            {
                // 创建临时目录
                string tempDir = Path.Combine(Path.GetTempPath(), "HarmonyOSToolbox_ADB_Update");
                string zipPath = Path.Combine(tempDir, "platform-tools.zip");
                
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                // 下载ADB工具包
                progress?.Report(10);
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    
                    using var response = await client.GetAsync(ADB_DOWNLOAD_URL, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);

                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progressPercentage = (int)((totalRead * 50) / totalBytes) + 10; // 10-60%
                            progress?.Report(progressPercentage);
                        }
                    }
                }

                progress?.Report(60);

                // 解压文件
                string extractPath = Path.Combine(tempDir, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                progress?.Report(70);

                // 查找platform-tools文件夹
                string platformToolsPath = Path.Combine(extractPath, "platform-tools");
                if (!Directory.Exists(platformToolsPath))
                {
                    return (false, "下载的文件格式不正确");
                }

                progress?.Report(80);

                // 备份旧版本
                if (IsAdbInstalled())
                {
                    string backupPath = Path.Combine(adbToolsPath, $"backup_{DateTime.Now:yyyyMMddHHmmss}");
                    Directory.CreateDirectory(backupPath);
                    
                    foreach (var file in Directory.GetFiles(adbToolsPath))
                    {
                        string fileName = Path.GetFileName(file);
                        File.Copy(file, Path.Combine(backupPath, fileName), true);
                    }
                }

                progress?.Report(85);

                // 复制新文件
                Directory.CreateDirectory(adbToolsPath);
                foreach (var file in Directory.GetFiles(platformToolsPath))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(adbToolsPath, fileName);
                    
                    // 如果文件正在使用，先删除
                    if (File.Exists(destFile))
                    {
                        try
                        {
                            File.Delete(destFile);
                        }
                        catch
                        {
                            // 文件可能正在使用，尝试重命名
                            File.Move(destFile, destFile + ".old");
                        }
                    }
                    
                    File.Copy(file, destFile, true);
                }

                progress?.Report(95);

                // 清理临时文件
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // 清理失败不影响更新结果
                }

                progress?.Report(100);

                string version = await GetCurrentVersionAsync();
                return (true, $"ADB更新成功！\n当前版本: {version}");
            }
            catch (Exception ex)
            {
                return (false, $"更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从本地ZIP文件安装ADB
        /// </summary>
        public async Task<(bool success, string message)> InstallFromZipAsync(string zipFilePath, IProgress<int>? progress = null)
        {
            try
            {
                if (!File.Exists(zipFilePath))
                {
                    return (false, "ZIP文件不存在");
                }

                progress?.Report(10);

                // 创建临时目录
                string tempDir = Path.Combine(Path.GetTempPath(), "HarmonyOSToolbox_ADB_Install");
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                progress?.Report(30);

                // 解压文件
                ZipFile.ExtractToDirectory(zipFilePath, tempDir);
                progress?.Report(60);

                // 查找platform-tools文件夹或直接的adb.exe
                string? platformToolsPath = null;
                
                // 尝试找platform-tools文件夹
                var platformTools = Path.Combine(tempDir, "platform-tools");
                if (Directory.Exists(platformTools))
                {
                    platformToolsPath = platformTools;
                }
                else
                {
                    // 直接在根目录查找
                    if (File.Exists(Path.Combine(tempDir, "adb.exe")))
                    {
                        platformToolsPath = tempDir;
                    }
                }

                if (platformToolsPath == null)
                {
                    return (false, "ZIP文件中未找到ADB工具");
                }

                progress?.Report(70);

                // 备份旧版本
                if (IsAdbInstalled())
                {
                    string backupPath = Path.Combine(adbToolsPath, $"backup_{DateTime.Now:yyyyMMddHHmmss}");
                    Directory.CreateDirectory(backupPath);
                    
                    foreach (var file in Directory.GetFiles(adbToolsPath))
                    {
                        string fileName = Path.GetFileName(file);
                        File.Copy(file, Path.Combine(backupPath, fileName), true);
                    }
                }

                progress?.Report(85);

                // 复制新文件
                Directory.CreateDirectory(adbToolsPath);
                foreach (var file in Directory.GetFiles(platformToolsPath))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(adbToolsPath, fileName);
                    
                    if (File.Exists(destFile))
                    {
                        try
                        {
                            File.Delete(destFile);
                        }
                        catch
                        {
                            File.Move(destFile, destFile + ".old");
                        }
                    }
                    
                    File.Copy(file, destFile, true);
                }

                progress?.Report(95);

                // 清理临时文件
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch { }

                progress?.Report(100);

                string version = await GetCurrentVersionAsync();
                return (true, $"ADB安装成功！\n当前版本: {version}");
            }
            catch (Exception ex)
            {
                return (false, $"安装失败: {ex.Message}");
            }
        }
    }
}
