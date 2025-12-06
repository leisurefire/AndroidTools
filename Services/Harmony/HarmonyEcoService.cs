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

        public string OAuth2Token { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;

        public HarmonyEcoService()
        {
            _http = new HttpClientService();
        }

        /// <summary>
        /// 使用 tempToken 换取 JWT Token 和用户信息
        /// 参考: auto-installer/core/ecoService.js
        /// </summary>
        public async Task<UserInfo> GetTokenByTempToken(string tempTokenData)
        {
            try
            {
                Console.WriteLine("[华为认证] 开始Token换取流程...");

                // 1. 提取 tempToken
                var tempToken = ExtractTempToken(tempTokenData);
                if (string.IsNullOrEmpty(tempToken))
                {
                    throw new Exception("无法从回调数据中提取 tempToken");
                }
                Console.WriteLine("[华为认证] tempToken已提取");

                // 2. 验证 tempToken，获取 JWT Token
                var jwtTokenUrl = $"https://cn.devecostudio.huawei.com/authrouter/auth/api/temptoken/check?site=CN&tempToken={tempToken}&appid=1007&version=0.0.0";
                Console.WriteLine("[华为认证] 正在验证 tempToken...");
                var jwtToken = await _http.SendAsync<string>(jwtTokenUrl, HttpMethod.Get);
                Console.WriteLine("[华为认证] JWT Token 获取成功");

                // 3. 使用 JWT Token 获取用户信息
                var userInfoUrl = "https://cn.devecostudio.huawei.com/authrouter/auth/api/jwToken/check";
                Console.WriteLine("[华为认证] 正在获取用户信息...");
                var headers = new Dictionary<string, string>
                {
                    { "jwtToken", jwtToken },
                    { "refresh", "false" }
                };
                var userInfoResponse = await _http.SendAsync<UserInfoResponse>(userInfoUrl, HttpMethod.Get, null, headers);

                if (userInfoResponse?.UserInfo == null)
                {
                    throw new Exception("获取用户信息失败");
                }

                var userInfo = userInfoResponse.UserInfo;
                Console.WriteLine($"[华为认证] 登录成功！用户: {userInfo.NickName ?? userInfo.UserId}");

                // 4. 初始化认证信息
                InitCookie(userInfo);

                return userInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[华为认证] Token换取失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 初始化认证信息
        /// </summary>
        public void InitCookie(UserInfo authInfo)
        {
            OAuth2Token = authInfo.AccessToken ?? string.Empty;
            UserId = authInfo.UserId ?? string.Empty;
            TeamId = authInfo.UserId ?? string.Empty;
            NickName = authInfo.NickName ?? string.Empty;
            Console.WriteLine($"[华为认证] 认证信息已初始化 - 用户: {NickName}, UserID: {UserId}");
        }

        /// <summary>
        /// 从回调数据中提取 tempToken
        /// </summary>
        private string ExtractTempToken(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            var parts = data.Split('&');
            foreach (var part in parts)
            {
                if (part.StartsWith("tempToken="))
                {
                    return part.Substring("tempToken=".Length);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取用户团队列表
        /// </summary>
        public async Task<UserTeamListResponse> GetUserTeamList()
        {
            return await BaseRequest<UserTeamListResponse>(
                "https://connect-api.cloud.huawei.com/api/ups/user-permission-service/v1/user-team-list",
                method: HttpMethod.Get);
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
    public class UserInfo
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }
        
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
        
        [JsonPropertyName("nickName")]
        public string? NickName { get; set; }
    }

    public class UserInfoResponse
    {
        [JsonPropertyName("userInfo")]
        public UserInfo? UserInfo { get; set; }
    }

    public class UserTeamListResponse
    {
        [JsonPropertyName("teams")]
        public List<TeamInfo> Teams { get; set; } = new();
    }

    public class TeamInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("userType")]
        public int UserType { get; set; }
    }

    public class CertListResponse
    {
        [JsonPropertyName("certList")]
        public List<CertInfo> CertList { get; set; } = new();
    }

    public class CertInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("certName")]
        public string CertName { get; set; } = string.Empty;
        
        [JsonPropertyName("certType")]
        public int CertType { get; set; }
        
        [JsonPropertyName("certObjectId")]
        public string CertObjectId { get; set; } = string.Empty;
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
