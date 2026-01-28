using System;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.ErrorHandling
{
    /// <summary>
    /// 錯誤處理服務介面
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// 當錯誤發生時觸發
        /// </summary>
        event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;

        /// <summary>
        /// 安全執行操作並返回結果
        /// </summary>
        Task<T> SafeExecuteAsync<T>(Func<Task<T>> operation, string operationName, ErrorSeverity severity = ErrorSeverity.Error);
        
        /// <summary>
        /// 安全執行操作（無返回值）
        /// </summary>
        Task SafeExecuteAsync(Func<Task> operation, string operationName, ErrorSeverity severity = ErrorSeverity.Error);
        
        /// <summary>
        /// 同步安全執行操作並返回結果
        /// </summary>
        T SafeExecute<T>(Func<T> operation, string operationName, ErrorSeverity severity = ErrorSeverity.Error);
        
        /// <summary>
        /// 同步安全執行操作（無返回值）
        /// </summary>
        void SafeExecute(Action operation, string operationName, ErrorSeverity severity = ErrorSeverity.Error);
        
        /// <summary>
        /// 處理異常
        /// </summary>
        Task HandleExceptionAsync(Exception exception, string context, ErrorSeverity severity = ErrorSeverity.Error);
        
        /// <summary>
        /// 嘗試恢復
        /// </summary>
        Task<bool> TryRecoverAsync(Exception exception, string context);
        
        /// <summary>
        /// 註冊恢復策略
        /// </summary>
        void RegisterRecoveryStrategy<T>(Func<T, string, Task<bool>> recoveryStrategy) where T : Exception;
        
        /// <summary>
        /// 設定預設錯誤處理動作
        /// </summary>
        void SetDefaultErrorHandler(ErrorSeverity severity, Func<Exception, string, Task> handler);
        
        /// <summary>
        /// 獲取錯誤統計
        /// </summary>
        Task<ErrorStatistics> GetStatisticsAsync();
        
        /// <summary>
        /// 重置錯誤統計
        /// </summary>
        Task ResetStatisticsAsync();
    }
    
    /// <summary>
    /// 錯誤嚴重程度
    /// </summary>
    public enum ErrorSeverity
    {
        Info,        // 資訊：記錄日誌，不顯示彈窗
        Warning,     // 警告：記錄日誌，顯示警告彈窗
        Error,       // 錯誤：記錄日誌，顯示錯誤彈窗
        Critical,    // 嚴重：記錄日誌，顯示嚴重錯誤彈窗
        Fatal        // 致命：記錄日誌，準備關閉程式
    }
    
    /// <summary>
    /// 錯誤統計資訊
    /// </summary>
    public class ErrorStatistics
    {
        public int TotalErrors { get; set; }
        public int CriticalErrors { get; set; }
        public int Warnings { get; set; }
        public int RecoveredErrors { get; set; }
        public DateTime LastErrorTime { get; set; }
        public string MostCommonErrorType { get; set; } = "";
        public System.Collections.Generic.Dictionary<string, int> ErrorTypes { get; set; } = new();
    }
    
    /// <summary>
    /// 錯誤上下文資訊
    /// </summary>
    public class ErrorContext
    {
        public string OperationName { get; set; } = "";
        public string Module { get; set; } = "";
        public string UserId { get; set; } = "";
        public string SessionId { get; set; } = "";
        public System.Collections.Generic.Dictionary<string, object> AdditionalData { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 恢復結果
    /// </summary>
    public class RecoveryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public RecoveryStrategy Strategy { get; set; }
        public TimeSpan RecoveryTime { get; set; }
    }
    
    /// <summary>
    /// 恢復策略
    /// </summary>
    public enum RecoveryStrategy
    {
        None,
        Retry,
        Fallback,
        Reset,
        Restart
    }

    /// <summary>
    /// 錯誤發生事件參數
    /// </summary>
    public class ErrorOccurredEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Context { get; }
        public ErrorSeverity Severity { get; }
        public bool Recovered { get; }

        public ErrorOccurredEventArgs(Exception exception, string context, ErrorSeverity severity, bool recovered)
        {
            Exception = exception;
            Context = context;
            Severity = severity;
            Recovered = recovered;
        }
    }
}
