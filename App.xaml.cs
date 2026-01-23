using System;
using System.Windows;

namespace RimWorldTranslationTool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 設定全局異常處理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"程式發生未處理的錯誤：\n{e.Exception.Message}", 
                          "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
