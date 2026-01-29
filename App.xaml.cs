using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Dialogs;
using RimWorldTranslationTool.Services.ErrorHandling;
using RimWorldTranslationTool.Services.Paths;
using RimWorldTranslationTool.Services.CrashReporting;
using RimWorldTranslationTool.Services.EmergencySave;
using RimWorldTranslationTool.Services.ECS;
using RimWorldTranslationTool.Services.Scanning;
using RimWorldTranslationTool.Services.Infrastructure;
using RimWorldTranslationTool.Services.Settings;
using RimWorldTranslationTool.ViewModels;

namespace RimWorldTranslationTool
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        public IServiceProvider? ServiceProvider => _serviceProvider;

        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
            try
            {
                // 1. 初始化 DI 容器
                ConfigureServices();

                // 2. 初始化本地化服務 - 預設繁體中文
                LocalizationService.Instance.SetLanguage("zh-TW");

                // 3. 設定全域異常處理
                SetupGlobalExceptionHandling();

                // 4. 顯示主視窗
                var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
                mainWindow?.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"程式初始化失敗: {ex.Message}", "嚴重錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        // 向後相容屬性
        public ILoggerService? LoggerService => _serviceProvider?.GetService<ILoggerService>();
        public IDialogService? DialogService => _serviceProvider?.GetService<IDialogService>();
        public IErrorHandler? ErrorHandler => _serviceProvider?.GetService<IErrorHandler>();
        public IPathService? PathService => _serviceProvider?.GetService<IPathService>();
        public ICrashReportService? CrashReportService => _serviceProvider?.GetService<ICrashReportService>();
        public IEmergencySaveService? EmergencySaveService => _serviceProvider?.GetService<IEmergencySaveService>();
        public IECSManager? ECSManager => _serviceProvider?.GetService<IECSManager>();

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // 1. 基礎設施與日誌
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<ILoggerService>(sp => 
                new LoggerService(LogConfiguration.CreateDevelopment()));
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IXmlParserService, XmlParserService>();

            // 2. ECS 安全防護體系
            services.AddSingleton<IErrorHandler, ErrorHandler>();
            services.AddSingleton<ICrashReportService, CrashReportService>();
            services.AddSingleton<IEmergencySaveService, EmergencySaveService>();
            services.AddSingleton<IECSManager, ECSManager>();
            services.AddSingleton<ECSNotificationBridge>();

            // 3. 業務邏輯服務
            services.AddSingleton<IModInfoService, ModInfoService>();
            services.AddSingleton<IModScannerService, ModScannerService>();
            services.AddSingleton<ITranslationMappingService, TranslationMappingService>();
            
            // 設定相關
            services.AddSingleton<SettingsValidationService>();
            services.AddSingleton<SettingsBackupService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<Controllers.SettingsController>();

            // 4. ViewModels
            services.AddSingleton<MainViewModel>();

            // 5. Views
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            // 啟動通知橋接器
            _serviceProvider.GetRequiredService<ECSNotificationBridge>();
        }

        private void SetupGlobalExceptionHandling()
        {
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private async void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ecs = _serviceProvider?.GetService<IECSManager>();
            if (ecs != null)
            {
                await ecs.HandleGlobalExceptionAsync(e.Exception, "UI Thread");
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
            if (e.ExceptionObject is Exception ex)
            {
                var ecs = _serviceProvider?.GetService<IECSManager>();
                if (ecs != null)
                {
                    await ecs.HandleGlobalExceptionAsync(ex, "AppDomain");
                }
                if (e.IsTerminating)
                {
                    await GracefulShutdownAsync(1);
                }
            }
        }

        private async void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            var errorHandler = _serviceProvider?.GetService<IErrorHandler>();
            if (errorHandler != null)
            {
                await errorHandler.HandleExceptionAsync(e.Exception, "TaskScheduler", ErrorSeverity.Warning);
                e.SetObserved();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var logger = _serviceProvider?.GetService<ILoggerService>();
                if (logger != null)
                {
                    logger.LogInfoAsync($"應用程式關閉，退出代碼: {e.ApplicationExitCode}", "Application").Wait();
                    if (logger is IDisposable disposable) disposable.Dispose();
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
