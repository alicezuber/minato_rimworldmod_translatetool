using System;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.CrashReporting
{
    /// <summary>
    /// 崩潰報告服務介面
    /// </summary>
    public interface ICrashReportService
    {
        /// <summary>
        /// 是否啟用自動發送
        /// </summary>
        bool IsAutoSendEnabled { get; set; }
        
        /// <summary>
        /// 崩潰報告目錄
        /// </summary>
        string CrashReportDirectory { get; }
        
        /// <summary>
        /// 生成崩潰報告
        /// </summary>
        Task<CrashReport> GenerateCrashReportAsync(Exception exception, string context = "");
        
        /// <summary>
        /// 保存崩潰報告
        /// </summary>
        Task SaveCrashReportAsync(CrashReport report);
        
        /// <summary>
        /// 發送崩潰報告
        /// </summary>
        Task<bool> SendCrashReportAsync(CrashReport report);
        
        /// <summary>
        /// 獲取所有崩潰報告
        /// </summary>
        Task<CrashReport[]> GetAllCrashReportsAsync();
        
        /// <summary>
        /// 清理舊的崩潰報告
        /// </summary>
        Task CleanupOldReportsAsync(int keepDays = 30);
    }
    
    /// <summary>
    /// 崩潰報告
    /// </summary>
    public class CrashReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Exception Exception { get; set; } = null!;
        public string Context { get; set; } = "";
        public SystemInfo SystemInfo { get; set; } = new();
        public ApplicationInfo ApplicationInfo { get; set; } = new();
        public UserActionInfo UserActionInfo { get; set; } = new();
        public string StackTrace { get; set; } = "";
        public string InnerExceptions { get; set; } = "";
        public bool IsHandled { get; set; }
        public string ReportPath { get; set; } = "";
    }
    
    /// <summary>
    /// 系統資訊
    /// </summary>
    public class SystemInfo
    {
        public string OSVersion { get; set; } = "";
        public string OSArchitecture { get; set; } = "";
        public string MachineName { get; set; } = "";
        public string UserName { get; set; } = "";
        public int ProcessorCount { get; set; }
        public long TotalMemory { get; set; }
        public long AvailableMemory { get; set; }
        public string DotNetVersion { get; set; } = "";
        public string CultureName { get; set; } = "";
    }
    
    /// <summary>
    /// 應用程式資訊
    /// </summary>
    public class ApplicationInfo
    {
        public string ApplicationName { get; set; } = "";
        public string Version { get; set; } = "";
        public string BuildDate { get; set; } = "";
        public string StartupTime { get; set; } = "";
        public string CurrentLanguage { get; set; } = "";
        public string Theme { get; set; } = "";
        public bool IsDebugMode { get; set; }
        public string[] LoadedAssemblies { get; set; } = Array.Empty<string>();
    }
    
    /// <summary>
    /// 使用者操作資訊
    /// </summary>
    public class UserActionInfo
    {
        public string LastAction { get; set; } = "";
        public DateTime LastActionTime { get; set; }
        public string[] RecentActions { get; set; } = Array.Empty<string>();
        public string CurrentTab { get; set; } = "";
        public string CurrentOperation { get; set; } = "";
        public bool IsUserInteraction { get; set; }
    }
}
