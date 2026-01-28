using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RimWorldTranslationTool.Models;
using RimWorldTranslationTool.Services.Paths;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.EmergencySave;

namespace RimWorldTranslationTool.Services.Settings
{
    /// <summary>
    /// 設定服務實現
    /// </summary>
    public class SettingsService : ISettingsService, ISavableComponent
    {
        private readonly SettingsManager _settingsManager;
        private readonly SettingsValidationService _validationService;
        private readonly IPathService _pathService;
        private readonly ILoggerService _loggerService;
        private readonly IEmergencySaveService _emergencySaveService;
        
        public string ComponentName => "Settings";

        public SettingsService(
            SettingsValidationService validationService, 
            IPathService pathService,
            IEmergencySaveService emergencySaveService)
        {
            _settingsManager = SettingsManager.Instance;
            _validationService = validationService;
            _pathService = pathService;
            _emergencySaveService = emergencySaveService;
            _loggerService = new LoggerService();
            
            // 註冊到緊急儲存服務
            _emergencySaveService.RegisterComponent(this);
            
            // 轉發事件
            _settingsManager.SettingsLoaded += (s, e) => SettingsLoaded?.Invoke(this, e);
            _settingsManager.SettingsSaved += (s, e) => SettingsSaved?.Invoke(this, e);
        }
        
        public async Task<AppSettings> LoadSettingsAsync()
        {
            return await _settingsManager.LoadSettingsAsync();
        }
        
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            await _settingsManager.SaveSettingsAsync(settings);
        }

        public async Task SaveAsync()
        {
            await SaveSettingsAsync(GetCurrentSettings());
        }
        
        public void UpdateSetting(Action<AppSettings> updateAction)
        {
            _settingsManager.UpdateSetting(updateAction);
        }
        
        public AppSettings GetCurrentSettings()
        {
            return _settingsManager.GetCurrentSettings();
        }
        
        public async Task<bool> DetectModsConfigAsync()
        {
            try
            {
                var settings = GetCurrentSettings();
                
                // 檢查遊戲路徑是否設定
                if (string.IsNullOrEmpty(settings.GamePath))
                {
                    return false;
                }
                
                // 使用 PathService 獲取 ModsConfig.xml 路徑
                string configPath = _pathService.GetModsConfigPath();
                
                if (File.Exists(configPath))
                {
                    // 更新 ModsConfig 路徑
                    UpdateSetting(s => s.ModsConfigPath = configPath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("檢測 ModsConfig 失敗", ex);
                return false;
            }
        }
        
        public async Task<ValidationResult> ValidateGamePathAsync(string path)
        {
            return await _validationService.ValidateGamePathAsync(path);
        }
        
        public void EnableAutoSave(bool enable = true)
        {
            if (enable)
            {
                _settingsManager.EnableAutoSave();
            }
            else
            {
                _settingsManager.DisableAutoSave();
            }
        }
        
        public event EventHandler<SettingsLoadedEventArgs>? SettingsLoaded;
        public event EventHandler<SettingsSavedEventArgs>? SettingsSaved;
    }
}
