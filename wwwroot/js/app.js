// ===== 主应用入口文件 =====
// 所有模块已通过独立文件加载，这里只负责初始化和全局事件绑定

// 窗口控制按钮图标更新函数
function updateMaxButton(state) {
  const maxBtn = document.getElementById("maxBtn");
  if (!maxBtn) return;

  if (state === "maximized") {
    maxBtn.title = "向下还原";
    maxBtn.innerHTML = `
            <svg width="10" height="10" viewBox="0 0 10 10">
                <path d="M3.5,3.5 L3.5,1.5 L9.5,1.5 L9.5,7.5 L7.5,7.5 M1.5,3.5 L7.5,3.5 L7.5,9.5 L1.5,9.5 L1.5,3.5 Z" stroke="currentColor" stroke-width="1" fill="none"/>
            </svg>`;
  } else {
    maxBtn.title = "最大化";
    maxBtn.innerHTML = `
            <svg width="10" height="10" viewBox="0 0 10 10">
                <rect x="1.5" y="1.5" width="7" height="7" stroke="currentColor" stroke-width="1" fill="none"/>
            </svg>`;
  }
}

// 全局导航对象
window.app = {
  navigate: (page) => window.pageLoader.loadPage(page),
};

// ===== 初始化 =====
document.addEventListener("DOMContentLoaded", () => {
  // 初始化所有管理器
  window.logger.init();
  window.deviceManager.init();
  window.modeManager.init();

  window.logger.log("鸿蒙工具箱已启动");

  // 禁用所有右键菜单
  document.addEventListener("contextmenu", (e) => {
    e.preventDefault();
    return false;
  });

  // 初始化标签页切换
  window.modeManager.initTabs();

  // 加载默认页面（主菜单）
  window.pageLoader.loadPage("function");

  // 设备刷新按钮
  const refreshDeviceBtn = document.getElementById("refreshDeviceBtn");
  if (refreshDeviceBtn) {
    refreshDeviceBtn.addEventListener("click", () =>
      window.deviceManager.checkDevice()
    );
  }

  // 清空日志按钮
  const clearLogBtn = document.getElementById("clearLogBtn");
  if (clearLogBtn) {
    clearLogBtn.addEventListener("click", () => {
      window.logger.clear();
      window.logger.log("日志已清空");
    });
  }

  // 初始化窗口控制
  console.log("Window controls initialized (Native)");
});
