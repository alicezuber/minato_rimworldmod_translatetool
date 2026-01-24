using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using RimWorldTranslationTool.Services.Settings;
using RimWorldTranslationTool.Models;

namespace RimWorldTranslationTool.Controllers
{
    /// <summary>
    /// 設定頁控制器 - 處理設定相關的 UI 邏輯
    /// </summary>
    public class SettingsController
    {
        private readonly ISettingsService _settingsService;
        private readonly SettingsBackupService _backupService;
        private readonly MainWindow _mainWindow;
        
        public SettingsController(ISettingsService settingsService, SettingsBackupService backupService, MainWindow mainWindow)
        {
            _settingsService = settingsService;
            _backupService = backupService;
            _mainWindow = mainWindow;
            
            // 訂閱事件
            _settingsService.SettingsLoaded += OnSettingsLoaded;
            _settingsService.SettingsSaved += OnSettingsSaved;
        }
        
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
                Logger.LogError("初始化設定失敗", ex);
                MessageBox.Show($"載入設定失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                
                // 更新 UI 狀態
                UpdateGamePathValidation(validation);
                
                // 保存設定
                if (validation.IsValid || validation.Status == ValidationStatus.Warning)
                {
                    _settingsService.UpdateSetting(s => s.GamePath = newPath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("處理遊戲路徑變更失敗", ex);
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
                    // 更新 UI
                    _mainWindow.GamePathTextBox.Text = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("瀏覽遊戲路徑失敗", ex);
                MessageBox.Show($"瀏覽失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    _mainWindow.ModsConfigPathText.Text = Path.GetFileName(settings.ModsConfigPath);
                    _mainWindow.ModsConfigStatusText.Text = "已檢測";
                    _mainWindow.ModsConfigStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
                    
                    MessageBox.Show("已自動檢測到 ModsConfig.xml", "檢測成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("無法檢測到 ModsConfig.xml，請手動選擇檔案", "檢測失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("自動檢測 ModsConfig 失敗", ex);
                MessageBox.Show($"檢測失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var settings = _settingsService.GetCurrentSettings();
                    _settingsService.UpdateSetting(s => s.ModsConfigPath = dialog.FileName);
                    
                    // 更新 UI
                    _mainWindow.ModsConfigPathText.Text = Path.GetFileName(dialog.FileName);
                    _mainWindow.ModsConfigPathSubText.Text = dialog.FileName;
                    _mainWindow.ModsConfigStatusText.Text = "已選擇";
                    _mainWindow.ModsConfigStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("選擇 ModsConfig 檔案失敗", ex);
                MessageBox.Show($"選擇檔案失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("設定已手動儲存", "儲存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("手動儲存失敗", ex);
                MessageBox.Show($"手動儲存失敗: {ex.Message}", "儲存失敗", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Logger.LogError("設定自動儲存失敗", ex);
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
                    
                    // 複製到匯出位置
                    var sourceFile = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "RimWorldTranslationTool", "RimWorldTranslationTool_Settings.json");
                    
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, dialog.FileName, true);
                        MessageBox.Show("設定已匯出", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("匯出設定失敗", ex);
                MessageBox.Show($"匯出設定失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        
                        // 更新 UI
                        await UpdateUIFromSettings(settings);
                        
                        MessageBox.Show("設定已匯入", "匯入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("匯入設定失敗", ex);
                MessageBox.Show($"匯入設定失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 處理重設設定
        /// </summary>
        public void HandleResetSettings()
        {
            var result = MessageBox.Show(
                "確定要重設所有設定為預設值嗎？此操作無法復原。", 
                "確認重設", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
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
                    
                    // 更新 UI
                    UpdateUIFromSettings(defaultSettings).Wait();
                    
                    MessageBox.Show("設定已重設為預設值", "重設成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError("重設設定失敗", ex);
                    MessageBox.Show($"重設設定失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        /// <summary>
        /// 更新遊戲路徑驗證 UI
        /// </summary>
        private void UpdateGamePathValidation(ValidationResult validation)
        {
            try
            {
                if (_mainWindow.GamePathValidationIcon != null)
                {
                    switch (validation.Status)
                    {
                        case ValidationStatus.Valid:
                            _mainWindow.GamePathValidationIcon.Text = "✓";
                            _mainWindow.GamePathValidationIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
                            _mainWindow.GamePathStatusIcon.Visibility = Visibility.Visible;
                            break;
                        case ValidationStatus.Warning:
                            _mainWindow.GamePathValidationIcon.Text = "⚠️";
                            _mainWindow.GamePathValidationIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
                            _mainWindow.GamePathStatusIcon.Visibility = Visibility.Collapsed;
                            break;
                        case ValidationStatus.Error:
                            _mainWindow.GamePathValidationIcon.Text = "✗";
                            _mainWindow.GamePathValidationIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                            _mainWindow.GamePathStatusIcon.Visibility = Visibility.Collapsed;
                            break;
                        default:
                            _mainWindow.GamePathValidationIcon.Text = "";
                            _mainWindow.GamePathStatusIcon.Visibility = Visibility.Collapsed;
                            break;
                    }
                }
                
                if (_mainWindow.GamePathValidationText != null)
                {
                    _mainWindow.GamePathValidationText.Text = validation.Message;
                    _mainWindow.GamePathValidationText.Foreground = validation.Status switch
                    {
                        ValidationStatus.Valid => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
                        ValidationStatus.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),
                        ValidationStatus.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
                        _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128))
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("更新遊戲路徑驗證 UI 失敗", ex);
            }
        }
        
        /// <summary>
        /// 從設定更新 UI
        /// </summary>
        private async Task UpdateUIFromSettings(AppSettings settings)
        {
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (_mainWindow.GamePathTextBox != null)
                        _mainWindow.GamePathTextBox.Text = settings.GamePath;
                    
                    if (_mainWindow.GameVersionComboBox != null)
                        _mainWindow.GameVersionComboBox.SelectedItem = settings.GameVersion;
                    
                    if (_mainWindow.LanguageComboBox != null)
                        _mainWindow.LanguageComboBox.SelectedItem = settings.Language;
                    
                    if (_mainWindow.ModsConfigPathText != null && !string.IsNullOrEmpty(settings.ModsConfigPath))
                    {
                        _mainWindow.ModsConfigPathText.Text = Path.GetFileName(settings.ModsConfigPath);
                        _mainWindow.ModsConfigPathSubText.Text = settings.ModsConfigPath;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("更新 UI 失敗", ex);
                }
            });
        }
        
        /// <summary>
        /// 設定載入完成事件處理
        /// </summary>
        private async void OnSettingsLoaded(object? sender, SettingsLoadedEventArgs e)
        {
            await UpdateUIFromSettings(e.Settings);
        }
        
        /// <summary>
        /// 設定保存完成事件處理
        /// </summary>
        private void OnSettingsSaved(object? sender, SettingsSavedEventArgs e)
        {
            Logger.Log("設定保存完成");
        }
    }
}
