// ===== 定制模式模块 =====
window.modules = window.modules || {};

window.modules.customMode = {
  init() {
    const cards = document.querySelectorAll("#main .function-card");

    cards.forEach((card) => {
      const packageName = card.dataset.package;
      const uninstallBtn = card.querySelector(".uninstall-btn");
      const installBtn = card.querySelector(".install-btn");
      const appName = card.querySelector("h3")?.textContent;

      if (uninstallBtn) {
        uninstallBtn.addEventListener("click", async () => {
          if (!(await window.deviceManager.checkDevice())) return;

          window.logger.log(`正在卸载 ${appName}...`);
          uninstallBtn.disabled = true;

          try {
            const result = await window.api.call("uninstallApp", packageName);
            if (result.success) {
              window.logger.success(result.message);
            } else {
              window.logger.error(result.message);
            }
          } catch (error) {
            window.logger.error(`卸载失败: ${error.message}`);
          } finally {
            uninstallBtn.disabled = false;
          }
        });
      }

      if (installBtn) {
        installBtn.addEventListener("click", async () => {
          if (!(await window.deviceManager.checkDevice())) return;

          window.logger.log(`正在安装 ${appName}...`);
          installBtn.disabled = true;

          try {
            const result = await window.api.call("installApp", packageName);
            if (result.success) {
              window.logger.success(result.message);
            } else {
              window.logger.error(result.message);
            }
          } catch (error) {
            window.logger.error(`安装失败: ${error.message}`);
          } finally {
            installBtn.disabled = false;
          }
        });
      }
    });

    // 应用管理页面的查看软件按钮
    const customOutput = document.getElementById("customOutput");
    const getUserPackagesBtn2 = document.getElementById("getUserPackagesBtn2");
    
    if (getUserPackagesBtn2) {
      getUserPackagesBtn2.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;
        window.logger.log("正在获取用户软件列表...");
        customOutput.textContent = "加载中...";

        try {
          const result = await window.api.call("listPackages", "user");
          if (result.success) {
            customOutput.textContent =
              `用户软件包（共${result.count}个）:\n\n` +
              result.packages.join("\n");
            window.logger.success(`获取成功，共 ${result.count} 个用户应用`);
          } else {
            customOutput.textContent = "获取失败";
            window.logger.error("获取用户软件列表失败");
          }
        } catch (error) {
          customOutput.textContent = "获取失败";
          window.logger.error(`错误: ${error.message}`);
        }
      });
    }

    const getSystemPackagesBtn2 = document.getElementById("getSystemPackagesBtn2");
    if (getSystemPackagesBtn2) {
      getSystemPackagesBtn2.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;
        window.logger.log("正在获取系统软件列表...");
        customOutput.textContent = "加载中...";

        try {
          const result = await window.api.call("listPackages", "system");
          if (result.success) {
            customOutput.textContent =
              `系统软件包（共${result.count}个）:\n\n` +
              result.packages.join("\n");
            window.logger.success(`获取成功，共 ${result.count} 个系统应用`);
          } else {
            customOutput.textContent = "获取失败";
            window.logger.error("获取系统软件列表失败");
          }
        } catch (error) {
          customOutput.textContent = "获取失败";
          window.logger.error(`错误: ${error.message}`);
        }
      });
    }

    const getAllPackagesBtn2 = document.getElementById("getAllPackagesBtn2");
    if (getAllPackagesBtn2) {
      getAllPackagesBtn2.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;
        window.logger.log("正在获取所有软件列表...");
        customOutput.textContent = "加载中...";

        try {
          const result = await window.api.call("listPackages", "all");
          if (result.success) {
            customOutput.textContent =
              `所有软件包（共${result.count}个）:\n\n` +
              result.packages.join("\n");
            window.logger.success(`获取成功，共 ${result.count} 个应用`);
          } else {
            customOutput.textContent = "获取失败";
            window.logger.error("获取软件列表失败");
          }
        } catch (error) {
          customOutput.textContent = "获取失败";
          window.logger.error(`错误: ${error.message}`);
        }
      });
    }

    // 应用管理页面的手动卸载/安装
    const manualUninstallBtn2 = document.getElementById("manualUninstallBtn2");
    if (manualUninstallBtn2) {
      manualUninstallBtn2.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;
        const packageName = document
          .getElementById("manualPackage2")
          .value.trim();
        if (!packageName) {
          alert("请输入包名");
          return;
        }
        window.logger.log(`正在卸载 ${packageName}...`);

        try {
          const result = await window.api.call("uninstallApp", packageName);
          if (result.success) {
            window.logger.success(result.message);
          } else {
            window.logger.error(result.message);
          }
        } catch (error) {
          window.logger.error(`卸载失败: ${error.message}`);
        }
      });
    }

    const manualInstallBtn2 = document.getElementById("manualInstallBtn2");
    if (manualInstallBtn2) {
      manualInstallBtn2.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;
        const packageName = document
          .getElementById("manualPackage2")
          .value.trim();
        if (!packageName) {
          alert("请输入包名");
          return;
        }
        window.logger.log(`正在安装 ${packageName}...`);

        try {
          const result = await window.api.call("installApp", packageName);
          if (result.success) {
            window.logger.success(result.message);
          } else {
            window.logger.error(result.message);
          }
        } catch (error) {
          window.logger.error(`安装失败: ${error.message}`);
        }
      });
    }
  },
};
