using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Logging;

namespace RimWorldTranslationTool.Services.ErrorHandling
{
    /// <summary>
    /// 錯誤處理服務實現 (解藕 UI 依賴)
    /// </summary>
    public class ErrorHandler : IErrorHandler
    {
        private readonly ILoggerService _loggerService;
        private readonly ConcurrentDictionary<Type, Func<Exception, string, Task<bool>>> _recoveryStrategies;
        private readonly Dictionary<ErrorSeverity, Func<Exception, string, Task>> _defaultHandlers;
        private readonly ConcurrentQueue<ErrorInfo> _errorHistory;
        private readonly object _statsLock = new object();
        private ErrorStatistics _statistics;

        public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;
        
        public ErrorHandler(ILoggerService loggerService)
        {
            _loggerService = loggerService;
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
                UpdateStatistics(exception, severity, context);
                
                // 嘗試恢復
                var recovered = await TryRecoverAsync(exception, context);
                
                // 觸發事件 (通知 UI 或 ECSManager)
                ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs(exception, context, severity, recovered));

                // 使用預設處理器 (自定義處理器)
                if (_defaultHandlers.TryGetValue(severity, out var handler))
                {
                    await handler(exception, context);
                }
            }
            catch (Exception handlerEx)
            {
                Debug.WriteLine($"錯誤處理器失敗: {handlerEx.Message}");
            }
        }
        
        public async Task<bool> TryRecoverAsync(Exception exception, string context)
        {
            try
            {
                var exceptionType = exception.GetType();
                
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
        
        private void UpdateStatistics(Exception exception, ErrorSeverity severity, string context)
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
                
                _errorHistory.Enqueue(new ErrorInfo
                {
                    Exception = exception,
                    Context = context,
                    Severity = severity,
                    Timestamp = DateTime.Now
                });
                
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
        
        private void SetupDefaultHandlers()
        {
            // 預設不再這裡處理 UI，交由 ECSNotificationBridge
            SetDefaultErrorHandler(ErrorSeverity.Info, async (ex, context) => { });
        }
        
        private void RegisterDefaultRecoveryStrategies()
        {
            RegisterRecoveryStrategy<System.IO.IOException>(async (ex, context) =>
            {
                if (ex.Message.Contains("被使用中") || ex.Message.Contains("being used"))
                {
                    await Task.Delay(1000);
                    return true;
                }
                return false;
            });
            
            RegisterRecoveryStrategy<System.Net.WebException>(async (ex, context) =>
            {
                if (ex.Status == System.Net.WebExceptionStatus.Timeout)
                {
                    await Task.Delay(2000);
                    return true;
                }
                return false;
            });
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
