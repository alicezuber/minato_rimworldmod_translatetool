using System;
using System.Threading.Tasks;
using RimWorldTranslationTool.Models;

namespace RimWorldTranslationTool.Services.Settings
{
    /// <summary>
    /// 設定服務介面
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 事件：設定載入完成
        /// </summary>
        event EventHandler<SettingsLoadedEventArgs>? SettingsLoaded;
        
        /// <summary>
        /// 事件：設定保存完成
        /// </summary>
        event EventHandler<SettingsSavedEventArgs>? SettingsSaved;
        
        /// <summary>
        /// 載入設定
        /// </summary>
        Task<AppSettings> LoadSettingsAsync();
        
        /// <summary>
        /// 保存設定
        /// </summary>
        Task SaveSettingsAsync(AppSettings settings);
        
        /// <summary>
        /// 更新設定
        /// </summary>
        void UpdateSetting(Action<AppSettings> updateAction);
        
        /// <summary>
        /// 獲取當前設定
        /// </summary>
        AppSettings GetCurrentSettings();
        
        /// <summary>
        /// 自動檢測 ModsConfig.xml
        /// </summary>
        Task<bool> DetectModsConfigAsync();
        
        /// <summary>
        /// 驗證遊戲路徑
        /// </summary>
        Task<ValidationResult> ValidateGamePathAsync(string path);
        
        /// <summary>
        /// 啟用/禁用自動保存
        /// </summary>
        void EnableAutoSave(bool enable = true);
    }
    
    /// <summary>
    /// 驗證結果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";
        public ValidationStatus Status { get; set; } = ValidationStatus.None;
    }
    
    /// <summary>
    /// 驗證狀態
    /// </summary>
    public enum ValidationStatus
    {
        None,
        Valid,
        Warning,
        Error
    }
}
