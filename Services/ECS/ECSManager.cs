using System;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.CrashReporting;
using RimWorldTranslationTool.Services.EmergencySave;
using RimWorldTranslationTool.Services.ErrorHandling;
using RimWorldTranslationTool.Services.Logging;

namespace RimWorldTranslationTool.Services.ECS
{
    /// <summary>
    /// ECS 協調管理器實現
    /// </summary>
    public class ECSManager : IECSManager
    {
        private readonly IErrorHandler _errorHandler;
        private readonly ICrashReportService _crashReportService;
        private readonly IEmergencySaveService _emergencySaveService;
        private readonly ILoggerService _loggerService;
        private readonly ECSStatus _status;

        public ECSManager(
            IErrorHandler errorHandler,
            ICrashReportService crashReportService,
            IEmergencySaveService emergencySaveService,
            ILoggerService loggerService)
        {
            _errorHandler = errorHandler;
            _crashReportService = crashReportService;
            _emergencySaveService = emergencySaveService;
            _loggerService = loggerService;
            _status = new ECSStatus();

            // 訂閱錯誤事件
            _errorHandler.ErrorOccurred += OnErrorOccurred;
        }

        public async Task HandleGlobalExceptionAsync(Exception exception, string source)
        {
            _status.IsEmergencyMode = true;
            _status.LastIncidentTime = DateTime.Now;
            _status.LastError = exception.Message;

            await _loggerService.LogCriticalAsync($"[ECS] 捕捉到全域異常: {source}", exception, "ECS");

            // 判斷是否需要執行緊急關閉流程
            // 對於全域未處理異常，通常視為致命
            await PerformEmergencyShutdownAsync(exception, source);
        }

        public async Task PerformEmergencyShutdownAsync(Exception exception, string context)
        {
            try
            {
                await _loggerService.LogInfoAsync($"[ECS] 開始緊急關閉流程: {context}", "ECS");

                // 1. 生成崩潰報告 (C)
                await _loggerService.LogInfoAsync("[ECS] 正在生成崩潰報告...", "ECS");
                var report = await _crashReportService.GenerateCrashReportAsync(exception, context);
                await _crashReportService.SaveCrashReportAsync(report);

                // 2. 執行緊急儲存 (S)
                await _loggerService.LogInfoAsync("[ECS] 正在執行緊急儲存...", "ECS");
                await _emergencySaveService.EmergencySaveAllAsync();

                await _loggerService.LogCriticalAsync("[ECS] 緊急關閉流程完成，程式即將退出", null, "ECS");
            }
            catch (Exception ex)
            {
                // 如果 ECS 流程本身失敗，最後的保險
                await _loggerService.LogCriticalAsync("[ECS] 緊急流程執行失敗!", ex, "ECS");
            }
        }

        public ECSStatus GetStatus() => _status;

        private async void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
        {
            // 當 ErrorHandler 捕捉到錯誤時，ECSManager 可以根據嚴重程度採取額外行動
            if (e.Severity >= ErrorSeverity.Critical && !e.Recovered)
            {
                await PerformEmergencyShutdownAsync(e.Exception, e.Context);
            }
        }
    }
}
