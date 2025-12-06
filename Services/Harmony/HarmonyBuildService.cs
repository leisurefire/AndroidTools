using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using HarmonyOSToolbox.Models.Harmony;

namespace HarmonyOSToolbox.Services.Harmony
{
    public class HarmonyBuildService
    {
        private readonly HarmonyCoreService _core;
        private EcoConfig _ecoConfig = new();

        public HarmonyBuildService(HarmonyCoreService core)
        {
            _core = core;
        }

        public async Task CheckEcoAccount(CommonInfo commonInfo)
        {
            _core.CommonInfo = commonInfo;
            
            // Try load auth info
            try
            {
                var authInfo = _core.Dh.ReadFileToObj<dynamic>("ds-authInfo.json");
                if (authInfo != null)
                {
                    // In a real scenario we should map dynamic properties or use specific class
                    // For now assuming keys exist if authInfo is not null
                    // _core.Eco.OAuth2Token = ... 
                    // But dynamic is tricky with System.Text.Json.
                    // Let's rely on specific class later or assume re-login if fails.
                }
                
                // Load EcoConfig
                _ecoConfig = _core.Dh.ReadFileToObj<EcoConfig>("eco_config.json");
                if (_ecoConfig == null) _ecoConfig = new EcoConfig();

                await UpdateStep(_core.AccountInfo.Steps, "Checking Account", true);
                // Real check would involve calling an API to verify token
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CheckAccount error: {ex.Message}");
            }
        }

        public async Task StartBuild(CommonInfo commonInfo)
        {
            _core.CommonInfo = commonInfo;
            
            // 1. Create Cert if needed
            // 2. Create Profile if needed
            // 3. Sign
            // 4. Install
            
            try 
            {
                await SignAndInstall();
            }
            catch (Exception ex)
            {
                // Handle error, update step status
                Console.WriteLine($"Build failed: {ex.Message}");
                throw; 
            }
        }

        public async Task SignAndInstall()
        {
            // Use default debug cert/profile for now or logic from JS
            // Assuming _ecoConfig has valid paths or we create them.
            
            // Simplified logic for migration prototype:
            // 1. Check if Cert/Profile exists locally
            // 2. If not, call Eco service (omitted for brevity in this step, needs full implementation)
            // 3. Sign
            
            var signConfig = new SignConfig
            {
                KeystoreFile = Path.Combine(_core.Dh.ConfigDir, _ecoConfig.Keystore ?? "xiaobai.p12"),
                KeystorePwd = _ecoConfig.Storepass ?? "xiaobai123",
                KeyAlias = _ecoConfig.KeyAlias ?? "xiaobai",
                CertFile = _ecoConfig.DebugCert?.Path ?? string.Empty,
                ProfileFile = _ecoConfig.DebugProfile?.Path ?? string.Empty,
                InFile = _core.CommonInfo.HapPath,
                OutFile = Path.Combine(_core.Dh.SignedDir, "signed.hap")
            };

            if (string.IsNullOrEmpty(signConfig.CertFile) || !File.Exists(signConfig.CertFile))
            {
                 // In real app, trigger creation flow
                 throw new Exception("Certificate not found. Please configure in settings or login.");
            }

            await _core.Cmd.SignHap(signConfig);
            
            await _core.Cmd.SendAndInstall(signConfig.OutFile, _core.CommonInfo.DeviceIp);
        }

        private async Task UpdateStep(List<StepInfo> steps, string name, bool finish)
        {
            var step = steps.FirstOrDefault(s => s.Name == name);
            if (step != null)
            {
                step.Finish = finish;
                step.Loading = !finish;
            }
            await Task.CompletedTask;
        }
    }
}
