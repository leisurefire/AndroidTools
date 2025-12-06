using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyOSToolbox.Services
{
    /// <summary>
    /// 命令执行工具类 - 优先检查系统PATH，再检查tools文件夹
    /// </summary>
    public class CommandExecutor
    {
        private readonly string toolsDirectory;

        public CommandExecutor()
        {
            // 获取tools目录路径（相对于应用程序根目录）
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            toolsDirectory = Path.Combine(baseDir, "tools");
        }

        /// <summary>
        /// 执行命令 - 优先使用系统PATH，失败时检查tools文件夹
        /// </summary>
        /// <param name="command">命令名称 (如 "adb", "hdc")</param>
        /// <param name="arguments">命令参数</param>
        /// <param name="timeout">超时时间(毫秒)</param>
        /// <returns>执行结果</returns>
        public async Task<CommandResult> ExecuteAsync(string command, string arguments, int timeout = 30000)
        {
            // 1. 先尝试使用系统PATH中的命令
            var result = await TryExecuteAsync(command, arguments, timeout);
            
            if (result.Success)
            {
                return result;
            }

            // 2. 如果系统PATH失败，尝试tools文件夹
            string? toolPath = FindInToolsDirectory(command);
            if (!string.IsNullOrEmpty(toolPath))
            {
                result = await TryExecuteAsync(toolPath, arguments, timeout);
                if (result.Success)
                {
                    return result;
                }
            }

            // 3. 都失败了，返回错误
            return new CommandResult
            {
                Success = false,
                Output = $"未找到命令 '{command}'。请确保已安装并添加到系统PATH，或将其放置在 tools 文件夹中。",
                Error = $"Command '{command}' not found in PATH or tools directory"
            };
        }

        /// <summary>
        /// 尝试执行命令
        /// </summary>
        private async Task<CommandResult> TryExecuteAsync(string fileName, string arguments, int timeout)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Output = "",
                        Error = "Failed to start process"
                    };
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                bool exited = await Task.Run(() => process.WaitForExit(timeout));

                if (!exited)
                {
                    process.Kill();
                    return new CommandResult
                    {
                        Success = false,
                        Output = "",
                        Error = "Command execution timeout"
                    };
                }

                string output = await outputTask;
                string error = await errorTask;

                // Some commands output to stderr even on success
                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    // daemon started is not really an error
                    if (error.Contains("daemon started successfully") || error.Contains("成功"))
                    {
                        return new CommandResult
                        {
                            Success = true,
                            Output = output + "\n" + error,
                            Error = ""
                        };
                    }

                    return new CommandResult
                    {
                        Success = false,
                        Output = output,
                        Error = error
                    };
                }

                return new CommandResult
                {
                    Success = true,
                    Output = output,
                    Error = error
                };
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Command not found in PATH
                return new CommandResult
                {
                    Success = false,
                    Output = "",
                    Error = "Command not found"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Output = "",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// 在tools目录中查找命令
        /// </summary>
        private string? FindInToolsDirectory(string command)
        {
            if (!Directory.Exists(toolsDirectory))
            {
                return null;
            }

            // 尝试不同的文件扩展名
            string[] extensions = { ".exe", ".cmd", ".bat", "" };

            foreach (var ext in extensions)
            {
                string fullPath = Path.Combine(toolsDirectory, command + ext);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // 也检查子目录 (如 tools/platform-tools/adb.exe)
            try
            {
                var files = Directory.GetFiles(toolsDirectory, command + ".*", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    return files[0];
                }
            }
            catch
            {
                // Ignore directory access errors
            }

            return null;
        }

        /// <summary>
        /// 检查命令是否可用
        /// </summary>
        public bool IsCommandAvailable(string command)
        {
            // 检查系统PATH
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore
            }

            // 检查tools目录
            return !string.IsNullOrEmpty(FindInToolsDirectory(command));
        }
    }

    /// <summary>
    /// 命令执行结果
    /// </summary>
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
