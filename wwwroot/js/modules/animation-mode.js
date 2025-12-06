// ===== 动画模式模块 =====
window.modules = window.modules || {};

window.modules.animationMode = {
  init() {
    const applyAnimationBtn = document.getElementById("applyAnimationBtn");
    if (applyAnimationBtn) {
      applyAnimationBtn.addEventListener("click", async () => {
        if (!(await window.deviceManager.checkDevice())) return;

        const windowAnim = parseFloat(
          document.getElementById("windowAnim").value
        );
        const transitionAnim = parseFloat(
          document.getElementById("transitionAnim").value
        );
        const animatorAnim = parseFloat(
          document.getElementById("animatorAnim").value
        );

        window.logger.log(
          `正在设置动画速度: 窗口${windowAnim} 过渡${transitionAnim} 动画${animatorAnim}`
        );

        try {
          const result = await window.api.call("setAnimation", {
            Window: windowAnim,
            Transition: transitionAnim,
            Animator: animatorAnim,
          });

          if (result.success) {
            window.logger.success("动画速度设置成功");
          } else {
            window.logger.error("动画速度设置失败");
          }
        } catch (error) {
          window.logger.error(`设置失败: ${error.message}`);
        }
      });
    }

    const resetAnimationBtn = document.getElementById("resetAnimationBtn");
    if (resetAnimationBtn) {
      resetAnimationBtn.addEventListener("click", () => {
        document.getElementById("windowAnim").value = 1;
        document.getElementById("transitionAnim").value = 1;
        document.getElementById("animatorAnim").value = 1;
        window.logger.log("已重置动画速度为默认值");
      });
    }
  },
};
