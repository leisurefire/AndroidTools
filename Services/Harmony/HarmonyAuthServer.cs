using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyOSToolbox.Services.Harmony
{
    /// <summary>
    /// 本地 HTTP 服务器，用于接收华为开发者认证回调
    /// 参考: auto-installer/electron/main.js
    /// </summary>
    public class HarmonyAuthServer : IDisposable
    {
        private HttpListener? _listener;
        private HarmonyEcoService _ecoService;
        public int Port { get; private set; }
        public event EventHandler<UserInfo>? OnAuthSuccess;
        public event EventHandler<string>? OnAuthError;

        public HarmonyAuthServer(HarmonyEcoService ecoService)
        {
            _ecoService = ecoService;
        }

        /// <summary>
        /// 启动本地HTTP服务器
        /// </summary>
        public Task<int> StartAsync()
        {
            try
            {
                _listener = new HttpListener();
                
                // 尝试随机端口（0 表示自动选择）
                Port = GetRandomPort();
                var prefix = $"http://localhost:{Port}/";
                _listener.Prefixes.Add(prefix);
                
                _listener.Start();
                Console.WriteLine($"[华为认证服务器] 已启动在端口: {Port}");

                // 开始监听请求
                _ = Task.Run(async () => await ListenAsync());

                return Task.FromResult(Port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[华为认证服务器] 启动失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取随机可用端口
        /// </summary>
        private int GetRandomPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// 监听HTTP请求
        /// </summary>
        private async Task ListenAsync()
        {
            try
            {
                while (_listener != null && _listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
            }
            catch (HttpListenerException)
            {
                // 服务器已停止
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[华为认证服务器] 监听错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理HTTP请求
        /// </summary>
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                Console.WriteLine($"[华为认证服务器] 收到请求: {request.HttpMethod} {request.Url}");

                // 只处理 POST /callback
                if (request.HttpMethod == "POST" && request.Url?.LocalPath == "/callback")
                {
                    // 读取 POST 数据
                    string body;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        body = await reader.ReadToEndAsync();
                    }

                    Console.WriteLine($"[华为认证服务器] 收到 tempToken 数据");

                    // 使用 tempToken 换取用户信息
                    try
                    {
                        var userInfo = await _ecoService.GetTokenByTempToken(body);
                        
                        // 触发成功事件
                        OnAuthSuccess?.Invoke(this, userInfo);

                        // 返回成功响应
                        var responseString = "登录成功，请返回应用";
                        var buffer = Encoding.UTF8.GetBytes(responseString);
                        response.ContentType = "text/plain; charset=utf-8";
                        response.ContentLength64 = buffer.Length;
                        response.StatusCode = 200;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[华为认证服务器] Token 换取失败: {ex.Message}");
                        OnAuthError?.Invoke(this, ex.Message);

                        var responseString = $"认证失败: {ex.Message}";
                        var buffer = Encoding.UTF8.GetBytes(responseString);
                        response.ContentType = "text/plain; charset=utf-8";
                        response.ContentLength64 = buffer.Length;
                        response.StatusCode = 500;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                }
                else
                {
                    // 其他请求返回 404
                    response.StatusCode = 404;
                    var responseString = "Not Found";
                    var buffer = Encoding.UTF8.GetBytes(responseString);
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[华为认证服务器] 处理请求失败: {ex.Message}");
            }
            finally
            {
                response.Close();
            }
        }

        /// <summary>
        /// 打开华为认证页面
        /// </summary>
        public void OpenAuthPage()
        {
            var url = $"https://cn.devecostudio.huawei.com/console/DevEcoIDE/apply?port={Port}&appid=1007&code=20698961dd4f420c8b44f49010c6f0cc";
            Console.WriteLine($"[华为认证] 打开认证页面: {url}");
            
            try
            {
                // 使用默认浏览器打开
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[华为认证] 打开浏览器失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
                Console.WriteLine("[华为认证服务器] 已停止");
            }
        }

        public void Dispose()
        {
            Stop();
            _listener = null;
        }
    }
}
