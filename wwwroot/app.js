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
        logger.log(response.result.message, response.result.type || "info");
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

const api = new WebView2API();

// ===== 页面加载模块 =====
class PageLoader {
  constructor() {
    this.pages = {
      function: "pages/function.html",
      custom: "pages/custom.html",
      main: "pages/main.html",
      animation: "pages/animation.html",
      help: "pages/help.html",
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
      const result = await api.call("loadPage", pageId);

      if (!result.success) {
        throw new Error(result.error || "Failed to load page");
      }

      this.contentArea.innerHTML = result.content;
      this.currentPage = pageId;

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
    switch (this.currentPage) {
      case "function":
        initFunctionMode();
        break;
      case "custom":
        initCustomMode();
        break;
      case "main":
        initMainMenu();
        break;
      case "animation":
        initAnimationMode();
        break;
      case "help":
        // 帮助页面无需额外初始化
        break;
    }
  }
}

const pageLoader = new PageLoader();

// ===== 日志管理 =====
class LogManager {
  constructor() {
    this.logContent = document.getElementById("logContent");
  }

  log(message, type = "info") {
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
    this.logContent.innerHTML = "";
  }
}

const logger = new LogManager();

// ===== 设备状态管理 =====
async function checkDevice() {
  try {
    const result = await api.call("checkDevice");
    const statusIndicator = document.getElementById("statusIndicator");
    const statusText = document.getElementById("statusText");

    if (result.connected) {
      statusIndicator.classList.add("connected");
      statusText.textContent = `已连接 ${result.deviceCount} 个设备`;
      logger.success(result.message);
    } else {
      statusIndicator.classList.remove("connected");
      statusText.textContent = "未连接设备";
      logger.error("未检测到设备，请检查USB连接");
    }

    return result.connected;
  } catch (error) {
    logger.error(`检查设备失败: ${error.message}`);
    return false;
  }
}

// ===== 标签页切换 =====
function initTabs() {
  const navItems = document.querySelectorAll(".nav-item");

  navItems.forEach((item) => {
    item.addEventListener("click", () => {
      const tabId = item.dataset.tab;

      // 更新导航状态
      navItems.forEach((nav) => nav.classList.remove("active"));
      item.classList.add("active");

      // 加载对应页面
      pageLoader.loadPage(tabId);

      logger.log(`切换到 ${item.textContent.trim()} 模块`);
    });
  });
}

// ===== 定制模式功能 =====
function initCustomMode() {
  const cards = document.querySelectorAll("#main .function-card");

  cards.forEach((card) => {
    const packageName = card.dataset.package;
    const uninstallBtn = card.querySelector(".uninstall-btn");
    const installBtn = card.querySelector(".install-btn");
    const appName = card.querySelector("h3").textContent;

    if (uninstallBtn) {
      uninstallBtn.addEventListener("click", async () => {
        if (!(await checkDevice())) return;

        logger.log(`正在卸载 ${appName}...`);
        uninstallBtn.disabled = true;

        try {
          const result = await api.call("uninstallApp", packageName);
          if (result.success) {
            logger.success(result.message);
          } else {
            logger.error(result.message);
          }
        } catch (error) {
          logger.error(`卸载失败: ${error.message}`);
        } finally {
          uninstallBtn.disabled = false;
        }
      });
    }

    if (installBtn) {
      installBtn.addEventListener("click", async () => {
        if (!(await checkDevice())) return;

        logger.log(`正在安装 ${appName}...`);
        installBtn.disabled = true;

        try {
          const result = await api.call("installApp", packageName);
          if (result.success) {
            logger.success(result.message);
          } else {
            logger.error(result.message);
          }
        } catch (error) {
          logger.error(`安装失败: ${error.message}`);
        } finally {
          installBtn.disabled = false;
        }
      });
    }
  });

  // 应用管理页面的查看软件按钮
  const customOutput = document.getElementById("customOutput");

  document
    .getElementById("getUserPackagesBtn2")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;
      logger.log("正在获取用户软件列表...");
      customOutput.textContent = "加载中...";

      try {
        const result = await api.call("listPackages", "user");
        if (result.success) {
          customOutput.textContent =
            `用户软件包（共${result.count}个）:\n\n` +
            result.packages.join("\n");
          logger.success(`获取成功，共 ${result.count} 个用户应用`);
        } else {
          customOutput.textContent = "获取失败";
          logger.error("获取用户软件列表失败");
        }
      } catch (error) {
        customOutput.textContent = "获取失败";
        logger.error(`错误: ${error.message}`);
      }
    });

  document
    .getElementById("getSystemPackagesBtn2")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;
      logger.log("正在获取系统软件列表...");
      customOutput.textContent = "加载中...";

      try {
        const result = await api.call("listPackages", "system");
        if (result.success) {
          customOutput.textContent =
            `系统软件包（共${result.count}个）:\n\n` +
            result.packages.join("\n");
          logger.success(`获取成功，共 ${result.count} 个系统应用`);
        } else {
          customOutput.textContent = "获取失败";
          logger.error("获取系统软件列表失败");
        }
      } catch (error) {
        customOutput.textContent = "获取失败";
        logger.error(`错误: ${error.message}`);
      }
    });

  document
    .getElementById("getAllPackagesBtn2")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;
      logger.log("正在获取所有软件列表...");
      customOutput.textContent = "加载中...";

      try {
        const result = await api.call("listPackages", "all");
        if (result.success) {
          customOutput.textContent =
            `所有软件包（共${result.count}个）:\n\n` +
            result.packages.join("\n");
          logger.success(`获取成功，共 ${result.count} 个应用`);
        } else {
          customOutput.textContent = "获取失败";
          logger.error("获取软件列表失败");
        }
      } catch (error) {
        customOutput.textContent = "获取失败";
        logger.error(`错误: ${error.message}`);
      }
    });

  // 应用管理页面的手动卸载/安装
  document
    .getElementById("manualUninstallBtn2")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;
      const packageName = document
        .getElementById("manualPackage2")
        .value.trim();
      if (!packageName) {
        alert("请输入包名");
        return;
      }
      logger.log(`正在卸载 ${packageName}...`);

      try {
        const result = await api.call("uninstallApp", packageName);
        if (result.success) {
          logger.success(result.message);
        } else {
          logger.error(result.message);
        }
      } catch (error) {
        logger.error(`卸载失败: ${error.message}`);
      }
    });

  document
    .getElementById("manualInstallBtn2")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;
      const packageName = document
        .getElementById("manualPackage2")
        .value.trim();
      if (!packageName) {
        alert("请输入包名");
        return;
      }
      logger.log(`正在安装 ${packageName}...`);

      try {
        const result = await api.call("installApp", packageName);
        if (result.success) {
          logger.success(result.message);
        } else {
          logger.error(result.message);
        }
      } catch (error) {
        logger.error(`安装失败: ${error.message}`);
      }
    });
}

// ===== 主菜单功能（快捷卸载页面） =====
function initMainMenu() {
  const cards = document.querySelectorAll("#main .function-card");

  cards.forEach((card) => {
    const packageName = card.dataset.package;
    const uninstallBtn = card.querySelector(".uninstall-btn");
    const installBtn = card.querySelector(".install-btn");
    const appName = card.querySelector("h3").textContent;

    if (uninstallBtn) {
      uninstallBtn.addEventListener("click", async () => {
        if (!(await checkDevice())) return;

        logger.log(`正在卸载 ${appName}...`);
        uninstallBtn.disabled = true;

        try {
          const result = await api.call("uninstallApp", packageName);
          if (result.success) {
            logger.success(result.message);
          } else {
            logger.error(result.message);
          }
        } catch (error) {
          logger.error(`卸载失败: ${error.message}`);
        } finally {
          uninstallBtn.disabled = false;
        }
      });
    }

    if (installBtn) {
      installBtn.addEventListener("click", async () => {
        if (!(await checkDevice())) return;

        logger.log(`正在安装 ${appName}...`);
        installBtn.disabled = true;

        try {
          const result = await api.call("installApp", packageName);
          if (result.success) {
            logger.success(result.message);
          } else {
            logger.error(result.message);
          }
        } catch (error) {
          logger.error(`安装失败: ${error.message}`);
        } finally {
          installBtn.disabled = false;
        }
      });
    }
  });
}

// ===== 动画模式功能 =====
function initAnimationMode() {
  document
    .getElementById("applyAnimationBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      const windowAnim = parseFloat(
        document.getElementById("windowAnim").value
      );
      const transitionAnim = parseFloat(
        document.getElementById("transitionAnim").value
      );
      const animatorAnim = parseFloat(
        document.getElementById("animatorAnim").value
      );

      logger.log(
        `正在设置动画速度: 窗口${windowAnim} 过渡${transitionAnim} 动画${animatorAnim}`
      );

      try {
        const result = await api.call("setAnimation", {
          Window: windowAnim,
          Transition: transitionAnim,
          Animator: animatorAnim,
        });

        if (result.success) {
          logger.success("动画速度设置成功");
        } else {
          logger.error("动画速度设置失败");
        }
      } catch (error) {
        logger.error(`设置失败: ${error.message}`);
      }
    });

  document.getElementById("resetAnimationBtn").addEventListener("click", () => {
    document.getElementById("windowAnim").value = 1;
    document.getElementById("transitionAnim").value = 1;
    document.getElementById("animatorAnim").value = 1;
    logger.log("已重置动画速度为默认值");
  });
}

// ===== 功能模式 =====
function initFunctionMode() {
  // Shizuku授权
  document
    .getElementById("shizukuAuthBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      logger.log("正在启动 Shizuku 服务...");

      try {
        const result = await api.call(
          "execAdb",
          "shell sh /sdcard/Android/data/moe.shizuku.privileged.api/start.sh"
        );
        if (result.success) {
          logger.success("Shizuku 启动成功");
          if (result.output) {
            logger.log(result.output);
          }
        } else {
          logger.error("Shizuku 启动失败，请确保已安装Shizuku应用");
        }
      } catch (error) {
        logger.error(`启动失败: ${error.message}`);
      }
    });

  document
    .getElementById("listDevicesBtn")
    .addEventListener("click", async () => {
      logger.log("正在查询已连接设备...");

      try {
        const result = await api.call("checkDevice");
        if (result.connected) {
          const deviceList = result.deviceList.join("\n");
          alert(`已连接设备:\n\n${deviceList}`);
          logger.success(`找到 ${result.deviceCount} 个设备`);
        } else {
          alert("未检测到设备");
          logger.error("未检测到设备");
        }
      } catch (error) {
        logger.error(`查询失败: ${error.message}`);
      }
    });

  document
    .getElementById("getVersionBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      logger.log("正在查询系统版本...");

      try {
        const result = await api.call("getSystemVersion");
        if (result.success) {
          alert(`AOSP版本: ${result.version}`);
          logger.success(`系统版本: ${result.version}`);
        } else {
          logger.error("获取版本失败");
        }
      } catch (error) {
        logger.error(`查询失败: ${error.message}`);
      }
    });

  // 查看内存情况
  document
    .getElementById("getMemoryBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      logger.log("正在获取内存信息...");

      try {
        const result = await api.call("getMemory");
        if (result.success) {
          // 在弹窗中显示内存信息
          alert(`内存信息:\n\n${result.output}`);
          logger.success("内存信息获取成功");
        } else {
          alert("获取内存信息失败");
          logger.error("获取内存信息失败");
        }
      } catch (error) {
        logger.error(`错误: ${error.message}`);
      }
    });

  // APK文件安装
  document
    .getElementById("installApkFileBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;
      logger.log("正在等待选择APK文件...");
      try {
        const result = await api.call("installApkFile");
        if (result.success) {
          logger.success(result.message);
        } else {
          // 如果只是取消选择，不显示错误
          if (result.message !== "未选择文件") {
            logger.error(result.message);
          } else {
            logger.log("已取消选择");
          }
        }
      } catch (error) {
        logger.error(`操作失败: ${error.message}`);
      }
    });

  // APK文件夹批量安装
  document
    .getElementById("installApkFolderBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;
      logger.log("正在等待选择APK文件夹...");
      try {
        const result = await api.call("installApkFolder");
        if (result.success) {
          logger.success(result.message);
          // 显示详细结果
          if (result.results && result.results.length > 0) {
            result.results.forEach((res) => {
              if (!res.success) logger.error(res.message);
            });
          }
        } else {
          if (result.message !== "未选择文件夹") {
            logger.error(result.message);
          } else {
            logger.log("已取消选择");
          }
        }
      } catch (error) {
        logger.error(`操作失败: ${error.message}`);
      }
    });

  // Manual uninstall/install (optional - only if elements exist in function page)
  const manualUninstallBtn = document.getElementById("manualUninstallBtn");
  const manualInstallBtn = document.getElementById("manualInstallBtn");
  const manualPackageInput = document.getElementById("manualPackage");

  if (manualUninstallBtn && manualPackageInput) {
    manualUninstallBtn.addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      const packageName = manualPackageInput.value.trim();
      if (!packageName) {
        alert("请输入包名");
        return;
      }

      logger.log(`正在卸载 ${packageName}...`);

      try {
        const result = await api.call("uninstallApp", packageName);
        if (result.success) {
          logger.success(result.message);
        } else {
          logger.error(result.message);
        }
      } catch (error) {
        logger.error(`卸载失败: ${error.message}`);
      }
    });
  }

  if (manualInstallBtn && manualPackageInput) {
    manualInstallBtn.addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      const packageName = manualPackageInput.value.trim();
      if (!packageName) {
        alert("请输入包名");
        return;
      }

      logger.log(`正在安装 ${packageName}...`);

      try {
        const result = await api.call("installApp", packageName);
        if (result.success) {
          logger.success(result.message);
        } else {
          logger.error(result.message);
        }
      } catch (error) {
        logger.error(`安装失败: ${error.message}`);
      }
    });
  }

  // ADB Terminal
  document.getElementById("execAdbBtn").addEventListener("click", async () => {
    const cmdInput = document.getElementById("manualAdbCommand");
    const terminalOutput = document.getElementById("adbTerminalOutput");
    const command = cmdInput.value.trim();

    if (!command) {
      logger.error("请输入ADB命令");
      return;
    }

    logger.log(`执行: adb ${command}`);
    terminalOutput.value = `> adb ${command}\n执行中...\n`;

    try {
      // Note: we don't checkDevice() here strictly because user might want to run 'devices' or 'kill-server' manually
      const result = await api.call("execAdb", command);

      terminalOutput.value = `> adb ${command}\n--------------------------\n${result.output}`;

      if (result.success) {
        logger.success("命令执行完成");
      } else {
        logger.error("命令执行返回错误");
      }
    } catch (error) {
      terminalOutput.value += `\n[JS Error]: ${error.message}`;
      logger.error(`执行异常: ${error.message}`);
    }
  });
}

// ===== 初始化 =====
document.addEventListener("DOMContentLoaded", () => {
  logger.log("鸿蒙工具箱已启动");

  // 禁用所有右键菜单
  document.addEventListener("contextmenu", (e) => {
    e.preventDefault();
    return false;
  });

  // 初始化标签页切换
  initTabs();

  // 加载默认页面（主菜单）
  pageLoader.loadPage("function");

  // 设备刷新按钮
  document
    .getElementById("refreshDeviceBtn")
    .addEventListener("click", checkDevice);

  // 清空日志按钮
  document.getElementById("clearLogBtn").addEventListener("click", () => {
    logger.clear();
    logger.log("日志已清空");
  });

  // 初始化窗口控制
  initWindowControls();
});

// ===== 窗口控制 =====
    function initWindowControls() {
        // Native window controls are now used.
        // This function is kept for potential future custom controls or IPC handling.
        console.log('Window controls initialized (Native)');
    }

// Update max button icon based on state
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
