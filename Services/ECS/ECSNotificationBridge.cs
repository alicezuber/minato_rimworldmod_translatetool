using System;
using System.Windows;
using RimWorldTranslationTool.Services.Dialogs;
using RimWorldTranslationTool.Services.ErrorHandling;

namespace RimWorldTranslationTool.Services.ECS
{
    /// <summary>
    /// ECS 通知橋接器 - 負責將錯誤事件轉換為 UI 彈窗
    /// </summary>
    public class ECSNotificationBridge
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IDialogService _dialogService;

        public ECSNotificationBridge(IErrorHandler errorHandler, IDialogService dialogService)
        {
            _errorHandler = errorHandler;
            _dialogService = dialogService;
            
            _errorHandler.ErrorOccurred += OnErrorOccurred;
        }

        private async void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
        {
            // 確保在 UI 執行緒執行
            if (Application.Current == null) return;

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                switch (e.Severity)
                {
                    case ErrorSeverity.Info:
                        // Info 級別預設不顯示彈窗
                        break;

                    case ErrorSeverity.Warning:
                        await _dialogService.ShowWarningAsync($"{e.Context}: {e.Exception.Message}", "警告");
                        break;

                    case ErrorSeverity.Error:
                        await _dialogService.ShowErrorAsync($"{e.Context}: {e.Exception.Message}", e.Exception, "錯誤");
                        break;

                    case ErrorSeverity.Critical:
                    case ErrorSeverity.Fatal:
                        await _dialogService.ShowCriticalErrorAsync(
                            $"{e.Context}: {e.Exception.Message}\n\n程式遇到嚴重錯誤，正在執行緊急儲存與報告生成...", 
                            e.Exception, 
                            "嚴重錯誤");
                        break;
                }
            });
        }
    }
}
