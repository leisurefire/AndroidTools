class HarmonyAPI {
    constructor() {
        this.requestIdCounter = 0;
        this.pendingRequests = new Map();
        
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.addEventListener('message', event => {
                const message = event.data;
                try {
                    const response = typeof message === 'string' ? JSON.parse(message) : message;
                    if (response.requestId && this.pendingRequests.has(response.requestId)) {
                        const { resolve, reject } = this.pendingRequests.get(response.requestId);
                        this.pendingRequests.delete(response.requestId);
                        if (response.result && response.result.success === false) {
                            reject(new Error(response.result.error));
                        } else {
                            resolve(response.result);
                        }
                    }
                } catch (e) {
                    console.error('Error parsing message:', e);
                }
            });
        }
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
                data
            };
            window.chrome.webview.postMessage(JSON.stringify(payload));
        });
    }

    async uploadHap(file) {
        const buffer = await file.arrayBuffer();
        let binary = '';
        const bytes = new Uint8Array(buffer);
        const len = bytes.byteLength;
        for (let i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        const base64 = window.btoa(binary);
        return this.send('harmony_uploadHap', { buffer: base64, fileName: file.name });
    }
    
    async openBigHap() { return this.send('harmony_openBigHap'); }
    async getEnvInfo() { return this.send('harmony_getEnvInfo'); }
    async getAccountInfo() { return this.send('harmony_getAccountInfo'); }
    async checkAccount(info) { return this.send('harmony_checkAccount', info); }
    async getBuildInfo() { return this.send('harmony_getBuildInfo'); }
    async startBuild(info) { return this.send('harmony_startBuild', info); }
    async getGitBranches(url) { return this.send('harmony_getGitBranches', { url }); }
}

window.harmonyApi = new HarmonyAPI();
