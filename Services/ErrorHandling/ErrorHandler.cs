using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Dialogs;
using RimWorldTranslationTool.Services.Logging;

namespace RimWorldTranslationTool.Services.ErrorHandling
{
    /// <summary>
    /// 錯誤處理服務實現
    /// </summary>
    public class ErrorHandler : IErrorHandler
    {
        private readonly ILoggerService _loggerService;
        private readonly IDialogService _dialogService;
        private readonly ConcurrentDictionary<Type, Func<Exception, string, Task<bool>>> _recoveryStrategies;
        private readonly Dictionary<ErrorSeverity, Func<Exception, string, Task>> _defaultHandlers;
        private readonly ConcurrentQueue<ErrorInfo> _errorHistory;
        private readonly object _statsLock = new object();
        private ErrorStatistics _statistics;
        
        public ErrorHandler(ILoggerService loggerService, IDialogService dialogService)
        {
            _loggerService = loggerService;
            _dialogService = dialogService;
            _recoveryStrategies = new ConcurrentDictionary<Type, Func<Exception, string, Task<bool>>>();
            _defaultHandlers = new Dictionary<ErrorSeverity, Func<Exception, string, Task>>();
            _errorHistory = new ConcurrentQueue<ErrorInfo>();
            _statistics = new ErrorStatistics();
            
            SetupDefaultHandlers();
            RegisterDefaultRecoveryStrategies();
        }
        
        public async Task<T> SafeExecuteAsync<T>(Func<Task<T>> operation, string operationName, ErrorSeverity severity = ErrorSeverity.Error)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _loggerService.LogOperationStartAsync(operationName);
                var result = await operation();
                stopwatch.Stop();
                await _loggerService.LogOperationCompleteAsync(operationName, stopwatch.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggerService.LogOperationFailedAsync(operationName, ex, stopwatch.Elapsed);
                await HandleExceptionAsync(ex, operationName, severity);
                
                // 根據嚴重程度決定是否重新拋出
                if (severity >= ErrorSeverity.Critical)
                {
                    throw;
                }
                
                return default(T)!;
            }
        }
        
        public async Task SafeExecuteAsync(Func<Task> operation, string operationName, ErrorSeverity severity = ErrorSeverity.Error)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _loggerService.LogOperationStartAsync(operationName);
                await operation();
                stopwatch.Stop();
                await _loggerService.LogOperationCompleteAsync(operationName, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggerService.LogOperationFailedAsync(operationName, ex, stopwatch.Elapsed);
                await HandleExceptionAsync(ex, operationName, severity);
                
                if (severity >= ErrorSeverity.Critical)
                {
                    throw;
                }
            }
        }
        
        public T SafeExecute<T>(Func<T> operation, string operationName, ErrorSeverity severity = ErrorSeverity.Error)
        {
            return SafeExecuteAsync(() => Task.Run(operation), operationName, severity).GetAwaiter().GetResult();
        }
        
        public void SafeExecute(Action operation, string operationName, ErrorSeverity severity = ErrorSeverity.Error)
        {
            SafeExecuteAsync(() => Task.Run(operation), operationName, severity).GetAwaiter().GetResult();
        }
        
        public async Task HandleExceptionAsync(Exception exception, string context, ErrorSeverity severity = ErrorSeverity.Error)
        {
            try
            {
                // 記錄錯誤
                await LogErrorAsync(exception, context, severity);
                
                // 更新統計
                UpdateStatistics(exception, severity);
                
                // 嘗試恢復
                var recovered = await TryRecoverAsync(exception, context);
                
                // 使用預設處理器
                if (_defaultHandlers.TryGetValue(severity, out var handler))
                {
                    await handler(exception, context);
                }
                else
                {
                    await DefaultExceptionHandler(exception, context, severity);
                }
                
                // 如果是致命錯誤且無法恢復，觸發應用程式關閉
                if (severity == ErrorSeverity.Fatal && !recovered)
                {
                    await HandleFatalErrorAsync(exception, context);
                }
            }
            catch (Exception handlerEx)
            {
                // 錯誤處理器本身出錯，記錄到調試視窗
                Debug.WriteLine($"錯誤處理器失敗: {handlerEx.Message}");
            }
        }
        
        public async Task<bool> TryRecoverAsync(Exception exception, string context)
        {
            try
            {
                var exceptionType = exception.GetType();
                
                // 尋找具體的恢復策略
                if (_recoveryStrategies.TryGetValue(exceptionType, out var strategy))
                {
                    var recovered = await strategy(exception, context);
                    if (recovered)
                    {
                        await _loggerService.LogInfoAsync($"錯誤恢復成功: {context} | 策略: {exceptionType.Name}", "Recovery");
                        UpdateRecoveryStatistics();
                        return true;
                    }
                }
                
                // 尋找父類型的恢復策略
                var baseType = exceptionType.BaseType;
                while (baseType != null)
                {
                    if (_recoveryStrategies.TryGetValue(baseType, out strategy))
                    {
                        var recovered = await strategy(exception, context);
                        if (recovered)
                        {
                            await _loggerService.LogInfoAsync($"錯誤恢復成功: {context} | 策略: {baseType.Name}", "Recovery");
                            UpdateRecoveryStatistics();
                            return true;
                        }
                    }
                    baseType = baseType.BaseType;
                }
                
                await _loggerService.LogWarningAsync($"無法恢復錯誤: {context} | 類型: {exceptionType.Name}", "Recovery");
                return false;
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"恢復過程中發生錯誤: {context}", ex, "Recovery");
                return false;
            }
        }
        
        public void RegisterRecoveryStrategy<T>(Func<T, string, Task<bool>> recoveryStrategy) where T : Exception
        {
            _recoveryStrategies[typeof(T)] = (ex, context) => recoveryStrategy((T)ex, context);
        }
        
        public void SetDefaultErrorHandler(ErrorSeverity severity, Func<Exception, string, Task> handler)
        {
            _defaultHandlers[severity] = handler;
        }
        
        public async Task<ErrorStatistics> GetStatisticsAsync()
        {
            return await Task.FromResult(_statistics);
        }
        
        public async Task ResetStatisticsAsync()
        {
            await Task.Run(() =>
            {
                lock (_statsLock)
                {
                    _statistics = new ErrorStatistics();
                    while (_errorHistory.TryDequeue(out _)) { }
                }
            });
        }
        
        private async Task LogErrorAsync(Exception exception, string context, ErrorSeverity severity)
        {
            var message = $"錯誤發生: {context} | 類型: {exception.GetType().Name} | 訊息: {exception.Message}";
            
            switch (severity)
            {
                case ErrorSeverity.Info:
                    await _loggerService.LogInfoAsync(message, "ErrorHandler");
                    break;
                case ErrorSeverity.Warning:
                    await _loggerService.LogWarningAsync(message, "ErrorHandler");
                    break;
                case ErrorSeverity.Error:
                    await _loggerService.LogErrorAsync(message, exception, "ErrorHandler");
                    break;
                case ErrorSeverity.Critical:
                case ErrorSeverity.Fatal:
                    await _loggerService.LogCriticalAsync(message, exception, "ErrorHandler");
                    break;
            }
        }
        
        private void UpdateStatistics(Exception exception, ErrorSeverity severity)
        {
            lock (_statsLock)
            {
                _statistics.TotalErrors++;
                _statistics.LastErrorTime = DateTime.Now;
                
                if (severity == ErrorSeverity.Critical)
                    _statistics.CriticalErrors++;
                else if (severity == ErrorSeverity.Warning)
                    _statistics.Warnings++;
                
                var errorType = exception.GetType().Name;
                if (!_statistics.ErrorTypes.ContainsKey(errorType))
                    _statistics.ErrorTypes[errorType] = 0;
                _statistics.ErrorTypes[errorType]++;
                
                _statistics.MostCommonErrorType = _statistics.ErrorTypes
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault().Key;
                
                // 記錄錯誤歷史
                _errorHistory.Enqueue(new ErrorInfo
                {
                    Exception = exception,
                    Context = "",
                    Severity = severity,
                    Timestamp = DateTime.Now
                });
                
                // 保持歷史記錄在合理範圍內
                while (_errorHistory.Count > 1000)
                {
                    _errorHistory.TryDequeue(out _);
                }
            }
        }
        
        private void UpdateRecoveryStatistics()
        {
            lock (_statsLock)
            {
                _statistics.RecoveredErrors++;
            }
        }
        
        private async Task DefaultExceptionHandler(Exception exception, string context, ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Info:
                    // 資訊級別不顯示彈窗
                    break;
                    
                case ErrorSeverity.Warning:
                    await _dialogService.ShowWarningAsync($"{context}: {exception.Message}", "警告");
                    break;
                    
                case ErrorSeverity.Error:
                    await _dialogService.ShowErrorAsync($"{context}: {exception.Message}", exception, "錯誤");
                    break;
                    
                case ErrorSeverity.Critical:
                    await _dialogService.ShowCriticalErrorAsync($"{context}: {exception.Message}", exception, "嚴重錯誤");
                    break;
                    
                case ErrorSeverity.Fatal:
                    await _dialogService.ShowCriticalErrorAsync($"程式遇到致命錯誤: {context}\n{exception.Message}", exception, "致命錯誤");
                    break;
            }
        }
        
        private void SetupDefaultHandlers()
        {
            SetDefaultErrorHandler(ErrorSeverity.Info, async (ex, context) => { });
            SetDefaultErrorHandler(ErrorSeverity.Warning, async (ex, context) => 
                await _dialogService.ShowWarningAsync($"{context}: {ex.Message}", "警告"));
            SetDefaultErrorHandler(ErrorSeverity.Error, async (ex, context) => 
                await _dialogService.ShowErrorAsync($"{context}: {ex.Message}", ex, "錯誤"));
            SetDefaultErrorHandler(ErrorSeverity.Critical, async (ex, context) => 
                await _dialogService.ShowCriticalErrorAsync($"{context}: {ex.Message}", ex, "嚴重錯誤"));
            SetDefaultErrorHandler(ErrorSeverity.Fatal, async (ex, context) => 
                await _dialogService.ShowCriticalErrorAsync($"程式遇到致命錯誤: {context}", ex, "致命錯誤"));
        }
        
        private void RegisterDefaultRecoveryStrategies()
        {
            // 檔案存取錯誤恢復策略
            RegisterRecoveryStrategy<System.IO.IOException>(async (ex, context) =>
            {
                if (ex.Message.Contains("被使用中") || ex.Message.Contains("being used"))
                {
                    await Task.Delay(1000); // 等待一秒後重試
                    return true;
                }
                return false;
            });
            
            // 網路錯誤恢復策略
            RegisterRecoveryStrategy<System.Net.WebException>(async (ex, context) =>
            {
                if (ex.Status == System.Net.WebExceptionStatus.Timeout)
                {
                    // 嘗試增加超時時間重試
                    await Task.Delay(2000);
                    return true;
                }
                return false;
            });
            
            // 權限錯誤恢復策略
            RegisterRecoveryStrategy<System.UnauthorizedAccessException>(async (ex, context) =>
            {
                await _dialogService.ShowWarningAsync("權限不足，請以管理員身份執行程式", "權限錯誤");
                return false;
            });
        }
        
        private async Task HandleFatalErrorAsync(Exception exception, string context)
        {
            await _loggerService.LogCriticalAsync("程式即將關閉 - 致命錯誤", exception, "FatalError");
            
            // 這裡可以添加緊急儲存邏輯
            // await _emergencySaveService.SaveCriticalDataAsync();
            
            // 關閉應用程式
            System.Windows.Application.Current?.Shutdown(1);
        }
    }
    
    internal class ErrorInfo
    {
        public Exception Exception { get; set; } = null!;
        public string Context { get; set; } = "";
        public ErrorSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
