using System;
using System.Windows;
using WPFLocalizeExtension.Engine;

namespace RimWorldTranslationTool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化 WPFLocalizeExtension - 確保與系統文化同步
            var currentCulture = System.Globalization.CultureInfo.CurrentCulture;
            WPFLocalizeExtension.Engine.LocalizeDictionary.Instance.Culture = currentCulture;
            
            // 設定全局異常處理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(string.Format(LocalizationManager.GetString("UnhandledException_Message"), e.Exception.Message), 
                          LocalizationManager.GetString("Error_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
