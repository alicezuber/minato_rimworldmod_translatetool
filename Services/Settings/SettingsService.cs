using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RimWorldTranslationTool.Models;

namespace RimWorldTranslationTool.Services.Settings
{
    /// <summary>
    /// 設定服務實現
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly SettingsManager _settingsManager;
        private readonly SettingsValidationService _validationService;
        
        public SettingsService(SettingsValidationService validationService)
        {
            _settingsManager = SettingsManager.Instance;
            _validationService = validationService;
            
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
                
                // 查找 ModsConfig.xml
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "..", "LocalLow", "Ludeon Studios", "RimWorld by Ludeon Studios", "Config", "ModsConfig.xml");
                
                if (!File.Exists(configPath))
                {
                    // 嘗試遊戲目錄下的 Config
                    configPath = Path.Combine(settings.GamePath, "Config", "ModsConfig.xml");
                }
                
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
                Logger.LogError("檢測 ModsConfig 失敗", ex);
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
