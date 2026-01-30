using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RimWorldTranslationTool.Models;
using RimWorldTranslationTool.Services.EmergencySave;
using RimWorldTranslationTool.Services.Infrastructure;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool.Services.Settings
{
    /// <summary>
    /// 設定服務實現 - 整合了原本的 SettingsManager 邏輯
    /// </summary>
    public class SettingsService : ISettingsService, ISavableComponent, IDisposable
    {
        private readonly SettingsValidationService _validationService;
        private readonly IPathService _pathService;
        private readonly ILoggerService _loggerService;
        private readonly IEmergencySaveService _emergencySaveService;
        private readonly object _lockObject = new object();

        private AppSettings _currentSettings = new AppSettings();
        private CancellationTokenSource? _saveCts;
        private bool _isLoadingSettings = false;
        private bool _autoSaveEnabled = false;
        private bool _manualSaveMode = false;
        private bool _disposed = false;

        public SettingsService(
            SettingsValidationService validationService,
            IPathService pathService,
            IEmergencySaveService emergencySaveService,
            ILoggerService loggerService)
        {
            _validationService = validationService;
            _pathService = pathService;
            _emergencySaveService = emergencySaveService;
            _loggerService = loggerService;

            // 註冊到緊急儲存服務
            _emergencySaveService.RegisterComponent(this);
        }

        public event EventHandler<SettingsLoadedEventArgs>? SettingsLoaded;
        public event EventHandler<SettingsSavedEventArgs>? SettingsSaved;

        public string ComponentName => "Settings";

        private string SettingsFilePath => _pathService.GetSettingsFilePath();

        public async Task<AppSettings> LoadSettingsAsync()
        {
            lock (_lockObject)
            {
                if (_isLoadingSettings)
                    return _currentSettings;

                _isLoadingSettings = true;
            }

            try
            {
                // 確保設定目錄存在
                var settingsDir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(settingsDir))
                {
                    _pathService.EnsureDirectoryExists(settingsDir);
                }

                if (File.Exists(SettingsFilePath))
                {
                    await _loggerService.LogInfoAsync($"開始載入設定檔案: {SettingsFilePath}");

                    var json = await File.ReadAllTextAsync(SettingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonConfiguration.DefaultOptions) ?? new AppSettings();

                    _currentSettings = loadedSettings;
                    await _loggerService.LogInfoAsync($"設定載入成功 - GamePath: {_currentSettings.GamePath}, Version: {_currentSettings.GameVersion}");
                }
                else
                {
                    await _loggerService.LogInfoAsync($"設定檔案不存在，使用預設值: {SettingsFilePath}");
                    _currentSettings = new AppSettings();
                }

                // 觸發載入完成事件
                SettingsLoaded?.Invoke(this, new SettingsLoadedEventArgs(_currentSettings));

                return _currentSettings;
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"載入設定失敗，檔案路徑: {SettingsFilePath}", ex);
                _currentSettings = new AppSettings();
                return _currentSettings;
            }
            finally
            {
                lock (_lockObject)
                {
                    _isLoadingSettings = false;
                }
            }
        }

        public async Task SaveSettingsAsync(AppSettings? settings = null)
        {
            if (_isLoadingSettings) return;

            var settingsToSave = settings ?? _currentSettings;

            try
            {
                // 確保設定目錄存在
                var settingsDir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(settingsDir))
                {
                    _pathService.EnsureDirectoryExists(settingsDir);
                }

                var json = JsonSerializer.Serialize(settingsToSave, JsonConfiguration.DefaultOptions);
                await File.WriteAllTextAsync(SettingsFilePath, json);

                await _loggerService.LogInfoAsync($"設定已儲存到: {SettingsFilePath}");

                // 觸發保存完成事件
                SettingsSaved?.Invoke(this, new SettingsSavedEventArgs(settingsToSave));
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"儲存設定失敗，檔案路徑: {SettingsFilePath}", ex);
                throw;
            }
        }

        // 為了相容 ISettingsService 介面
        async Task ISettingsService.SaveSettingsAsync(AppSettings settings)
        {
            await SaveSettingsAsync(settings);
        }

        public async Task SaveAsync()
        {
            await SaveSettingsAsync();
        }

        public void UpdateSetting(Action<AppSettings> updateAction)
        {
            if (updateAction == null) return;

            lock (_lockObject)
            {
                updateAction(_currentSettings);
            }

            TriggerAutoSave();
        }

        public AppSettings GetCurrentSettings()
        {
            lock (_lockObject)
            {
                return _currentSettings;
            }
        }

        public void TriggerAutoSave()
        {
            if (_manualSaveMode || _isLoadingSettings || !_autoSaveEnabled) return;

            try
            {
                _saveCts?.Cancel();
                _saveCts = new CancellationTokenSource();

                var token = _saveCts.Token;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500, token);
                        await SaveSettingsAsync();
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        await _loggerService.LogErrorAsync("自動保存背景任務失敗", ex);
                    }
                }, token);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("觸發自動保存失敗", ex);
            }
        }

        public void EnableAutoSave(bool enable = true)
        {
            if (enable)
            {
                lock (_lockObject)
                {
                    if (!_manualSaveMode)
                    {
                        _autoSaveEnabled = true;
                    }
                }
                _ = _loggerService.LogInfoAsync(_manualSaveMode ? "手動保存模式已啟用，不自動保存" : "自動保存已啟用");
            }
            else
            {
                lock (_lockObject)
                {
                    _autoSaveEnabled = false;
                    _saveCts?.Cancel();
                }
                _ = _loggerService.LogInfoAsync("自動保存已禁用");
            }
        }

        public void SetManualSaveMode(bool manualMode)
        {
            lock (_lockObject)
            {
                _manualSaveMode = manualMode;
                if (manualMode)
                {
                    _autoSaveEnabled = false;
                    _saveCts?.Cancel();
                }
            }
            _ = _loggerService.LogInfoAsync(manualMode ? "手動保存模式已啟用" : "手動保存模式已關閉");
        }

        public async Task<bool> DetectModsConfigAsync()
        {
            try
            {
                string configPath = _pathService.GetModsConfigPath();
                if (File.Exists(configPath))
                {
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _saveCts?.Cancel();
                _saveCts?.Dispose();
            }
        }
    }
}
