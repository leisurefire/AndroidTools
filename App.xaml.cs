using System;
using System.Windows;

namespace HarmonyOSToolbox
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Support GBK encoding for ADB output
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            base.OnStartup(e);

            // 设置全局异常处理
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = (Exception)args.ExceptionObject;
                MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }
    }
}
