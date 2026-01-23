using System;
using System.Windows;

namespace RimWorldTranslationTool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化本地化服務 - 預設繁體中文
            LocalizationService.Instance.SetLanguage("zh-TW");
            
            // 設定全局異常處理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(LocalizationManager.GetString("UnhandledException_Message", e.Exception.Message), 
                          LocalizationManager.GetString("Error_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
