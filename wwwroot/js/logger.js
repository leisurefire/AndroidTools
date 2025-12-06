// ===== 日志管理模块 =====
class LogManager {
  constructor() {
    this.logContent = null;
  }

  init() {
    this.logContent = document.getElementById("logContent");
  }

  log(message, type = "info") {
    if (!this.logContent) return;
    
    const time = new Date().toLocaleTimeString();
    const entry = document.createElement("div");
    entry.className = `log-entry ${type}`;
    entry.innerHTML = `<span class="log-time">[${time}]</span>${message}`;
    this.logContent.appendChild(entry);
    this.logContent.scrollTop = this.logContent.scrollHeight;
  }

  success(message) {
    this.log(message, "success");
  }

  error(message) {
    this.log(message, "error");
  }

  clear() {
    if (this.logContent) {
      this.logContent.innerHTML = "";
    }
  }
}

// 导出全局logger实例
window.logger = new LogManager();
