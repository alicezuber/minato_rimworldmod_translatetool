using System;
using System.Threading.Tasks;
using System.Windows;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Dialogs;
using RimWorldTranslationTool.Services.ErrorHandling;

namespace RimWorldTranslationTool
{
    public partial class App : Application
    {
        private ILoggerService? _loggerService;
        private IDialogService? _dialogService;
        private IErrorHandler? _errorHandler;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // 初始化服務
                InitializeServices();
                
                // 設定全域異常處理
                SetupGlobalExceptionHandling();
                
                // 初始化本地化服務 - 預設繁體中文
                LocalizationService.Instance.SetLanguage("zh-TW");
                
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                // 如果初始化失敗，顯示基本錯誤訊息
                MessageBox.Show($"程式初始化失敗: {ex.Message}", "嚴重錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
        
        private void InitializeServices()
        {
            try
            {
                // 初始化日誌服務
                var logConfig = LogConfiguration.CreateDevelopment();
                _loggerService = new LoggerService(logConfig);
                
                // 初始化彈窗服務
                _dialogService = new DialogService();
                
                // 初始化錯誤處理服務
                _errorHandler = new ErrorHandler(_loggerService!, _dialogService!);
                
                // 記錄應用程式啟動
                _loggerService.LogInfoAsync("應用程式啟動", "Application").Wait();
            }
            catch (Exception ex)
            {
                // 如果服務初始化失敗，使用基本的錯誤處理
                MessageBox.Show($"服務初始化失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SetupGlobalExceptionHandling()
        {
            // WPF UI 執行緒異常
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            
            // AppDomain 異常
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // TaskScheduler 異常
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }
        
        private async void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                // 記錄嚴重錯誤
                if (_loggerService != null)
                {
                    await _loggerService.LogCriticalAsync("UI執行緒未處理異常", e.Exception, "GlobalException");
                }
                
                // 顯示友善錯誤訊息
                if (_dialogService != null)
                {
                    await _dialogService.ShowCriticalErrorAsync(
                        "程式發生未預期的錯誤，即將關閉。\n\n錯誤資訊已儲存，您可以重新啟動程式。",
                        e.Exception,
                        "嚴重錯誤");
                }
                else
                {
                    MessageBox.Show("程式發生未預期的錯誤，即將關閉。", "嚴重錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                // 防止程式崩潰
                e.Handled = true;
                
                // 優雅關閉
                await GracefulShutdownAsync(1);
            }
            catch (Exception handlerEx)
            {
                // 如果錯誤處理器本身失敗，記錄到調試視窗
                System.Diagnostics.Debug.WriteLine($"全域異常處理失敗: {handlerEx.Message}");
                
                // 最後的防線：基本錯誤訊息
                MessageBox.Show("程式發生嚴重錯誤，即將關閉。", "致命錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                
                e.Handled = true;
                Shutdown(1);
            }
        }
        
        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    if (_loggerService != null)
                    {
                        await _loggerService.LogCriticalAsync("後台執行緒未處理異常", ex, "GlobalException");
                    }
                    
                    // 如果是嚴重錯誤，準備關閉
                    if (e.IsTerminating)
                    {
                        if (_loggerService != null)
                        {
                            await _loggerService.LogCriticalAsync("程式即將終止", null, "GlobalException");
                        }
                        
                        if (_dialogService != null)
                        {
                            await _dialogService.ShowCriticalErrorAsync(
                                "程式遇到致命錯誤，即將關閉。",
                                ex,
                                "致命錯誤");
                        }
                        
                        await GracefulShutdownAsync(1);
                    }
                }
            }
            catch (Exception handlerEx)
            {
                System.Diagnostics.Debug.WriteLine($"後台異常處理失敗: {handlerEx.Message}");
            }
        }
        
        private async void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                if (_loggerService != null)
                {
                    await _loggerService.LogCriticalAsync("非同步任務未觀察到異常", e.Exception, "GlobalException");
                }
                
                // 防止程式崩潰
                e.SetObserved();
                
                // 記錄錯誤但繼續執行
                if (_dialogService != null)
                {
                    await _dialogService.ShowWarningAsync(
                        "發生非同步任務錯誤，但程式會繼續執行。",
                        "非同步錯誤");
                }
            }
            catch (Exception handlerEx)
            {
                System.Diagnostics.Debug.WriteLine($"非同步異常處理失敗: {handlerEx.Message}");
                e.SetObserved();
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 記錄應用程式關閉
                if (_loggerService != null)
                {
                    _loggerService.LogInfoAsync($"應用程式關閉，退出代碼: {e.ApplicationExitCode}", "Application").Wait();
                    
                    // 清理資源
                    if (_loggerService is IDisposable disposableLogger)
                    {
                        disposableLogger.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"應用程式關閉時發生錯誤: {ex.Message}");
            }
            
            base.OnExit(e);
        }
        
        private async Task GracefulShutdownAsync(int exitCode)
        {
            try
            {
                // 給一些時間完成清理
                await Task.Delay(1000);
                
                // 關閉應用程式
                Current.Dispatcher.Invoke(() =>
                {
                    Shutdown(exitCode);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"優雅關閉失敗: {ex.Message}");
                Shutdown(exitCode);
            }
        }
    }
}
