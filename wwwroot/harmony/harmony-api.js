class HarmonyAPI {
  constructor() {
    this.requestIdCounter = 0;
    this.pendingRequests = new Map();
    this.accountUpdateCallbacks = [];

    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.addEventListener("message", (event) => {
        const message = event.data;
        try {
          const response =
            typeof message === "string" ? JSON.parse(message) : message;

          // 处理账号更新通知
          if (response.requestId === "harmonyAccountUpdated") {
            console.log("[HarmonyAPI] 收到账号更新通知:", response.result);
            this.accountUpdateCallbacks.forEach((cb) => cb(response.result));
            return;
          }

          if (
            response.requestId &&
            this.pendingRequests.has(response.requestId)
          ) {
            const { resolve, reject } = this.pendingRequests.get(
              response.requestId
            );
            this.pendingRequests.delete(response.requestId);
            if (response.result && response.result.success === false) {
              reject(new Error(response.result.error));
            } else {
              resolve(response.result);
            }
          }
        } catch (e) {
          console.error("Error parsing message:", e);
        }
      });
    }
  }

  // 注册账号更新回调
  onAccountUpdate(callback) {
    this.accountUpdateCallbacks.push(callback);
    // 返回取消注册函数
    return () => {
      const index = this.accountUpdateCallbacks.indexOf(callback);
      if (index > -1) {
        this.accountUpdateCallbacks.splice(index, 1);
      }
    };
  }

  send(action, data = null) {
    return new Promise((resolve, reject) => {
      if (!window.chrome || !window.chrome.webview) {
        reject(new Error("WebView2 not available"));
        return;
      }

      const requestId = `harmony_${Date.now()}_${this.requestIdCounter++}`;
      this.pendingRequests.set(requestId, { resolve, reject });

      const payload = {
        requestId,
        action,
        data,
      };
      window.chrome.webview.postMessage(JSON.stringify(payload));
    });
  }

  async uploadHap(file) {
    const buffer = await file.arrayBuffer();
    let binary = "";
    const bytes = new Uint8Array(buffer);
    const len = bytes.byteLength;
    for (let i = 0; i < len; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    const base64 = window.btoa(binary);
    return this.send("harmony_uploadHap", {
      buffer: base64,
      fileName: file.name,
    });
  }

  async openBigHap() {
    return this.send("harmony_openBigHap");
  }
  async getEnvInfo() {
    return this.send("harmony_getEnvInfo");
  }
  async getAccountInfo() {
    return this.send("harmony_getAccountInfo");
  }
  async checkAccount(info) {
    return this.send("harmony_checkAccount", info);
  }
  async loginHuawei(info) {
    return this.send("harmony_loginHuawei", info);
  }
  async getBuildInfo() {
    return this.send("harmony_getBuildInfo");
  }
  async startBuild(info) {
    return this.send("harmony_startBuild", info);
  }
  async applyCertAndProfile(info) {
    return this.send("harmony_applyCertAndProfile", info);
  }
  async getGitBranches(url) {
    return this.send("harmony_getGitBranches", { url });
  }
}

window.harmonyApi = new HarmonyAPI();
