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
        if (typeof updateMaxButton === 'function') {
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
  const tabContents = document.querySelectorAll(".tab-content");

  navItems.forEach((item) => {
    item.addEventListener("click", () => {
      const tabId = item.dataset.tab;

      // 更新导航状态
      navItems.forEach((nav) => nav.classList.remove("active"));
      item.classList.add("active");

      // 更新内容显示
      tabContents.forEach((content) => content.classList.remove("active"));
      document.getElementById(tabId).classList.add("active");

      logger.log(`切换到 ${item.textContent.trim()} 模块`);
    });
  });
}

// ===== 主菜单功能 =====
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

// ===== 定制模式功能 =====
function initCustomMode() {
  const cards = document.querySelectorAll("#custom .function-card");

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

// ===== 监视模式功能 =====
function initMonitorMode() {
  const monitorOutput = document.getElementById("monitorOutput");

  document
    .getElementById("getMemoryBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      logger.log("正在获取内存信息...");
      monitorOutput.textContent = "加载中...";

      try {
        const result = await api.call("getMemory");
        if (result.success) {
          monitorOutput.textContent = result.output;
          logger.success("内存信息获取成功");
        } else {
          monitorOutput.textContent = "获取失败";
          logger.error("获取内存信息失败");
        }
      } catch (error) {
        monitorOutput.textContent = "获取失败";
        logger.error(`错误: ${error.message}`);
      }
    });

  document
    .getElementById("getSystemPackagesBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      logger.log("正在获取系统软件列表...");
      monitorOutput.textContent = "加载中...";

      try {
        const result = await api.call("listPackages", "system");
        if (result.success) {
          monitorOutput.textContent =
            `系统软件包（共${result.count}个）:\n\n` +
            result.packages.join("\n");
          logger.success(`获取成功，共 ${result.count} 个系统应用`);
        } else {
          monitorOutput.textContent = "获取失败";
          logger.error("获取系统软件列表失败");
        }
      } catch (error) {
        monitorOutput.textContent = "获取失败";
        logger.error(`错误: ${error.message}`);
      }
    });

  document
    .getElementById("getUserPackagesBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      logger.log("正在获取用户软件列表...");
      monitorOutput.textContent = "加载中...";

      try {
        const result = await api.call("listPackages", "user");
        if (result.success) {
          monitorOutput.textContent =
            `用户软件包（共${result.count}个）:\n\n` +
            result.packages.join("\n");
          logger.success(`获取成功，共 ${result.count} 个用户应用`);
        } else {
          monitorOutput.textContent = "获取失败";
          logger.error("获取用户软件列表失败");
        }
      } catch (error) {
        monitorOutput.textContent = "获取失败";
        logger.error(`错误: ${error.message}`);
      }
    });

  document
    .getElementById("getAllPackagesBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      logger.log("正在获取所有软件列表...");
      monitorOutput.textContent = "加载中...";

      try {
        const result = await api.call("listPackages", "all");
        if (result.success) {
          monitorOutput.textContent =
            `所有软件包（共${result.count}个）:\n\n` +
            result.packages.join("\n");
          logger.success(`获取成功，共 ${result.count} 个应用`);
        } else {
          monitorOutput.textContent = "获取失败";
          logger.error("获取软件列表失败");
        }
      } catch (error) {
        monitorOutput.textContent = "获取失败";
        logger.error(`错误: ${error.message}`);
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

  document
    .getElementById("manualUninstallBtn")
    .addEventListener("click", async () => {
      if (!(await checkDevice())) return;

      const packageName = document.getElementById("manualPackage").value.trim();
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
      .getElementById("manualInstallBtn")
      .addEventListener("click", async () => {
        if (!(await checkDevice())) return;
  
        const packageName = document.getElementById("manualPackage").value.trim();
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

  // 初始化各个模块
  initTabs();
  initMainMenu();
  initCustomMode();
  initMonitorMode();
  initAnimationMode();
  initFunctionMode();

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
  // 绑定窗口操作按钮
  const bindBtn = (id, action) => {
    const btn = document.getElementById(id);
    if (btn) {
      console.log(`绑定按钮: ${id} -> ${action}`);
      btn.addEventListener("click", (e) => {
        console.log(`按钮点击: ${id} -> ${action}`);
        e.preventDefault();
        e.stopPropagation();
        api
          .call("windowControl", action)
          .then(() => {
            console.log(`窗口控制执行成功: ${action}`);
          })
          .catch((err) => {
            console.error(`窗口控制失败: ${action}`, err);
          });
      });
    } else {
      console.error(`未找到按钮: ${id}`);
    }
  };

  bindBtn("minBtn", "minimize");
  bindBtn("maxBtn", "toggleMaximize");
  bindBtn("closeBtn", "close");

  console.log("窗口控制已初始化");
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
