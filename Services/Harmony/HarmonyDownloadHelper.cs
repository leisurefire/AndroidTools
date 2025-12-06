using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace HarmonyOSToolbox.Services.Harmony
{
    public class HarmonyDownloadHelper
    {
        public string ConfigDir { get; private set; }
        public string CodeDir { get; private set; }
        public string HapDir { get; private set; }
        public string SignedDir { get; private set; }

        private readonly HttpClient _httpClient;

        public HarmonyDownloadHelper()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            ConfigDir = Path.Combine(userProfile, ".autoPublisher", "config");
            CodeDir = Path.Combine(userProfile, ".autoPublisher", "code");
            HapDir = Path.Combine(userProfile, ".autoPublisher", "haps");
            SignedDir = Path.Combine(userProfile, ".autoPublisher", "signeds");

            Directory.CreateDirectory(ConfigDir);
            Directory.CreateDirectory(CodeDir);
            Directory.CreateDirectory(HapDir);
            Directory.CreateDirectory(SignedDir);

            _httpClient = new HttpClient();
        }

        public async Task<string> DownloadFile(string url, string fileName)
        {
            var filePath = Path.Combine(ConfigDir, fileName);
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(filePath, bytes);
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download failed: {ex.Message}");
                if (File.Exists(filePath)) File.Delete(filePath);
                throw;
            }
        }

        public void WriteObjToFile(string filename, object obj, string? basePath = null)
        {
            var filePath = Path.Combine(basePath ?? ConfigDir, filename);
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public T ReadFileToObj<T>(string filename, string? basePath = null) where T : new()
        {
            var filePath = Path.Combine(basePath ?? ConfigDir, filename);
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var result = JsonSerializer.Deserialize<T>(json);
                    return result ?? new T();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadFileToObj error: {ex.Message}");
            }
            return new T();
        }

        public string ReadPng(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var bytes = File.ReadAllBytes(filePath);
                    return Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadPng error: {ex.Message}");
            }
            return string.Empty;
        }

        public void CloneGit(string repoUrl, string branch = "master")
        {
            var repoName = Path.GetFileNameWithoutExtension(repoUrl);
            var destination = Path.Combine(CodeDir, repoName);
            
            if (Directory.Exists(destination))
            {
                return; 
            }

            Repository.Clone(repoUrl, destination, new CloneOptions { BranchName = branch });
            
            using (var repo = new Repository(destination))
            {
                foreach (var submodule in repo.Submodules)
                {
                    repo.Submodules.Update(submodule.Name, new SubmoduleUpdateOptions { Init = true });
                }
            }
        }
    }
}
