using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.Logging
{
    /// <summary>
    /// 日誌服務實現
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private readonly LogConfiguration _config;
        private readonly ConcurrentQueue<LogEntry> _logQueue;
        private readonly Timer? _cleanupTimer;
        private readonly SemaphoreSlim _writeLock;
        private bool _disposed = false;
        
        public LogLevel MinimumLevel 
        { 
            get => _config.MinimumLevel;
            set => _config.MinimumLevel = value;
        }
        
        public bool EnableFileLogging 
        { 
            get => _config.EnableFileLogging;
            set => _config.EnableFileLogging = value;
        }
        
        public bool EnableConsoleLogging 
        { 
            get => _config.EnableConsoleLogging;
            set => _config.EnableConsoleLogging = value;
        }
        
        public LoggerService(LogConfiguration? config = null)
        {
            _config = config ?? LogConfiguration.CreateDefault();
            _logQueue = new ConcurrentQueue<LogEntry>();
            _writeLock = new SemaphoreSlim(1, 1);
            
            // 確保日誌目錄存在
            EnsureLogDirectoryExists();
            
            // 啟動自動清理定時器
            if (_config.AutoCleanupIntervalHours > 0)
            {
                var interval = TimeSpan.FromHours(_config.AutoCleanupIntervalHours);
                _cleanupTimer = new Timer(async _ => await CleanupOldLogsAsync(), null, interval, interval);
            }
            
            // 啟動背景寫入任務
            Task.Run(BackgroundLogWriter);
        }
        
        public async Task LogInfoAsync(string message, string category = "")
        {
            await LogAsync(LogLevel.Info, message, category, null);
        }
        
        public async Task LogWarningAsync(string message, string category = "")
        {
            await LogAsync(LogLevel.Warning, message, category, null);
        }
        
        public async Task LogErrorAsync(string message, Exception? exception = null, string category = "")
        {
            await LogAsync(LogLevel.Error, message, category, exception);
        }
        
        public async Task LogDebugAsync(string message, string category = "")
        {
            await LogAsync(LogLevel.Debug, message, category, null);
        }
        
        public async Task LogCriticalAsync(string message, Exception? exception = null, string category = "")
        {
            await LogAsync(LogLevel.Critical, message, category, exception);
        }
        
        public async Task LogOperationStartAsync(string operationName, string parameters = "")
        {
            var message = string.IsNullOrEmpty(parameters) 
                ? $"開始執行: {operationName}"
                : $"開始執行: {operationName} | 參數: {parameters}";
            await LogInfoAsync(message, "Operation");
        }
        
        public async Task LogOperationCompleteAsync(string operationName, TimeSpan duration, string result = "")
        {
            var message = string.IsNullOrEmpty(result)
                ? $"執行完成: {operationName} | 耗時: {duration.TotalMilliseconds:F0}ms"
                : $"執行完成: {operationName} | 耗時: {duration.TotalMilliseconds:F0}ms | 結果: {result}";
            await LogInfoAsync(message, "Operation");
        }
        
        public async Task LogOperationFailedAsync(string operationName, Exception exception, TimeSpan duration)
        {
            var message = $"執行失敗: {operationName} | 耗時: {duration.TotalMilliseconds:F0}ms | 錯誤: {exception.Message}";
            await LogErrorAsync(message, exception, "Operation");
        }
        
        public string GetLogFilePath(DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            var fileName = string.Format(_config.FileNameFormat, targetDate);
            return Path.Combine(_config.LogDirectory, fileName);
        }
        
        public async Task<string[]> GetLogFilesAsync()
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(_config.LogDirectory))
                    return Array.Empty<string>();
                    
                return Directory.GetFiles(_config.LogDirectory, "RimWorld_*.log")
                    .OrderByDescending(f => f)
                    .ToArray();
            });
        }
        
        public async Task CleanupOldLogsAsync(int keepDays = 30)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(_config.LogDirectory))
                        return;
                        
                    var cutoffDate = DateTime.Today.AddDays(-keepDays);
                    var logFiles = Directory.GetFiles(_config.LogDirectory, "RimWorld_*.log");
                    
                    foreach (var file in logFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                // 記錄清理失敗，但不拋出異常
                                Debug.WriteLine($"清理日誌檔案失敗: {file} | 錯誤: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"清理日誌失敗: {ex.Message}");
                }
            });
        }
        
        public async Task ClearAllLogsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(_config.LogDirectory))
                        return;
                        
                    var logFiles = Directory.GetFiles(_config.LogDirectory, "RimWorld_*.log");
                    
                    foreach (var file in logFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"刪除日誌檔案失敗: {file} | 錯誤: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"清空日誌失敗: {ex.Message}");
                }
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cleanupTimer?.Dispose();
                _writeLock?.Dispose();
                
                // 確保所有日誌都被寫入
                FlushLogsAsync().Wait();
            }
        }
        
        private async Task LogAsync(LogLevel level, string message, string category, Exception? exception)
        {
            if (level < _config.MinimumLevel)
                return;
                
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Category = category,
                Exception = exception,
                ThreadId = _config.IncludeThreadInfo ? Thread.CurrentThread.ManagedThreadId.ToString() : "",
                ProcessId = Process.GetCurrentProcess().Id.ToString()
            };
            
            _logQueue.Enqueue(logEntry);
            
            // 對於嚴重錯誤，立即寫入
            if (level >= LogLevel.Critical)
            {
                await FlushLogsAsync();
            }
        }
        
        private async Task BackgroundLogWriter()
        {
            while (!_disposed)
            {
                await FlushLogsAsync();
                await Task.Delay(1000); // 每秒檢查一次
            }
        }
        
        private async Task FlushLogsAsync()
        {
            if (!_config.EnableFileLogging || _logQueue.IsEmpty)
                return;
                
            await _writeLock.WaitAsync();
            try
            {
                var logsToWrite = new List<LogEntry>();
                
                // 取出所有待寫入的日誌
                while (_logQueue.TryDequeue(out var logEntry))
                {
                    logsToWrite.Add(logEntry);
                }
                
                if (logsToWrite.Count == 0)
                    return;
                
                // 按日期分組寫入
                var groupedByDate = logsToWrite.GroupBy(l => l.Timestamp.Date);
                
                foreach (var group in groupedByDate)
                {
                    var logFilePath = GetLogFilePath(group.Key);
                    await WriteLogsToFileAsync(logFilePath, group.ToList());
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }
        
        private async Task WriteLogsToFileAsync(string filePath, List<LogEntry> logEntries)
        {
            try
            {
                // 檢查檔案大小，如果超過限制則分割
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > _config.MaxFileSizeMB * 1024 * 1024)
                    {
                        var backupPath = Path.ChangeExtension(filePath, $".{DateTime.Now:HHmmss}.log");
                        File.Move(filePath, backupPath);
                    }
                }
                
                var logContent = new StringBuilder();
                foreach (var entry in logEntries)
                {
                    var formattedMessage = FormatLogMessage(entry);
                    logContent.AppendLine(formattedMessage);
                    
                    // 同時輸出到控制台（如果啟用）
                    if (_config.EnableConsoleLogging)
                    {
                        Console.WriteLine(formattedMessage);
                    }
                }
                
                await File.AppendAllTextAsync(filePath, logContent.ToString());
            }
            catch (Exception ex)
            {
                // 寫入日誌失敗，輸出到調試視窗
                Debug.WriteLine($"寫入日誌檔案失敗: {filePath} | 錯誤: {ex.Message}");
            }
        }
        
        private string FormatLogMessage(LogEntry entry)
        {
            var levelText = entry.Level.ToString().ToUpper();
            var categoryText = string.IsNullOrEmpty(entry.Category) ? "" : $"[{entry.Category}]";
            var threadText = _config.IncludeThreadInfo ? $"[T:{entry.ThreadId}]" : "";
            
            var message = string.Format(_config.LogFormat, 
                entry.Timestamp, levelText, categoryText, entry.Message);
                
            if (!string.IsNullOrEmpty(threadText))
            {
                message += $" {threadText}";
            }
            
            if (entry.Exception != null && _config.IncludeExceptionStack)
            {
                message += Environment.NewLine + entry.Exception.ToString();
            }
            
            return message;
        }
        
        private void EnsureLogDirectoryExists()
        {
            try
            {
                if (!string.IsNullOrEmpty(_config.LogDirectory) && !Directory.Exists(_config.LogDirectory))
                {
                    Directory.CreateDirectory(_config.LogDirectory);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"建立日誌目錄失敗: {_config.LogDirectory} | 錯誤: {ex.Message}");
            }
        }
    }
}
