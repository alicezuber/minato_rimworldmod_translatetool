using System;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.EmergencySave
{
    /// <summary>
    /// 緊急儲存服務介面
    /// </summary>
    public interface IEmergencySaveService
    {
        /// <summary>
        /// 緊急儲存所有重要資料
        /// </summary>
        Task<bool> EmergencySaveAllAsync();
        
        /// <summary>
        /// 緊急儲存使用者設定
        /// </summary>
        Task<bool> SaveSettingsAsync();
        
        /// <summary>
        /// 緊急儲存工作進度
        /// </summary>
        Task<bool> SaveWorkInProgressAsync();
        
        /// <summary>
        /// 緊急儲存應用程式狀態
        /// </summary>
        Task<bool> SaveApplicationStateAsync();
        
        /// <summary>
        /// 緊急儲存模組資料
        /// </summary>
        Task<bool> SaveModDataAsync();
        
        /// <summary>
        /// 獲取緊急儲存狀態
        /// </summary>
        Task<EmergencySaveStatus> GetStatusAsync();
        
        /// <summary>
        /// 清理舊的緊急儲存檔案
        /// </summary>
        Task CleanupOldSavesAsync(int keepCount = 5);
        
        /// <summary>
        /// 是否有未儲存的資料
        /// </summary>
        bool HasUnsavedData { get; }
        
        /// <summary>
        /// 最後儲存時間
        /// </summary>
        DateTime LastSaveTime { get; }
    }
    
    /// <summary>
    /// 緊急儲存狀態
    /// </summary>
    public class EmergencySaveStatus
    {
        public bool IsInProgress { get; set; }
        public DateTime LastSaveTime { get; set; }
        public int SaveCount { get; set; }
        public string LastSaveResult { get; set; } = "";
        public bool[] ComponentSaveStatus { get; set; } = Array.Empty<bool>();
        public TimeSpan LastSaveDuration { get; set; }
        public string EmergencySaveDirectory { get; set; } = "";
    }
    
    /// <summary>
    /// 緊急儲存結果
    /// </summary>
    public class EmergencySaveResult
    {
        public bool Success { get; set; }
        public string Component { get; set; } = "";
        public string Message { get; set; } = "";
        public Exception? Exception { get; set; }
        public TimeSpan SaveDuration { get; set; }
        public string SavePath { get; set; } = "";
    }
}
