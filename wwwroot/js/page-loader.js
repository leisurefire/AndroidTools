// ===== 页面加载模块 =====
class PageLoader {
  constructor() {
    this.pages = {
      function: "pages/function.html",
      custom: "pages/custom.html",
      main: "pages/main.html",
      animation: "pages/animation.html",
      help: "pages/help.html",
      "harmony-account": "harmony/pages/account.html",
      "harmony-install": "harmony/pages/install.html",
      "harmony-dependencies": "harmony/pages/dependencies.html",
    };
    this.currentPage = null;
    this.contentArea = null;
  }

  async loadPage(pageId) {
    if (!this.contentArea) {
      this.contentArea = document.getElementById("contentArea");
    }

    const pageUrl = this.pages[pageId];
    if (!pageUrl) {
      console.error(`页面 ${pageId} 不存在`);
      return;
    }

    try {
      // Use C# API to load page content (fix CORS issue)
      const result = await window.api.call("loadPage", pageId);

      if (!result.success) {
        throw new Error(result.error || "Failed to load page");
      }

      // Extract scripts
      const parser = new DOMParser();
      const doc = parser.parseFromString(result.content, "text/html");
      const scripts = doc.querySelectorAll("script");
      
      // Remove scripts from the content to be inserted (optional, but cleaner)
      // We can just use the body innerHTML from the parser which might lack scripts if we remove them, 
      // or just use the raw string regex to strip them if we want to be precise.
      // Simpler approach: Insert raw HTML, then manually run scripts.
      // Note: innerHTML inserts scripts but marks them as not-executable.
      
      this.contentArea.innerHTML = result.content;
      this.currentPage = pageId;

      // Execute scripts
      Array.from(this.contentArea.querySelectorAll("script")).forEach(
        (oldScript) => {
          const newScript = document.createElement("script");
          Array.from(oldScript.attributes).forEach((attr) =>
            newScript.setAttribute(attr.name, attr.value)
          );
          newScript.appendChild(document.createTextNode(oldScript.innerHTML));
          oldScript.parentNode.replaceChild(newScript, oldScript);
        }
      );

      // Add 'active' class to loaded content to make it visible
      const loadedSection = this.contentArea.querySelector(".tab-content");
      if (loadedSection) {
        loadedSection.classList.add("active");
      }

      // 触发页面初始化事件
      this.initCurrentPage();

      console.log(`[PageLoader] 页面 ${pageId} 加载成功`);
    } catch (error) {
      console.error(`[PageLoader] 加载页面 ${pageId} 失败:`, error);
      this.contentArea.innerHTML = `<div class="error-message">页面加载失败: ${error.message}</div>`;
    }
  }

  initCurrentPage() {
    // 根据当前页面重新绑定事件
    if (!window.modules) return;

    switch (this.currentPage) {
      case "function":
        if (window.modules.functionMode) {
          window.modules.functionMode.init();
        }
        break;
      case "custom":
        if (window.modules.customMode) {
          window.modules.customMode.init();
        }
        break;
      case "main":
        if (window.modules.mainMenu) {
          window.modules.mainMenu.init();
        }
        break;
      case "animation":
        if (window.modules.animationMode) {
          window.modules.animationMode.init();
        }
        break;
      case "help":
        // 帮助页面无需额外初始化
        break;
    }
  }
}

// 导出全局pageLoader实例
window.pageLoader = new PageLoader();
