// ===== 功能模式模块 =====
window.modules = window.modules || {};

window.modules.functionMode = {
  init() {
    // Shizuku授权
    const shizukuAuthBtn = document.getElementById("shizukuAuthBtn");
    if (shizukuAuthBtn) {
      shizukuAuthBtn.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;

        window.logger.log("正在启动 Shizuku 服务...");

        try {
          const result = await window.api.call(
            "execAdb",
            "shell sh /sdcard/Android/data/moe.shizuku.privileged.api/start.sh"
          );
          if (result.success) {
            window.logger.success("Shizuku 启动成功");
            if (result.output) {
              window.logger.log(result.output);
            }
          } else {
            window.logger.error("Shizuku 启动失败，请确保已安装Shizuku应用");
          }
        } catch (error) {
          window.logger.error(`启动失败: ${error.message}`);
        }
      });
    }

    // 列出设备
    const listDevicesBtn = document.getElementById("listDevicesBtn");
    if (listDevicesBtn) {
      listDevicesBtn.addEventListener("click", async () => {
        window.logger.log("正在查询已连接设备...");

        try {
          const result = await window.api.call("checkDevice");
          if (result.connected) {
            const deviceList = result.deviceList.join("\n");
            alert(`已连接设备:\n\n${deviceList}`);
            window.logger.success(`找到 ${result.deviceCount} 个设备`);
          } else {
            alert("未检测到设备");
            window.logger.error("未检测到设备");
          }
        } catch (error) {
          window.logger.error(`查询失败: ${error.message}`);
        }
      });
    }

    // 获取版本
    const getVersionBtn = document.getElementById("getVersionBtn");
    if (getVersionBtn) {
      getVersionBtn.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;

        window.logger.log("正在查询系统版本...");

        try {
          const result = await window.api.call("getSystemVersion");
          if (result.success) {
            alert(`AOSP版本: ${result.version}`);
            window.logger.success(`系统版本: ${result.version}`);
          } else {
            window.logger.error("获取版本失败");
          }
        } catch (error) {
          window.logger.error(`查询失败: ${error.message}`);
        }
      });
    }

    // 查看内存情况
    const getMemoryBtn = document.getElementById("getMemoryBtn");
    if (getMemoryBtn) {
      getMemoryBtn.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;

        window.logger.log("正在获取内存信息...");

        try {
          const result = await window.api.call("getMemory");
          if (result.success) {
            alert(`内存信息:\n\n${result.output}`);
            window.logger.success("内存信息获取成功");
          } else {
            alert("获取内存信息失败");
            window.logger.error("获取内存信息失败");
          }
        } catch (error) {
          window.logger.error(`错误: ${error.message}`);
        }
      });
    }

    // APK文件安装
    const installApkFileBtn = document.getElementById("installApkFileBtn");
    if (installApkFileBtn) {
      installApkFileBtn.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;
        window.logger.log("正在等待选择APK文件...");
        try {
          const result = await window.api.call("installApkFile");
          if (result.success) {
            window.logger.success(result.message);
          } else {
            if (result.message !== "未选择文件") {
              window.logger.error(result.message);
            } else {
              window.logger.log("已取消选择");
            }
          }
        } catch (error) {
          window.logger.error(`操作失败: ${error.message}`);
        }
      });
    }

    // APK文件夹批量安装
    const installApkFolderBtn = document.getElementById("installApkFolderBtn");
    if (installApkFolderBtn) {
      installApkFolderBtn.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;
        window.logger.log("正在等待选择APK文件夹...");
        try {
          const result = await window.api.call("installApkFolder");
          if (result.success) {
            window.logger.success(result.message);
            if (result.results && result.results.length > 0) {
              result.results.forEach((res) => {
                if (!res.success) window.logger.error(res.message);
              });
            }
          } else {
            if (result.message !== "未选择文件夹") {
              window.logger.error(result.message);
            } else {
              window.logger.log("已取消选择");
            }
          }
        } catch (error) {
          window.logger.error(`操作失败: ${error.message}`);
        }
      });
    }

    // ADB Terminal
    const execAdbBtn = document.getElementById("execAdbBtn");
    if (execAdbBtn) {
      execAdbBtn.addEventListener("click", async () => {
        const cmdInput = document.getElementById("manualAdbCommand");
        const terminalOutput = document.getElementById("adbTerminalOutput");
        
        if (!cmdInput || !terminalOutput) return;
        
        const command = cmdInput.value.trim();

        if (!command) {
          window.logger.error("请输入ADB命令");
          return;
        }

        window.logger.log(`执行: adb ${command}`);
        terminalOutput.value = `> adb ${command}\n执行中...\n`;

        try {
          const result = await window.api.call("execAdb", command);

          terminalOutput.value = `> adb ${command}\n--------------------------\n${result.output}`;

          if (result.success) {
            window.logger.success("命令执行完成");
          } else {
            window.logger.error("命令执行返回错误");
          }
        } catch (error) {
          terminalOutput.value += `\n[JS Error]: ${error.message}`;
          window.logger.error(`执行异常: ${error.message}`);
        }
      });
    }
  },
};
