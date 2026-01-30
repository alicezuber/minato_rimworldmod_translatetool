using System;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.Logging
{
    /// <summary>
    /// 日誌服務介面
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// 日誌級別
        /// </summary>
        LogLevel MinimumLevel { get; set; }
        
        /// <summary>
        /// 是否啟用檔案日誌
        /// </summary>
        bool EnableFileLogging { get; set; }
        
        /// <summary>
        /// 是否啟用控制台日誌
        /// </summary>
        bool EnableConsoleLogging { get; set; }
        
        /// <summary>
        /// 記錄資訊日誌
        /// </summary>
        Task LogInfoAsync(string message, string category = "");
        
        /// <summary>
        /// 記錄警告日誌
        /// </summary>
        Task LogWarningAsync(string message, string category = "");
        
        /// <summary>
        /// 記錄錯誤日誌
        /// </summary>
        Task LogErrorAsync(string message, Exception? exception = null, string category = "");
        
        /// <summary>
        /// 記錄除錯日誌
        /// </summary>
        Task LogDebugAsync(string message, string category = "");
        
        /// <summary>
        /// 記錄嚴重錯誤日誌
        /// </summary>
        Task LogCriticalAsync(string message, Exception? exception = null, string category = "");
        
        /// <summary>
        /// 記錄操作開始
        /// </summary>
        Task LogOperationStartAsync(string operationName, string parameters = "");
        
        /// <summary>
        /// 記錄操作完成
        /// </summary>
        Task LogOperationCompleteAsync(string operationName, TimeSpan duration, string result = "");
        
        /// <summary>
        /// 記錄操作失敗
        /// </summary>
        Task LogOperationFailedAsync(string operationName, Exception exception, TimeSpan duration);
        
        /// <summary>
        /// 獲取日誌檔案路徑
        /// </summary>
        string GetLogFilePath(DateTime? date = null);
        
        /// <summary>
        /// 獲取所有日誌檔案
        /// </summary>
        Task<string[]> GetLogFilesAsync();
        
        /// <summary>
        /// 清理舊日誌檔案
        /// </summary>
        Task CleanupOldLogsAsync(int keepDays = 30);
        
        /// <summary>
        /// 清空所有日誌
        /// </summary>
        Task ClearAllLogsAsync();
    }
    
    /// <summary>
    /// 日誌級別
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
    
    /// <summary>
    /// 日誌條目
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = "";
        public string Category { get; set; } = "";
        public Exception? Exception { get; set; }
        public string ThreadId { get; set; } = "";
        public string ProcessId { get; set; } = "";
    }
}
