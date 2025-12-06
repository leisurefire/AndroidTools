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
                var result = JsonSerializer.Deserialize<T>(content);
                if (result == null)
                {
                    throw new InvalidOperationException("Deserialization returned null");
                }
                return result;
            }
            catch (Exception ex)
            {
                // Log error here if logger is available
                Console.WriteLine($"HTTP Request failed: {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]> DownloadBytesAsync(string url)
        {
             return await _client.GetByteArrayAsync(url);
        }
    }
}
