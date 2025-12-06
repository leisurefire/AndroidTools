using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HarmonyOSToolbox.Models.Harmony;
using HarmonyOSToolbox.Services.Common;
using System.Text.Json.Serialization;

namespace HarmonyOSToolbox.Services.Harmony
{
    public class HarmonyEcoService
    {
        private readonly HttpClientService _http;
        // private readonly HarmonyCoreService _core; 

        public string OAuth2Token { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;

        public HarmonyEcoService() // Removed core dependency for now to avoid circular issues if not strictly needed in constructor
        {
            _http = new HttpClientService();
        }

        private async Task<T> BaseRequest<T>(string url, object? data = null,
            HttpMethod? method = null, Dictionary<string, string>? headers = null)
        {
            method ??= HttpMethod.Post;
            var h = new Dictionary<string, string>
            {
                { "oauth2Token", OAuth2Token ?? "" },
                { "teamId", TeamId ?? "" },
                { "uid", UserId ?? "" }
            };
            if (headers != null)
                foreach (var kv in headers) h[kv.Key] = kv.Value;

            return await _http.SendAsync<T>(url, method, data, h);
        }

        public async Task<CertListResponse> GetCertList()
        {
            return await BaseRequest<CertListResponse>(
                "https://connect-api.cloud.huawei.com/api/cps/harmony-cert-manage/v1/cert/list",
                method: HttpMethod.Get);
        }

        public async Task<CreateCertResponse> CreateCert(string name, string csr, int type = 1)
        {
            return await BaseRequest<CreateCertResponse>(
                "https://connect-api.cloud.huawei.com/api/cps/harmony-cert-manage/v1/cert/add",
                new { csr = csr, certName = name, certType = type });
        }

        public async Task<DeviceListResponse> DeviceList()
        {
            return await BaseRequest<DeviceListResponse>(
                "https://connect-api.cloud.huawei.com/api/cps/device-manage/v1/device/list?start=1&pageSize=100&encodeFlag=0",
                method: HttpMethod.Get);
        }

        public async Task<CreateProfileResponse> CreateProfile(string name, string certId,
            string packageName, List<string> deviceIds, ModuleJson moduleJson)
        {
            var aclList = GetAcl(moduleJson);
            return await BaseRequest<CreateProfileResponse>(
                "https://connect-api.cloud.huawei.com/api/cps/provision-manage/v1/ide/test/provision/add",
                new
                {
                    provisionName = name,
                    aclPermissionList = aclList,
                    deviceList = deviceIds,
                    certList = new[] { certId },
                    packageName = packageName
                });
        }

        public async Task<string?> DownloadObj(string url)
        {
             var res = await BaseRequest<DownloadUrlResponse>(
                 "https://connect-api.cloud.huawei.com/api/amis/app-manage/v1/objects/url/reapply",
                 new { sourceUrls = url });
             return res?.Url;
        }
        
        private List<string> GetAcl(ModuleJson moduleJson)
        {
             // In a real implementation, extract from moduleJson.
             // For now, return default list from doc.
             return new List<string>
             {
                "ohos.permission.READ_AUDIO",
                "ohos.permission.WRITE_AUDIO",
                "ohos.permission.READ_IMAGEVIDEO",
                "ohos.permission.WRITE_IMAGEVIDEO",
                "ohos.permission.SHORT_TERM_WRITE_IMAGEVIDEO",
                "ohos.permission.READ_CONTACTS",
                "ohos.permission.WRITE_CONTACTS",
                "ohos.permission.SYSTEM_FLOAT_WINDOW",
                "ohos.permission.ACCESS_DDK_USB",
                "ohos.permission.ACCESS_DDK_HID",
                "ohos.permission.INPUT_MONITORING",
                "ohos.permission.INTERCEPT_INPUT_EVENT",
                "ohos.permission.READ_PASTEBOARD"
             };
        }
    }

    // Response Models
    public class CertListResponse
    {
        [JsonPropertyName("data")]
        public List<CertInfo> Data { get; set; } = new();
    }

    public class CreateCertResponse
    {
        [JsonPropertyName("data")]
        public CertInfo Data { get; set; } = new();
    }

    public class DeviceListResponse
    {
        [JsonPropertyName("data")]
        public DeviceListData Data { get; set; } = new();
    }
    
    public class DeviceListData
    {
         [JsonPropertyName("list")]
         public List<DeviceInfo> List { get; set; } = new();
    }
    
    public class DeviceInfo
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;
        
        [JsonPropertyName("udid")]
        public string Udid { get; set; } = string.Empty;
    }

    public class CreateProfileResponse
    {
        [JsonPropertyName("data")]
        public ProfileInfo Data { get; set; } = new();
    }

    public class DownloadUrlResponse
    {
         [JsonPropertyName("url")]
         public string Url { get; set; } = string.Empty;
    }
}
