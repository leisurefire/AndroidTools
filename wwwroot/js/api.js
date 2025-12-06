// ===== API通信模块 =====
class WebView2API {
  constructor() {
    this.requestId = 0;
    this.pendingRequests = new Map();

    // 监听来自C#的消息
    window.chrome.webview.addEventListener("message", (event) => {
      const response = JSON.parse(event.data);

      // 处理日志消息
      if (response.requestId === "log") {
        if (window.logger) {
          window.logger.log(response.result.message, response.result.type || "info");
        }
        return;
      }

      // 处理进度消息
      if (response.requestId === "progress") {
        // 可以在这里处理进度条更新，目前仅在更新ADB时使用
        return;
      }

      // 处理窗口状态消息
      if (response.requestId === "windowState") {
        if (typeof updateMaxButton === "function") {
          updateMaxButton(response.result.state);
        }
        return;
      }

      const resolve = this.pendingRequests.get(response.requestId);
      if (resolve) {
        resolve(response.result);
        this.pendingRequests.delete(response.requestId);
      }
    });
  }

  async call(action, data) {
    return new Promise((resolve) => {
      const requestId = `req_${++this.requestId}`;
      this.pendingRequests.set(requestId, resolve);

      const message = {
        requestId,
        action,
        data,
      };

      window.chrome.webview.postMessage(JSON.stringify(message));
    });
  }
}

// 导出全局API实例
window.api = new WebView2API();
