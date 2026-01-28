using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Logging;

namespace RimWorldTranslationTool
{
    /// <summary>
    /// 設定管理器 - 負責設定的載入、保存和自動保存機制
    /// </summary>
    public class SettingsManager
    {
        private static readonly Lazy<SettingsManager> _instance = new Lazy<SettingsManager>(() => new SettingsManager());
        public static SettingsManager Instance => _instance.Value;

        private AppSettings _currentSettings = new AppSettings();
        private CancellationTokenSource? _saveCts;
        private bool _isLoadingSettings = false;
        private bool _autoSaveEnabled = false;  // 自動保存開關
        private bool _manualSaveMode = false;   // 手動保存模式
        private readonly object _lockObject = new object();
        private readonly ILoggerService _loggerService;

        // 設定檔案路徑
        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "RimWorldTranslationTool", 
            "RimWorldTranslationTool_Settings.json");

        // JSON 序列化選項
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null  // 使用原始屬性名稱，保持向後相容性
        };

        // 事件：設定載入完成
        public event EventHandler<SettingsLoadedEventArgs>? SettingsLoaded;
        
        // 事件：設定保存完成
        public event EventHandler<SettingsSavedEventArgs>? SettingsSaved;

        private SettingsManager() 
        { 
            _loggerService = new Services.Logging.LoggerService();
        }

        /// <summary>
        /// 載入設定
        /// </summary>
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
                if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                if (File.Exists(SettingsFilePath))
                {
                    await _loggerService.LogInfoAsync($"開始載入設定檔案: {SettingsFilePath}");
                    
                    var json = await File.ReadAllTextAsync(SettingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                    
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

        /// <summary>
        /// 觸發自動保存（防抖機制）
        /// </summary>
        public void TriggerAutoSave()
        {
            // 手動保存模式下，不自動保存
            if (_manualSaveMode) return;
            
            if (_isLoadingSettings || !_autoSaveEnabled) return;

            try
            {
                // 取消之前的保存操作
                _saveCts?.Cancel();
                _saveCts = new CancellationTokenSource();

                // 等待 500ms，如果沒有新的變更就保存
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500, _saveCts.Token);
                        await SaveSettingsAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        // 被取消，忽略
                    }
                });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("觸發自動保存失敗", ex);
            }
        }

        /// <summary>
        /// 手動保存設定
        /// </summary>
        public async Task ManualSaveAsync()
        {
            try
            {
                await SaveSettingsAsync();
                await _loggerService.LogInfoAsync("手動保存完成");
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("手動保存失敗", ex);
                throw;
            }
        }

        /// <summary>
        /// 啟用自動保存（延遲啟動）
        /// </summary>
        public void EnableAutoSave(int delaySeconds = 3)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // 等待指定的延遲時間
                    await Task.Delay(delaySeconds * 1000);
                    
                    lock (_lockObject)
                    {
                        if (!_manualSaveMode)
                        {
                            _autoSaveEnabled = true;
                            await _loggerService.LogInfoAsync($"自動保存已啟用（延遲 {delaySeconds} 秒）");
                        }
                        else
                        {
                            await _loggerService.LogInfoAsync("手動保存模式已啟用，不自動保存");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync("啟用自動保存失敗", ex);
                }
            });
        }

        /// <summary>
        /// 啟用手動保存模式（只允許手動保存）
        /// </summary>
        public void EnableManualSaveMode()
        {
            _ = Task.Run(async () =>
            {
                lock (_lockObject)
                {
                    _manualSaveMode = true;
                    _autoSaveEnabled = false;
                    _saveCts?.Cancel();
                }
                await _loggerService.LogInfoAsync("手動保存模式已啟用 - 只允許手動保存");
            });
        }

        /// <summary>
        /// 禁用自動保存
        /// </summary>
        public void DisableAutoSave()
        {
            _ = Task.Run(async () =>
            {
                lock (_lockObject)
                {
                    _autoSaveEnabled = false;
                    _saveCts?.Cancel();
                }
                await _loggerService.LogInfoAsync("自動保存已禁用");
            });
        }

        /// <summary>
        /// 檢查是否為手動保存模式
        /// </summary>
        public bool IsManualSaveMode()
        {
            lock (_lockObject)
            {
                return _manualSaveMode;
            }
        }

        /// <summary>
        /// 立即保存設定
        /// </summary>
        public async Task SaveSettingsAsync(AppSettings? settings = null)
        {
            if (_isLoadingSettings) return;

            var settingsToSave = settings ?? _currentSettings;

            try
            {
                // 確保設定目錄存在
                var settingsDir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                var json = JsonSerializer.Serialize(settingsToSave, JsonOptions);
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

        /// <summary>
        /// 更新設定值
        /// </summary>
        public void UpdateSetting(Action<AppSettings> updateAction)
        {
            if (updateAction == null) return;

            lock (_lockObject)
            {
                updateAction(_currentSettings);
            }

            TriggerAutoSave();
        }

        /// <summary>
        /// 獲取當前設定
        /// </summary>
        public AppSettings GetCurrentSettings()
        {
            lock (_lockObject)
            {
                return _currentSettings;
            }
        }

        /// <summary>
        /// 檢查是否正在載入設定
        /// </summary>
        public bool IsLoadingSettings()
        {
            lock (_lockObject)
            {
                return _isLoadingSettings;
            }
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            _saveCts?.Cancel();
            _saveCts?.Dispose();
        }
    }

    /// <summary>
    /// 設定載入完成事件參數
    /// </summary>
    public class SettingsLoadedEventArgs : EventArgs
    {
        public AppSettings Settings { get; }

        public SettingsLoadedEventArgs(AppSettings settings)
        {
            Settings = settings;
        }
    }

    /// <summary>
    /// 設定保存完成事件參數
    /// </summary>
    public class SettingsSavedEventArgs : EventArgs
    {
        public AppSettings Settings { get; }

        public SettingsSavedEventArgs(AppSettings settings)
        {
            Settings = settings;
        }
    }
}
