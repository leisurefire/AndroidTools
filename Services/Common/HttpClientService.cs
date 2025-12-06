using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HarmonyOSToolbox.Services.Common
{
    public class HttpClientService
    {
        private static readonly HttpClient _client = new HttpClient();

        public async Task<T> SendAsync<T>(string url, HttpMethod method, object? data = null, Dictionary<string, string>? headers = null)
        {
            var request = new HttpRequestMessage(method, url);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (!string.IsNullOrEmpty(header.Value))
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            if (data != null && method != HttpMethod.Get)
            {
                var json = JsonSerializer.Serialize(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                // 打印响应内容用于调试（仅打印前200个字符）
                var preview = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                Console.WriteLine($"[HTTP] Response preview: {preview}");
                
                // 特殊处理：当返回类型是 string 时，直接返回内容，不进行 JSON 反序列化
                // 这是因为华为 API 某些端点返回纯文本 JWT Token 而不是 JSON
                if (typeof(T) == typeof(string))
                {
                    Console.WriteLine($"[HTTP] Returning raw string response (length: {content.Length})");
                    return (T)(object)content;
                }
                
                // 尝试 JSON 反序列化
                try
                {
                    var result = JsonSerializer.Deserialize<T>(content);
                    if (result == null)
                    {
                        throw new InvalidOperationException("Deserialization returned null");
                    }
                    return result;
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"[HTTP] JSON 反序列化失败: {jsonEx.Message}");
                    Console.WriteLine($"[HTTP] 响应内容: {content}");
                    throw new InvalidOperationException($"无法解析响应 JSON: {jsonEx.Message}", jsonEx);
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[HTTP] 请求失败 ({url}): {httpEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HTTP] 未知错误 ({url}): {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]> DownloadBytesAsync(string url)
        {
             return await _client.GetByteArrayAsync(url);
        }
    }
}
