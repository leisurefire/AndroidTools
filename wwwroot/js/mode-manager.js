// ===== 模式管理模块 =====
class ModeManager {
  constructor() {
    this.currentMode = "adb";
    this.btnAdb = null;
    this.btnHarmony = null;
  }

  init() {
    this.btnAdb = document.getElementById("modeAdb");
    this.btnHarmony = document.getElementById("modeHarmony");

    if (this.btnAdb && this.btnHarmony) {
      this.btnAdb.addEventListener("click", () => this.switchMode("adb"));
      this.btnHarmony.addEventListener("click", () =>
        this.switchMode("harmony")
      );
    }
  }

  switchMode(mode) {
    this.currentMode = mode;
    if (mode === "adb") {
      this.btnAdb.classList.add("active");
      this.btnHarmony.classList.remove("active");
      this.updateSidebar("adb");
      window.pageLoader.loadPage("function");
    } else {
      this.btnHarmony.classList.add("active");
      this.btnAdb.classList.remove("active");
      this.updateSidebar("harmony");
      window.pageLoader.loadPage("harmony-account");
    }
  }

  updateSidebar(mode) {
    const menu = document.querySelector(".nav-menu");
    if (!menu) return;

    menu.innerHTML = "";

    if (mode === "adb") {
      menu.innerHTML = `
            <li class='nav-item active' data-tab='function'>
              <span class='icon'><svg viewBox='0 0 24 24'><path d='M22.7 19l-9.1-9.1c.9-2.3.4-5-1.5-6.9-2-2-5-2.4-7.4-1.3L9 6 6 9 1.6 4.7C.4 7.1.9 10.1 2.9 12.1c1.9 1.9 4.6 2.4 6.9 1.5l9.1 9.1c.4.4 1 .4 1.4 0l2.3-2.3c.5-.4.5-1.1.1-1.4z'/></svg></span>
              <span>主菜单</span>
            </li>
            <li class='nav-item' data-tab='custom'>
              <span class='icon'><svg viewBox='0 0 24 24'><path d='M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58a.49.49 0 00.12-.61l-1.92-3.32a.488.488 0 00-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54a.484.484 0 00-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58a.49.49 0 00-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.58 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z'/></svg></span>
              <span>应用管理</span>
            </li>
            <li class='nav-item' data-tab='main'>
               <span class='icon'><svg viewBox='0 0 24 24'><path d='M12 3L2 12h3v8h6v-6h2v6h6v-8h3L12 3z'/></svg></span>
               <span>快捷卸载</span>
            </li>
            <li class='nav-item' data-tab='animation'>
               <span class='icon'><svg viewBox='0 0 24 24'><path d='M22 8l-4-4h-9l-4 4H2v12h20V8zM8 18c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm12-3h-8v-2h8v2zm0-4h-8V9h8v2z'/></svg></span>
               <span>动画模式</span>
            </li>`;
    } else {
      menu.innerHTML = `
            <li class='nav-item active' data-tab='harmony-account'>
              <span class='icon'><svg viewBox='0 0 24 24'><path d='M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z' fill='currentColor'/></svg></span>
              <span>账号管理</span>
            </li>
            <li class='nav-item' data-tab='harmony-install'>
              <span class='icon'><svg viewBox='0 0 24 24'><path d='M9 16h6v-6h4l-7-7-7 7h4zm-4 2h14v2H5z' fill='currentColor'/></svg></span>
              <span>应用安装</span>
            </li>`;
    }

    this.initTabs();
  }

  initTabs() {
    const navItems = document.querySelectorAll(".nav-item");

    navItems.forEach((item) => {
      item.addEventListener("click", () => {
        const tabId = item.dataset.tab;

        // 更新导航状态
        navItems.forEach((nav) => nav.classList.remove("active"));
        item.classList.add("active");

        // 加载对应页面
        window.pageLoader.loadPage(tabId);

        window.logger.log(`切换到 ${item.textContent.trim()} 模块`);
      });
    });
  }
}

// 导出全局modeManager实例
window.modeManager = new ModeManager();
