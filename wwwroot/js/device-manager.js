// ===== 设备状态管理模块 =====
class DeviceManager {
  constructor() {
    this.statusIndicator = null;
    this.statusText = null;
  }

  init() {
    this.statusIndicator = document.getElementById("statusIndicator");
    this.statusText = document.getElementById("statusText");
  }

  async checkDevice() {
    try {
      const result = await window.api.call("checkDevice");

      if (this.statusIndicator && this.statusText) {
        if (result.connected) {
          this.statusIndicator.classList.add("connected");
          this.statusText.textContent = `已连接 ${result.deviceCount} 个设备`;
          window.logger.success(result.message);
        } else {
          this.statusIndicator.classList.remove("connected");
          this.statusText.textContent = "未连接设备";
          window.logger.error("未检测到设备，请检查USB连接");
        }
      }

      return result.connected;
    } catch (error) {
      window.logger.error(`检查设备失败: ${error.message}`);
      return false;
    }
  }
}

// 导出全局deviceManager实例
window.deviceManager = new DeviceManager();
