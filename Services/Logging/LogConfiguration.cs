using System;
using System.IO;

namespace RimWorldTranslationTool.Services.Logging
{
    /// <summary>
    /// 日誌配置
    /// </summary>
    public class LogConfiguration
    {
        /// <summary>
        /// 日誌資料夾路徑
        /// </summary>
        public string LogDirectory { get; set; } = "";
        
        /// <summary>
        /// 日誌檔案名稱格式
        /// </summary>
        public string FileNameFormat { get; set; } = "RimWorld_{0:yyyyMMdd}.log";
        
        /// <summary>
        /// 最大日誌檔案大小 (MB)
        /// </summary>
        public long MaxFileSizeMB { get; set; } = 10;
        
        /// <summary>
        /// 保留日誌天數
        /// </summary>
        public int RetentionDays { get; set; } = 30;
        
        /// <summary>
        /// 最小日誌級別
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
        
        /// <summary>
        /// 是否啟用檔案日誌
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;
        
        /// <summary>
        /// 是否啟用控制台日誌
        /// </summary>
        public bool EnableConsoleLogging { get; set; } = true;
        
        /// <summary>
        /// 日誌格式
        /// </summary>
        public string LogFormat { get; set; } = "[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] [{2}] {3}";
        
        /// <summary>
        /// 是否包含執行緒資訊
        /// </summary>
        public bool IncludeThreadInfo { get; set; } = true;
        
        /// <summary>
        /// 是否包含異常堆疊
        /// </summary>
        public bool IncludeExceptionStack { get; set; } = true;
        
        /// <summary>
        /// 自動清理間隔 (小時)
        /// </summary>
        public int AutoCleanupIntervalHours { get; set; } = 24;
        
        /// <summary>
        /// 建立預設配置
        /// </summary>
        public static LogConfiguration CreateDefault()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RimWorldTranslationTool",
                "Logs");
                
            return new LogConfiguration
            {
                LogDirectory = logDir,
                MinimumLevel = LogLevel.Info,
                EnableFileLogging = true,
                EnableConsoleLogging = false, // 生產環境關閉控制台日誌
                MaxFileSizeMB = 10,
                RetentionDays = 30
            };
        }
        
        /// <summary>
        /// 建立開發配置
        /// </summary>
        public static LogConfiguration CreateDevelopment()
        {
            var config = CreateDefault();
            config.MinimumLevel = LogLevel.Debug;
            config.EnableConsoleLogging = true;
            config.IncludeThreadInfo = true;
            config.RetentionDays = 7; // 開發環境保留較少天數
            return config;
        }
        
        /// <summary>
        /// 建立生產配置
        /// </summary>
        public static LogConfiguration CreateProduction()
        {
            var config = CreateDefault();
            config.MinimumLevel = LogLevel.Warning;
            config.EnableConsoleLogging = false;
            config.IncludeThreadInfo = false;
            config.RetentionDays = 90; // 生產環境保留更多天數
            return config;
        }
    }
}
