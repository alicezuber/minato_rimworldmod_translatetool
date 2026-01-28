using System;
using System.Threading.Tasks;
using System.Windows;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Dialogs;
using RimWorldTranslationTool.Services.ErrorHandling;
using RimWorldTranslationTool.Services.Paths;
using RimWorldTranslationTool.Services.CrashReporting;
using RimWorldTranslationTool.Services.EmergencySave;
using RimWorldTranslationTool.Services.ECS;

namespace RimWorldTranslationTool
{
    public partial class App : Application
    {
        public ILoggerService? LoggerService => _loggerService;
        public IDialogService? DialogService => _dialogService;
        public IErrorHandler? ErrorHandler => _errorHandler;
        public IPathService? PathService => _pathService;
        public ICrashReportService? CrashReportService => _crashReportService;
        public IEmergencySaveService? EmergencySaveService => _emergencySaveService;
        public IECSManager? ECSManager => _ecsManager;

        private ILoggerService? _loggerService;
        private IDialogService? _dialogService;
        private IErrorHandler? _errorHandler;
        private IPathService? _pathService;
        private ICrashReportService? _crashReportService;
        private IEmergencySaveService? _emergencySaveService;
        private IECSManager? _ecsManager;
        private ECSNotificationBridge? _notificationBridge;
        
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
                MessageBox.Show($"程式初始化失敗: {ex.Message}", "嚴重錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
        
        private void InitializeServices()
        {
            try
            {
                // 1. 基礎服務
                _pathService = new PathService();
                var logConfig = LogConfiguration.CreateDevelopment();
                _loggerService = new LoggerService(logConfig);
                _dialogService = new DialogService();
                
                // 2. ECS 組件
                _errorHandler = new ErrorHandler(_loggerService);
                _crashReportService = new CrashReportService(_pathService);
                _emergencySaveService = new EmergencySaveService(_pathService, _loggerService);
                
                // 3. ECS 管理器
                _ecsManager = new ECSManager(_errorHandler, _crashReportService, _emergencySaveService, _loggerService);
                
                // 4. UI 橋接 (解藕 ErrorHandler 與 DialogService)
                _notificationBridge = new ECSNotificationBridge(_errorHandler, _dialogService);

                _loggerService.LogInfoAsync("應用程式服務初始化完成", "Application").Wait();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"服務初始化失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SetupGlobalExceptionHandling()
        {
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }
        
        private async void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (_ecsManager != null)
            {
                await _ecsManager.HandleGlobalExceptionAsync(e.Exception, "UI Thread");
                e.Handled = true;
                await GracefulShutdownAsync(1);
            }
            else
            {
                MessageBox.Show($"全域異常 (無 ECS): {e.Exception.Message}", "致命錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
        
        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex && _ecsManager != null)
            {
                await _ecsManager.HandleGlobalExceptionAsync(ex, "AppDomain");
                if (e.IsTerminating)
                {
                    await GracefulShutdownAsync(1);
                }
            }
        }
        
        private async void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            if (_errorHandler != null)
            {
                await _errorHandler.HandleExceptionAsync(e.Exception, "TaskScheduler", ErrorSeverity.Warning);
                e.SetObserved();
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_loggerService != null)
                {
                    _loggerService.LogInfoAsync($"應用程式關閉，退出代碼: {e.ApplicationExitCode}", "Application").Wait();
                    if (_loggerService is IDisposable disposableLogger) disposableLogger.Dispose();
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
                await Task.Delay(1000);
                Current.Dispatcher.Invoke(() => Shutdown(exitCode));
            }
            catch
            {
                Shutdown(exitCode);
            }
        }
    }
}
