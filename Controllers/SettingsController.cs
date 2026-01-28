using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using RimWorldTranslationTool.Services.Settings;
using RimWorldTranslationTool.Models;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Dialogs;
using RimWorldTranslationTool.ViewModels;

namespace RimWorldTranslationTool.Controllers
{
    /// <summary>
    /// 設定頁控制器 - 處理設定相關的 UI 邏輯 (透過 ViewModel 與 DialogService)
    /// </summary>
    public class SettingsController
    {
        private readonly ISettingsService _settingsService;
        private readonly SettingsBackupService _backupService;
        private readonly ILoggerService _loggerService;
        private readonly RimWorldTranslationTool.Services.Paths.IPathService _pathService;
        private readonly IDialogService _dialogService;
        private MainViewModel? _viewModel;
        
        public SettingsController(
            ISettingsService settingsService, 
            SettingsBackupService backupService,
            ILoggerService loggerService,
            IDialogService dialogService,
            RimWorldTranslationTool.Services.Paths.IPathService pathService)
        {
            _settingsService = settingsService;
            _backupService = backupService;
            _loggerService = loggerService;
            _dialogService = dialogService;
            _pathService = pathService;
            
            // 訂閱事件
            _settingsService.SettingsLoaded += OnSettingsLoaded;
            _settingsService.SettingsSaved += OnSettingsSaved;
        }

        public void SetViewModel(MainViewModel viewModel)
        {            _viewModel = viewModel;
        }
        
        public string GetCurrentGamePath() => _settingsService.GetCurrentSettings().GamePath;
        
        /// <summary>
        /// 初始化設定
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await _settingsService.LoadSettingsAsync();
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("初始化設定失敗", ex);
                await _dialogService.ShowErrorAsync($"載入設定失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 處理遊戲路徑變更
        /// </summary>
        public async Task HandleGamePathChanged(string newPath)
        {
            try
            {
                // 驗證路徑
                var validation = await _settingsService.ValidateGamePathAsync(newPath);
                
                // 更新 ViewModel 狀態
                UpdateGamePathValidation(validation);
                
                // 保存設定
                if (validation.IsValid || validation.Status == ValidationStatus.Warning)
                {
                    _settingsService.UpdateSetting(s => s.GamePath = newPath);
                }
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("處理遊戲路徑變更失敗", ex);
            }
        }
        
        /// <summary>
        /// 處理瀏覽遊戲路徑
        /// </summary>
        public void HandleBrowseGamePath()
        {
            try
            {
                using var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "選擇 RimWorld 遊戲目錄 (steamapps\\common\\RimWorld)";
                dialog.ShowNewFolderButton = false;
                
                var currentSettings = _settingsService.GetCurrentSettings();
                dialog.SelectedPath = currentSettings.GamePath;
                
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    // 更新 ViewModel
                    if (_viewModel != null)
                    {
                        _viewModel.GamePath = dialog.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("瀏覽遊戲路徑失敗", ex);
                _ = _dialogService.ShowErrorAsync($"瀏覽失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 處理自動檢測 ModsConfig
        /// </summary>
        public async Task HandleAutoDetectModsConfig()
        {
            try
            {
                var detected = await _settingsService.DetectModsConfigAsync();
                
                if (detected)
                {
                    var settings = _settingsService.GetCurrentSettings();
                    if (_viewModel != null)
                    {
                        _viewModel.ModsConfigPath = Path.GetFileName(settings.ModsConfigPath);
                        _viewModel.ModsConfigStatus = "已檢測";
                        _viewModel.ModsConfigStatusColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
                    }
                    
                    await _dialogService.ShowSuccessAsync("已自動檢測到 ModsConfig.xml");
                }
                else
                {
                    await _dialogService.ShowWarningAsync("無法檢測到 ModsConfig.xml，請手動選擇檔案");
                }
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("自動檢測 ModsConfig 失敗", ex);
                await _dialogService.ShowErrorAsync($"檢測失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 處理選擇 ModsConfig 檔案
        /// </summary>
        public void HandleSelectModsConfig()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "XML 檔案 (*.xml)|*.xml",
                    Title = "選擇 ModsConfig.xml 檔案",
                    FileName = "ModsConfig.xml"
                };
                
                if (dialog.ShowDialog() == true)
                {
                    _settingsService.UpdateSetting(s => s.ModsConfigPath = dialog.FileName);
                    
                    // 更新 ViewModel
                    if (_viewModel != null)
                    {
                        _viewModel.ModsConfigPath = Path.GetFileName(dialog.FileName);
                        _viewModel.ModsConfigStatus = "已選擇";
                        _viewModel.ModsConfigStatusColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("選擇 ModsConfig 檔案失敗", ex);
                _ = _dialogService.ShowErrorAsync($"選擇檔案失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 處理手動保存
        /// </summary>
        public async Task HandleManualSave()
        {
            try
            {
                await _settingsService.SaveSettingsAsync(_settingsService.GetCurrentSettings());
                await _dialogService.ShowSuccessAsync("設定已手動儲存");
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("手動儲存失敗", ex);
                await _dialogService.ShowErrorAsync($"手動儲存失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 處理自動儲存設定變更
        /// </summary>
        public void HandleAutoSaveChanged(bool isEnabled)
        {
            try
            {
                _settingsService.EnableAutoSave(isEnabled);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("設定自動儲存失敗", ex);
            }
        }
        
        /// <summary>
        /// 處理匯出設定
        /// </summary>
        public async Task HandleExportSettings()
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "JSON 檔案 (*.json)|*.json",
                    Title = "匯出設定",
                    FileName = $"RimWorldTranslationTool_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                
                if (dialog.ShowDialog() == true)
                {
                    var settings = _settingsService.GetCurrentSettings();
                    await _settingsService.SaveSettingsAsync(settings);
                    
                    var sourceFile = _pathService.GetSettingsFilePath();
                    
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, dialog.FileName, true);
                        await _dialogService.ShowSuccessAsync("設定已匯出");
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("匯出設定失敗", ex);
                await _dialogService.ShowErrorAsync($"匯出設定失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 處理匯入設定
        /// </summary>
        public async Task HandleImportSettings()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "JSON 檔案 (*.json)|*.json",
                    Title = "匯入設定"
                };
                
                if (dialog.ShowDialog() == true)
                {
                    var json = await System.IO.File.ReadAllTextAsync(dialog.FileName);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                    
                    if (settings != null)
                    {
                        _settingsService.UpdateSetting(s => 
                        {
                            s.GamePath = settings.GamePath;
                            s.GameVersion = settings.GameVersion;
                            s.Language = settings.Language;
                            s.ModsConfigPath = settings.ModsConfigPath;
                        });
                        
                        await _dialogService.ShowSuccessAsync("設定已匯入");
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("匯入設定失敗", ex);
                await _dialogService.ShowErrorAsync($"匯入設定失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 處理重設設定
        /// </summary>
        public async Task HandleResetSettings()
        {
            var result = await _dialogService.ShowConfirmationAsync("確定要重設所有設定為預設值嗎？此操作無法復原。");
                
            if (result)
            {
                try
                {
                    var defaultSettings = new AppSettings();
                    _settingsService.UpdateSetting(s => 
                    {
                        s.GamePath = defaultSettings.GamePath;
                        s.GameVersion = defaultSettings.GameVersion;
                        s.Language = defaultSettings.Language;
                        s.ModsConfigPath = defaultSettings.ModsConfigPath;
                    });
                    
                    await _dialogService.ShowSuccessAsync("設定已重設為預設值");
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync("重設設定失敗", ex);
                    await _dialogService.ShowErrorAsync($"重設設定失敗: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 更新遊戲路徑驗證 ViewModel
        /// </summary>
        private void UpdateGamePathValidation(ValidationResult validation)
        {
            if (_viewModel == null) return;

            try
            {
                _viewModel.GamePathValidationIcon = validation.Status switch
                {
                    ValidationStatus.Valid => "✓",
                    ValidationStatus.Warning => "⚠️",
                    ValidationStatus.Error => "✗",
                    _ => ""
                };

                _viewModel.GamePathValidationColor = validation.Status switch
                {
                    ValidationStatus.Valid => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
                    ValidationStatus.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),
                    ValidationStatus.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128))
                };

                _viewModel.GamePathValidationMessage = validation.Message;
                _viewModel.IsGamePathValid = (validation.Status == ValidationStatus.Valid);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("更新遊戲路徑驗證 ViewModel 失敗", ex);
            }
        }
        
        /// <summary>
        /// 從設定更新 ViewModel
        /// </summary>
        private void UpdateViewModelFromSettings(AppSettings settings)
        {
            if (_viewModel == null) return;

            try
            {
                _viewModel.GamePath = settings.GamePath;
                _viewModel.SelectedGameVersion = settings.GameVersion;
                _viewModel.SelectedLanguage = settings.Language;
                
                if (!string.IsNullOrEmpty(settings.ModsConfigPath))
                {
                    _viewModel.ModsConfigPath = Path.GetFileName(settings.ModsConfigPath);
                }
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("更新 ViewModel 失敗", ex);
            }
        }
        
        /// <summary>
        /// 設定載入完成事件處理
        /// </summary>
        private void OnSettingsLoaded(object? sender, SettingsLoadedEventArgs e)
        {
            UpdateViewModelFromSettings(e.Settings);
        }
        
        /// <summary>
        /// 設定保存完成事件處理
        /// </summary>
        private void OnSettingsSaved(object? sender, SettingsSavedEventArgs e)
        {
            _ = _loggerService.LogInfoAsync("設定保存完成");
        }
    }
}
