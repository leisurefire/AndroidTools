using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyOSToolbox.Models.Harmony;

namespace HarmonyOSToolbox.Services.Harmony
{
    public class HarmonyCmdService
    {
        private string JavaHome { get; set; }
        private string SdkHome { get; set; }
        private string Hdc { get; set; }
        private string SignJar { get; set; }
        private string UnpackJar { get; set; }
        private string PackJar { get; set; }

        public HarmonyCmdService()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            JavaHome = Path.Combine(basePath, "tools", "harmony", "jbr");
            SdkHome = Path.Combine(basePath, "tools", "harmony", "toolchains");
            Hdc = Path.Combine(SdkHome, "hdc.exe");
            SignJar = Path.Combine(SdkHome, "lib", "hap-sign-tool.jar");
            UnpackJar = Path.Combine(SdkHome, "lib", "app_unpacking_tool.jar");
            PackJar = Path.Combine(SdkHome, "lib", "app_packing_tool.jar");
        }

        public async Task<string> ExeCmd(string cmd, string workDir = null)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {cmd}",
                    WorkingDirectory = workDir ?? Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                if (!string.IsNullOrEmpty(output)) return output;
                throw new Exception(error);
            }
            return output;
        }

        public async Task<List<string>> DeviceList()
        {
            try {
                var result = await ExeCmd($"\"{Hdc}\" list targets");
                if (string.IsNullOrWhiteSpace(result) || result.Contains("[Empty]"))
                    return new List<string>();
                return result.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            } catch {
                return new List<string>();
            }
        }

        public async Task ConnectDevice(string device)
        {
            if (string.IsNullOrEmpty(device)) return;
            var result = await ExeCmd($"\"{Hdc}\" tconn {device}");
            if (string.IsNullOrEmpty(result) || result.Contains("Connect failed"))
                throw new Exception($"连接失败，请检查地址: {device}");
        }

        public async Task<string> GetUdid(string device = "")
        {
            var deviceArg = string.IsNullOrEmpty(device) ? "" : $"-t {device}";
            var result = await ExeCmd($"\"{Hdc}\" {deviceArg} shell bm get --udid");
            if (result.Contains("Not match target founded"))
                throw new Exception($"未发现设备: {device}");
            
            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 ? lines[1].Trim() : lines.FirstOrDefault()?.Trim() ?? "";
        }

        public async Task SignHap(SignConfig config)
        {
            var javaPath = Path.Combine(JavaHome, "bin", "java.exe");
            var signParam = $"-mode \"localSign\" -keyAlias \"{config.KeyAlias}\" " +
                $"-appCertFile \"{config.CertFile}\" -profileFile \"{config.ProfileFile}\" " +
                $"-inFile \"{config.InFile}\" -signAlg \"SHA256withECDSA\" " +
                $"-keystoreFile \"{config.KeystoreFile}\" -keystorePwd \"{config.KeystorePwd}\" " +
                $"-keyPwd \"{config.KeystorePwd}\" -outFile \"{config.OutFile}\" -signCode \"1\"";
            
            await ExeCmd($"\"{javaPath}\" -jar \"{SignJar}\" sign-app {signParam}");
        }

        public async Task SendAndInstall(string filePath, string deviceIp = "")
        {
            string deviceKey = deviceIp;
            if (!string.IsNullOrEmpty(deviceKey))
            {
                await ConnectDevice(deviceKey);
            }
            else
            {
                var devices = await DeviceList();
                if (devices.Count == 0)
                    throw new Exception("请连接手机，并开启开发者模式和USB调试!");
                deviceKey = devices[0].Trim();
            }

            await SendFile(deviceKey, filePath);
            await InstallHap(deviceKey);
        }

        private async Task SendFile(string device, string filePath)
        {
            var deviceArg = string.IsNullOrEmpty(device) ? "" : $"-t {device}";
            await ExeCmd($"\"{Hdc}\" {deviceArg} shell rm -r data/local/tmp/hap");
            await ExeCmd($"\"{Hdc}\" {deviceArg} shell mkdir -p data/local/tmp/hap");
            var result = await ExeCmd($"\"{Hdc}\" {deviceArg} file send \"{filePath}\" data/local/tmp/hap/");
            
            if (!result.Contains("finish") && !result.Contains("FileTransfer finish"))
                 throw new Exception($"传输失败: {result}");
        }

        private async Task InstallHap(string device)
        {
            var deviceArg = string.IsNullOrEmpty(device) ? "" : $"-t {device}";
            var result = await ExeCmd($"\"{Hdc}\" {deviceArg} shell bm install -p data/local/tmp/hap/");
            
            if (!result.Contains("successfully"))
                throw new Exception($"安装失败: {result}");
        }
        
        public ModuleJson LoadModuleJson(string hapPath)
        {
            using var archive = ZipFile.OpenRead(hapPath);
            var entry = archive.GetEntry("module.json");
            if (entry == null) return null;
            
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            return JsonSerializer.Deserialize<ModuleJson>(content);
        }

        public async Task CreateKeystore(string keystore, string storepass = "xiaobai123", string alias = "xiaobai", string cn = "xiaobai")
        {
            if (File.Exists(keystore)) return;
            
            var keytool = Path.Combine(JavaHome, "bin", "keytool.exe");
            var prams = $"-genkeypair -alias {alias} -keyalg EC -sigalg SHA256withECDSA -dname \"C=CN,O=HUAWEI,OU=HUAWEI IDE,CN={cn}\"  -keystore \"{keystore}\" -storetype pkcs12 -validity 9125 -storepass {storepass} -keypass {storepass}";
            
            await ExeCmd($"\"{keytool}\" {prams}");
        }

        public async Task<string> CreateCsr(string keystore, string csrpath, string alias = "xiaobai", string storepass = "xiaobai123")
        {
            if (File.Exists(csrpath)) return csrpath;

            var keytool = Path.Combine(JavaHome, "bin", "keytool.exe");
            var prams = $"-certreq -alias {alias} -keystore \"{keystore}\" -storetype pkcs12 -file \"{csrpath}\" -storepass {storepass}";
            
            await ExeCmd($"\"{keytool}\" {prams}");
            return csrpath;
        }

        public async Task UnpackHap(string hapFilePath, string outPath)
        {
             var mode = Path.GetExtension(hapFilePath).TrimStart('.').ToLower();
             var pathArg = $"--{mode}-path";
             var javaPath = Path.Combine(JavaHome, "bin", "java.exe");
             var cmd = $"\"{javaPath}\" -jar \"{UnpackJar}\" --mode {mode} {pathArg} \"{hapFilePath}\" --out-path \"{outPath}\" --force true";
             await ExeCmd(cmd);
        }
    }
}
