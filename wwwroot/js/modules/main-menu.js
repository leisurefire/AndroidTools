// ===== 主菜单模块（快捷卸载） =====
window.modules = window.modules || {};

window.modules.mainMenu = {
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
  },
};
