using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.Json;
using System.Windows.Data;

namespace RimWorldTranslationTool
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }

    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        private List<ModInfo> _mods = new List<ModInfo>();
        private ModInfo? _selectedMod;
        private Dictionary<string, List<ModInfo>> _translationMappings = new();
        private string _selectedGameVersion = "1.6";
        private string _modsConfigPath = "";
        private AppSettings _settings = new AppSettings();
        private const string SettingsFileName = "RimWorldTranslationTool_Settings.json";
        
        // 簡化的路徑設定 - 只需要遊戲路徑
        private string _gamePath = "";
        
        // 自動推導的路徑
        private string WorkshopPath => !string.IsNullOrEmpty(_gamePath) ? 
            Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(_gamePath)) ?? "", "workshop", "content", "294100") : "";
        
        private string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "..", "LocalLow", "Ludeon Studios", "RimWorld by Ludeon Studios");
        
        // 模組管理相關
        private List<ModInfo> _modPool = new List<ModInfo>();
        private List<ModInfo> _enabledMods = new List<ModInfo>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            
            // 測試 i18n 功能
            TestI18n();
            
            // 載入設定
            LoadSettings();
            
            // 初始化版本選項
            InitializeGameVersions();
            
            // 初始化語言選項
            InitializeLanguages();
            
            // 設置選擇變更事件
            ModsDataGrid.SelectionChanged += ModsDataGrid_SelectionChanged;
            
            // 延遲更新 UI，確保控制項已初始化
            this.Loaded += MainWindow_Loaded;
        }
        
        private void TestI18n()
        {
            Logger.Log("=== i18n 測試開始 (原生 .NET 實現) ===");
            
            try
            {
                // 1. 測試 LocalizationService
                Logger.Log("1. 測試 LocalizationService");
                
                // 測試中文
                LocalizationService.Instance.SetLanguage("zh-TW");
                var zhTitle = LocalizationService.Instance.WindowTitle;
                var zhSettings = LocalizationService.Instance.TabSettings;
                Logger.Log($"   zh-TW WindowTitle: '{zhTitle}'");
                Logger.Log($"   zh-TW TabSettings: '{zhSettings}'");
                
                // 測試英文
                LocalizationService.Instance.SetLanguage("en-US");
                var enTitle = LocalizationService.Instance.WindowTitle;
                var enSettings = LocalizationService.Instance.TabSettings;
                Logger.Log($"   en-US WindowTitle: '{enTitle}'");
                Logger.Log($"   en-US TabSettings: '{enSettings}'");
                
                // 2. 檢查衛星組件
                Logger.Log("2. 檢查衛星組件");
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var location = assembly.Location ?? throw new InvalidOperationException("無法取得程式位置");
                Logger.Log($"   主程式位置: {location}");
                
                var directory = Path.GetDirectoryName(location) ?? throw new InvalidOperationException("無法取得程式目錄");
                var zhTWAssemblyPath = Path.Combine(directory, "zh-TW", "RimWorldTranslationTool.resources.dll");
                var enUSAssemblyPath = Path.Combine(directory, "en-US", "RimWorldTranslationTool.resources.dll");
                
                Logger.Log($"   zh-TW 衛星組件存在: {File.Exists(zhTWAssemblyPath)}");
                Logger.Log($"   en-US 衛星組件存在: {File.Exists(enUSAssemblyPath)}");
                
                // 恢復預設語言
                LocalizationService.Instance.CurrentCulture = System.Globalization.CultureInfo.CurrentUICulture;
                
                // 3. 檢查 UI 綁定
                Logger.Log("3. 檢查 UI 綁定");
                Logger.Log($"   當前語言: {LocalizationService.Instance.CurrentCulture.Name}");
                Logger.Log($"   視窗標題: {LocalizationService.Instance.WindowTitle}");
                
                // 檢查是否載入成功
                var testTitle = LocalizationService.Instance.WindowTitle;
                if (testTitle.Contains("WindowTitle") || testTitle.Contains("["))
                {
                    Logger.LogWarning("資源檔案載入失敗 - 顯示的是 key 而不是值");
                    Logger.Log("   可能原因:");
                    Logger.Log("   1. 資源檔案沒有正確編譯成衛星組件");
                    Logger.Log("   2. ResourceManager 找不到資源");
                    Logger.Log("   3. 資源檔案中的 key 不匹配");
                }
                else
                {
                    Logger.LogSuccess("資源檔案載入成功");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("i18n 測試發生錯誤", ex);
            }
            
            Logger.Log("=== i18n 測試完成 ===");
            Logger.Log($"日誌檔案位置: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n_test.log")}");
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 一次性更新所有 UI 元素
            UpdateAllUI();
            
            // 不要在啟動時立即載入 ModsConfig.xml
            // 等待用戶掃描模組後再載入
            System.Diagnostics.Debug.WriteLine("程式啟動完成，等待用戶掃描模組...");
        }
        
        private void UpdateAllUI()
        {
            // 更新路徑顯示
            UpdatePathDisplay();
            
            // 更新 ModsConfigPath 顯示
            if (ModsConfigPathText != null && !string.IsNullOrEmpty(_modsConfigPath))
            {
                ModsConfigPathText.Text = Path.GetFileName(_modsConfigPath);
            }
            
            // 更新遊戲版本選擇
            if (GameVersionComboBox != null && !string.IsNullOrEmpty(_selectedGameVersion))
            {
                GameVersionComboBox.SelectedItem = _selectedGameVersion;
            }
        }
        
        private void InitializeGameVersions()
        {
            var versions = new[] { 
                "0.14", "0.15", "0.16", "0.17", "0.18", "0.19", 
                "1.0", "1.1", "1.2", "1.3", "1.4", "1.5", "1.6" 
            };
            GameVersionComboBox.ItemsSource = versions;
            GameVersionComboBox.SelectedItem = _selectedGameVersion;
        }
        
        private void InitializeLanguages()
        {
            var languages = new[]
            {
                new { Code = "zh-TW", Name = "繁體中文" },
                new { Code = "en-US", Name = "English" }
            };
            
            LanguageComboBox.ItemsSource = languages;
            LanguageComboBox.DisplayMemberPath = "Name";
            LanguageComboBox.SelectedValuePath = "Code";
            
            // 設定當前選中的語言
            var currentLanguage = LocalizationService.Instance.CurrentCulture.Name;
            LanguageComboBox.SelectedItem = languages.FirstOrDefault(l => l.Code == currentLanguage);
        }
        
        private void LoadSettings()
        {
            _isLoadingSettings = true;  // 開始載入設定
            
            try
            {
                if (File.Exists(SettingsFileName))
                {
                    var json = File.ReadAllText(SettingsFileName);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    
                    // 調試信息
                    System.Diagnostics.Debug.WriteLine($"=== 載入設定開始 ===");
                    System.Diagnostics.Debug.WriteLine($"ModsDirectory: {_settings.ModsDirectory}");
                    System.Diagnostics.Debug.WriteLine($"ModsConfigPath: {_settings.ModsConfigPath}");
                    
                    System.Diagnostics.Debug.WriteLine($"載入設定 - WorkshopPath: {_settings.WorkshopPath}");
                    System.Diagnostics.Debug.WriteLine($"載入設定 - GamePath: {_settings.GamePath}");
                    System.Diagnostics.Debug.WriteLine($"載入設定 - ConfigPath: {_settings.ConfigPath}");
                    System.Diagnostics.Debug.WriteLine($"載入設定 - ModsConfigPath: {_settings.ModsConfigPath}");
                    System.Diagnostics.Debug.WriteLine($"載入設定 - GameVersion: {_settings.GameVersion}");
                    System.Diagnostics.Debug.WriteLine($"載入設定 - Language: {_settings.Language}");
                    
                    // 載入新設定
                    GamePath = _settings.GamePath;
                    _modsConfigPath = _settings.ModsConfigPath;
                    _selectedGameVersion = _settings.GameVersion;
                    
                    // 載入語言設定
                    if (!string.IsNullOrEmpty(_settings.Language))
                    {
                        LocalizationService.Instance.SetLanguage(_settings.Language);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"=== 載入設定完成 ===");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("設定檔案不存在，使用預設值");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入設定失敗：{ex.Message}");
                ShowErrorWithCopy(LocalizationManager.GetString("LoadSettingsFailed_Title"), 
                                 LocalizationManager.GetString("LoadSettingsFailed_Message"), ex.ToString());
            }
            finally
            {
                _isLoadingSettings = false;  // 完成載入設定
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                _settings.GamePath = GamePath;
                _settings.ModsConfigPath = _modsConfigPath;
                _settings.GameVersion = _selectedGameVersion;
                _settings.Language = LocalizationService.Instance.CurrentCulture.Name;
                
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFileName, json);
                
                // 調試信息
                System.Diagnostics.Debug.WriteLine($"儲存設定 - WorkshopPath: {_settings.WorkshopPath}");
                System.Diagnostics.Debug.WriteLine($"儲存設定 - GamePath: {_settings.GamePath}");
                System.Diagnostics.Debug.WriteLine($"儲存設定 - ConfigPath: {_settings.ConfigPath}");
                System.Diagnostics.Debug.WriteLine($"儲存設定 - ModsConfigPath: {_settings.ModsConfigPath}");
                System.Diagnostics.Debug.WriteLine($"儲存設定 - GameVersion: {_settings.GameVersion}");
                System.Diagnostics.Debug.WriteLine($"儲存設定 - Language: {_settings.Language}");
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy(LocalizationManager.GetString("SaveSettingsFailed_Title"), 
                                 LocalizationManager.GetString("SaveSettingsFailed_Message"), ex.ToString());
            }
        }

        // 簡化的屬性 - 只需要遊戲路徑
        public string GamePath 
        { 
            get => _gamePath;
            set
            {
                if (_gamePath != value)
                {
                    _gamePath = value;
                    OnPropertyChanged(nameof(GamePath));
                    OnPropertyChanged(nameof(FolderPath));
                    OnPropertyChanged(nameof(WorkshopPath)); // 通知自動推導的路徑也更新
                    OnPropertyChanged(nameof(ConfigPath));
                    
                    // 只有在不是載入設定時才自動保存
                    if (!_isLoadingSettings)
                    {
                        SaveSettings();
                    }
                }
            }
        }
        
        public string FolderPath 
        { 
            get => _gamePath; // 現在 FolderPath 指向遊戲路徑
            set
            {
                if (_gamePath != value)
                {
                    _gamePath = value;
                    OnPropertyChanged(nameof(FolderPath));
                    OnPropertyChanged(nameof(GamePath));
                    OnPropertyChanged(nameof(WorkshopPath)); // 通知自動推導的路徑也更新
                    OnPropertyChanged(nameof(ConfigPath));
                    
                    // 只有在不是載入設定時才自動保存
                    if (!_isLoadingSettings)
                    {
                        SaveSettings();
                    }
                }
            }
        }

        public ModInfo? SelectedMod 
        { 
            get => _selectedMod;
            set
            {
                if (_selectedMod != value)
                {
                    _selectedMod = value;
                    OnPropertyChanged(nameof(SelectedMod));
                    UpdatePreviewPanel();
                }
            }
        }

        private bool _isLoadingSettings = false;

        private void GameVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameVersionComboBox.SelectedItem is string selectedVersion)
            {
                _selectedGameVersion = selectedVersion;
                SaveSettings(); // 自動儲存設定
                RefreshVersionCompatibility();
            }
        }
        
        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem != null)
            {
                var selectedItem = LanguageComboBox.SelectedItem;
                var codeProperty = selectedItem.GetType().GetProperty("Code");
                var languageCode = codeProperty?.GetValue(selectedItem) as string;
                
                if (!string.IsNullOrEmpty(languageCode))
                {
                    LocalizationService.Instance.SetLanguage(languageCode);
                    SaveSettings(); // 自動儲存設定
                }
            }
        }
        
        private void RefreshVersionCompatibility()
        {
            foreach (var mod in _mods)
            {
                mod.IsVersionCompatible = IsVersionCompatible(mod.SupportedVersions);
            }
            
            // 刷新 DataGrid 顯示
            ModsDataGrid.Items.Refresh();
            
            // 更新預覽面板
            if (SelectedMod != null)
            {
                UpdatePreviewPanel();
            }
        }
        
        private bool IsVersionCompatible(string supportedVersions)
        {
            if (string.IsNullOrEmpty(supportedVersions))
                return false;
                
            var versions = supportedVersions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .ToArray();
            
            return versions.Contains(_selectedGameVersion);
        }

        private void GamePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 避免無限遞迴：只有當值真正改變時才更新
                if (textBox.Text != GamePath)
                {
                    GamePath = textBox.Text;
                }
            }
        }

        private void BrowseGameButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "選擇 RimWorld 遊戲目錄 (steamapps\\common\\RimWorld)";
            dialog.ShowNewFolderButton = false;
            dialog.SelectedPath = GamePath;
            
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                GamePath = dialog.SelectedPath;
            }
        }
        
        private void AutoDetectPaths_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var detectedGamePath = DetectRimWorldGamePath();
                
                if (detectedGamePath != null)
                {
                    GamePath = detectedGamePath;
                    ShowInfoMessage("檢測成功", "已自動檢測到 RimWorld 遊戲目錄");
                }
                else
                {
                    ShowInfoMessage("檢測失敗", "無法自動檢測到 RimWorld 路徑，請手動設定");
                }
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("自動檢測失敗", "自動檢測 RimWorld 路徑時發生錯誤", ex.ToString());
            }
        }
        
        private string? DetectRimWorldGamePath()
        {
            try
            {
                // 檢測 Steam 安裝目錄
                var steamPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                    @"C:\Steam",
                    @"D:\Steam",
                    @"E:\Steam"
                };
                
                foreach (var steamPath in steamPaths)
                {
                    if (Directory.Exists(steamPath))
                    {
                        // 檢測遊戲路徑
                        var gamePathCandidate = Path.Combine(steamPath, "steamapps", "common", "RimWorld");
                        if (Directory.Exists(gamePathCandidate))
                        {
                            return gamePathCandidate;
                        }
                        
                        break;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"自動檢測路徑時發生錯誤: {ex.Message}");
                return null;
            }
        }

        private void ModsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModsDataGrid.SelectedItem is ModInfo selectedMod)
            {
                SelectedMod = selectedMod;
            }
        }
        
        private void UpdatePreviewPanel()
        {
            if (SelectedMod == null)
            {
                ModInfoPanel.Visibility = Visibility.Collapsed;
                TranslationPatchesTitle.Visibility = Visibility.Collapsed;
                TranslationPatchesList.Visibility = Visibility.Collapsed;
                EmptyStateText.Visibility = Visibility.Visible;
                PreviewImage.Source = null;
                return;
            }
            
            ModInfoPanel.Visibility = Visibility.Visible;
            EmptyStateText.Visibility = Visibility.Collapsed;
            
            // 更新模組資訊
            ModNameText.Text = SelectedMod.Name;
            ModAuthorText.Text = $"作者: {SelectedMod.Author}";
            ModPackageIdText.Text = $"PackageId: {SelectedMod.PackageId}";
            ModVersionText.Text = $"版本: {SelectedMod.SupportedVersions}";
            ModFolderText.Text = $"資料夾: {SelectedMod.FolderName}";
            
            // 更新翻譯狀態
            ChineseTraditionalText.Text = $"繁體中文: {SelectedMod.HasChineseTraditional}";
            ChineseSimplifiedText.Text = $"簡體中文: {SelectedMod.HasChineseSimplified}";
            TranslationPatchText.Text = $"翻譯補丁: {SelectedMod.HasTranslationPatch}";
            CanTranslateText.Text = $"可翻譯: {SelectedMod.CanTranslate}";
            
            // 更新預覽圖片
            if (SelectedMod.PreviewImage != null)
            {
                PreviewImage.Source = SelectedMod.PreviewImage;
            }
            else
            {
                PreviewImage.Source = null;
            }
            
            // 更新翻譯補丁列表
            UpdateTranslationPatchesList();
        }
        
        private void UpdateTranslationPatchesList()
        {
            if (SelectedMod == null || !_translationMappings.ContainsKey(SelectedMod.PackageId))
            {
                TranslationPatchesTitle.Visibility = Visibility.Collapsed;
                TranslationPatchesList.Visibility = Visibility.Collapsed;
                return;
            }
            
            var patches = _translationMappings[SelectedMod.PackageId];
            if (patches.Count == 0)
            {
                TranslationPatchesTitle.Visibility = Visibility.Collapsed;
                TranslationPatchesList.Visibility = Visibility.Collapsed;
                return;
            }
            
            TranslationPatchesTitle.Visibility = Visibility.Visible;
            TranslationPatchesList.Visibility = Visibility.Visible;
            TranslationPatchesList.Children.Clear();
            
            foreach (var patch in patches)
            {
                var patchPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
                
                var nameText = new TextBlock 
                { 
                    Text = patch.Name,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                
                var authorText = new TextBlock 
                { 
                    Text = $"作者: {patch.Author}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(0, 0, 0, 1)
                };
                
                var versionText = new TextBlock 
                { 
                    Text = $"版本: {patch.SupportedVersions}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };
                
                patchPanel.Children.Add(nameText);
                patchPanel.Children.Add(authorText);
                patchPanel.Children.Add(versionText);
                
                // 添加點擊事件
                var border = new Border 
                { 
                    Background = new SolidColorBrush(Color.FromArgb(50, 0, 120, 215)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                border.Child = patchPanel;
                
                border.MouseLeftButtonDown += (s, e) => 
                {
                    // 選擇翻譯補丁模組
                    var patchMod = _mods.FirstOrDefault(m => m.PackageId == patch.PackageId);
                    if (patchMod != null)
                    {
                        ModsDataGrid.SelectedItem = patchMod;
                    }
                };
                
                TranslationPatchesList.Children.Add(border);
            }
        }
        
        private void UpdatePathDisplay()
        {
            // 更新遊戲路徑文字框顯示
            if (GamePathTextBox != null && GamePathTextBox.Text != GamePath)
            {
                GamePathTextBox.TextChanged -= GamePathTextBox_TextChanged;
                GamePathTextBox.Text = GamePath;
                GamePathTextBox.TextChanged += GamePathTextBox_TextChanged;
            }
            
            // 同時更新 ModsConfigPath 顯示和狀態
            if (ModsConfigPathText != null && !string.IsNullOrEmpty(_modsConfigPath))
            {
                var fileName = Path.GetFileName(_modsConfigPath);
                if (ModsConfigPathText.Text != fileName)
                {
                    ModsConfigPathText.Text = fileName;
                }
                
                // 更新狀態顯示
                if (ModsConfigStatusText != null)
                {
                    if (File.Exists(_modsConfigPath))
                    {
                        ModsConfigStatusText.Text = "✅";
                        ModsConfigStatusText.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        ModsConfigStatusText.Text = "⚠️ 檔案不存在";
                        ModsConfigStatusText.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                }
            }
            else if (ModsConfigStatusText != null)
            {
                ModsConfigStatusText.Text = "";
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValidModDirectory(FolderPath))
            {
                string errorMsg = "請確保路徑設定正確：\n\n" +
                                 "🎯 遊戲路徑：應指向 RimWorld 遊戲目錄 (steamapps\\common\\RimWorld)\n" +
                                 "📦 工作坊路徑：應指向 Steam 工作坊 (steamapps\\workshop\\content\\294100)\n\n" +
                                 "至少需要一個路徑包含有效的模組資料夾。";
                ShowErrorWithCopy("路徑驗證失敗", "無法找到有效的模組目錄", errorMsg);
                return;
            }

            await ScanModsAsync();
        }

        private bool IsValidModDirectory(string path)
        {
            // 調試：輸出當前路徑狀態
            System.Diagnostics.Debug.WriteLine("=== 路徑驗證開始 ===");
            System.Diagnostics.Debug.WriteLine($"輸入路徑: '{path}'");
            System.Diagnostics.Debug.WriteLine($"遊戲路徑: '{GamePath}'");
            System.Diagnostics.Debug.WriteLine($"工作坊路徑: '{WorkshopPath}'");
            System.Diagnostics.Debug.WriteLine($"設定路徑: '{ConfigPath}'");
            
            // 檢查遊戲路徑是否有效
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($"❌ 遊戲路徑無效: '{path}'");
                System.Diagnostics.Debug.WriteLine($"  路徑為空: {string.IsNullOrEmpty(path)}");
                System.Diagnostics.Debug.WriteLine($"  目錄存在: {(!string.IsNullOrEmpty(path) && Directory.Exists(path))}");
                return false;
            }
            
            // 檢查是否包含至少一個有效的模組位置
            bool hasValidModLocation = false;
            
            // 1. 檢查 Mods 資料夾
            var modsPath = Path.Combine(path, "Mods");
            System.Diagnostics.Debug.WriteLine($"檢查 Mods 資料夾: '{modsPath}'");
            if (Directory.Exists(modsPath))
            {
                try
                {
                    var modsDirs = Directory.GetDirectories(modsPath);
                    System.Diagnostics.Debug.WriteLine($"  找到 {modsDirs.Length} 個資料夾");
                    
                    var hasMods = modsDirs
                        .Any(dir => File.Exists(Path.Combine(dir, "About", "About.xml")));
                    System.Diagnostics.Debug.WriteLine($"  有有效模組: {hasMods}");
                    if (hasMods) hasValidModLocation = true;
                    
                    // 詳細列出前5個資料夾
                    foreach (var dir in modsDirs.Take(5))
                    {
                        var hasAbout = File.Exists(Path.Combine(dir, "About", "About.xml"));
                        System.Diagnostics.Debug.WriteLine($"    {Path.GetFileName(dir)}: {hasAbout}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"  檢查 Mods 資料夾時發生錯誤: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("  Mods 資料夾不存在");
            }
            
            // 2. 檢查 Data 資料夾（核心模組）
            var dataPath = Path.Combine(path, "Data");
            System.Diagnostics.Debug.WriteLine($"檢查 Data 資料夾: '{dataPath}'");
            if (Directory.Exists(dataPath))
            {
                try
                {
                    var dataDirs = Directory.GetDirectories(dataPath);
                    System.Diagnostics.Debug.WriteLine($"  找到 {dataDirs.Length} 個資料夾");
                    
                    var hasCoreMods = dataDirs
                        .Any(dir => File.Exists(Path.Combine(dir, "About.xml")));
                    System.Diagnostics.Debug.WriteLine($"  有核心模組: {hasCoreMods}");
                    if (hasCoreMods) hasValidModLocation = true;
                    
                    // 詳細列出前5個資料夾
                    foreach (var dir in dataDirs.Take(5))
                    {
                        var hasAbout = File.Exists(Path.Combine(dir, "About.xml"));
                        System.Diagnostics.Debug.WriteLine($"    {Path.GetFileName(dir)}: {hasAbout}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"  檢查 Data 資料夾時發生錯誤: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("  Data 資料夾不存在");
            }
            
            // 3. 如果有設定工作坊路徑，也檢查工作坊
            if (!string.IsNullOrEmpty(WorkshopPath))
            {
                System.Diagnostics.Debug.WriteLine($"檢查工作坊路徑: '{WorkshopPath}'");
                if (Directory.Exists(WorkshopPath))
                {
                    try
                    {
                        var workshopDirs = Directory.GetDirectories(WorkshopPath);
                        System.Diagnostics.Debug.WriteLine($"  找到 {workshopDirs.Length} 個資料夾");
                        
                        var hasWorkshopMods = workshopDirs
                            .Any(dir => File.Exists(Path.Combine(dir, "About", "About.xml")));
                        System.Diagnostics.Debug.WriteLine($"  有工作坊模組: {hasWorkshopMods}");
                        if (hasWorkshopMods) hasValidModLocation = true;
                        
                        // 詳細列出前5個資料夾
                        foreach (var dir in workshopDirs.Take(5))
                        {
                            var hasAbout = File.Exists(Path.Combine(dir, "About", "About.xml"));
                            System.Diagnostics.Debug.WriteLine($"    {Path.GetFileName(dir)}: {hasAbout}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"  檢查工作坊路徑時發生錯誤: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("  工作坊路徑不存在");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("工作坊路徑未設定");
            }
            
            System.Diagnostics.Debug.WriteLine($"最終結果: {hasValidModLocation}");
            System.Diagnostics.Debug.WriteLine("=== 路徑驗證結束 ===");
            
            return hasValidModLocation;
        }

        private async Task ScanModsAsync()
        {
            try
            {
                // 顯示進度條
                ProgressPanel.Visibility = Visibility.Visible;
                ScanButton.IsEnabled = false;
                
                StatusTextBlock.Text = "正在掃描模組...";
                ProgressTextBlock.Text = "準備掃描...";
                ScanProgressBar.Value = 0;
                
                _mods.Clear();
                ModsDataGrid.ItemsSource = null;

                var allDirectories = new List<string>();
                
                // 1. 掃描本體模組 (Mods 資料夾)
                if (!string.IsNullOrEmpty(GamePath))
                {
                    var modsPath = Path.Combine(GamePath, "Mods");
                    if (Directory.Exists(modsPath))
                    {
                        allDirectories.AddRange(Directory.GetDirectories(modsPath));
                        System.Diagnostics.Debug.WriteLine($"掃描本體模組: {modsPath}");
                    }
                    
                    // 2. 掃描 Data 資料夾中的核心模組
                    var dataPath = Path.Combine(GamePath, "Data");
                    if (Directory.Exists(dataPath))
                    {
                        var dataDirs = Directory.GetDirectories(dataPath);
                        foreach (var dir in dataDirs)
                        {
                            // 檢查是否有 About.xml
                            var aboutPath = Path.Combine(dir, "About.xml");
                            if (File.Exists(aboutPath))
                            {
                                allDirectories.Add(dir);
                                System.Diagnostics.Debug.WriteLine($"掃描核心模組: {dir}");
                            }
                        }
                    }
                }
                
                // 3. 掃描工作坊模組
                if (!string.IsNullOrEmpty(WorkshopPath))
                {
                    if (Directory.Exists(WorkshopPath))
                    {
                        allDirectories.AddRange(Directory.GetDirectories(WorkshopPath));
                        System.Diagnostics.Debug.WriteLine($"掃描工作坊模組: {WorkshopPath}");
                    }
                }

                int total = allDirectories.Count;
                int processed = 0;
                
                var modInfos = new List<ModInfo>();
                
                await Task.Run(() =>
                {
                    foreach (var dir in allDirectories)
                    {
                        var modInfo = LoadModInfo(dir);
                        if (modInfo != null)
                        {
                            modInfos.Add(modInfo);
                        }
                        
                        processed++;
                        double progress = (double)processed / total * 100;
                        
                        // 減少 Dispatcher.Invoke 調用頻率
                        if (processed % 5 == 0 || processed == total)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ScanProgressBar.Value = progress;
                                ProgressTextBlock.Text = $"掃描中... {processed}/{total}";
                                StatusTextBlock.Text = $"正在掃描模組... {processed}/{total}";
                            });
                        }
                    }
                });

                // 一次性更新所有模組
                _mods.AddRange(modInfos);
                
                // 建立翻譯補丁對應關係
                BuildTranslationMappings();
                
                ModsDataGrid.ItemsSource = _mods;
                StatusTextBlock.Text = $"找到 {_mods.Count} 個模組";
                ProgressTextBlock.Text = "掃描完成！";
                
                // 掃描完成後，自動載入 ModsConfig.xml（如果已設定）
                if (!string.IsNullOrEmpty(_modsConfigPath) && File.Exists(_modsConfigPath))
                {
                    System.Diagnostics.Debug.WriteLine("模組掃描完成，開始載入 ModsConfig.xml...");
                    LoadModsConfig();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("模組掃描完成，但未設定 ModsConfig.xml 路徑");
                }
            }
            finally
            {
                // 隱藏進度條
                ProgressPanel.Visibility = Visibility.Collapsed;
                ScanButton.IsEnabled = true;
            }
        }

        private ModInfo? LoadModInfo(string modPath)
        {
            try
            {
                string aboutPath = Path.Combine(modPath, "About", "About.xml");
                if (!File.Exists(aboutPath))
                    return null;

                var aboutXml = System.Xml.Linq.XDocument.Load(aboutPath);
                var meta = aboutXml.Element("ModMetaData");

                if (meta == null)
                    return null;

                var folderName = Path.GetFileName(modPath);
                var packageId = GetXmlElementValue(meta, "packageId");
                var name = GetXmlElementValue(meta, "name");

                // 調試：輸出每個模組的基本信息
                System.Diagnostics.Debug.WriteLine($"掃描到模組: {name}");
                System.Diagnostics.Debug.WriteLine($"  FolderName: '{folderName}'");
                System.Diagnostics.Debug.WriteLine($"  PackageId: '{packageId}'");
                System.Diagnostics.Debug.WriteLine($"  路徑: {modPath}");

                var modInfo = new ModInfo
                {
                    FolderName = folderName,
                    Name = name,
                    Author = GetXmlElementValue(meta, "author"),
                    PackageId = packageId,
                    SupportedVersions = GetVersionsString(meta.Element("supportedVersions")),
                    HasChineseTraditional = CheckChineseTraditionalTranslation(modPath),
                    HasChineseSimplified = CheckChineseSimplifiedTranslation(modPath),
                    HasTranslationPatch = CheckTranslationPatch(modPath),
                    CanTranslate = CheckIfTranslatable(modPath),
                    IsVersionCompatible = IsVersionCompatible(GetVersionsString(meta.Element("supportedVersions")))
                };

                // 載入預覽圖
                string previewPath = Path.Combine(modPath, "About", "Preview.png");
                if (File.Exists(previewPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(previewPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        modInfo.PreviewImage = bitmap;
                    }
                    catch
                    {
                        // 如果載入圖片失敗，使用預設圖片
                    }
                }

                return modInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入模組信息失敗 {modPath}: {ex.Message}");
                return null;
            }
        }

        private string GetXmlElementValue(System.Xml.Linq.XElement parent, string elementName)
        {
            var element = parent.Element(elementName);
            return element?.Value ?? "";
        }

        private string GetVersionsString(System.Xml.Linq.XElement? versionsElement)
        {
            if (versionsElement == null)
                return "";

            var versions = versionsElement.Elements("li")
                .Select(v => v.Value)
                .ToArray();

            return string.Join(", ", versions);
        }

        private string CheckChineseTraditionalTranslation(string modPath)
        {
            string chinesePath = Path.Combine(modPath, "Languages", "ChineseTraditional");
            bool hasTranslation = Directory.Exists(chinesePath);
            return hasTranslation ? "有" : "無";
        }
        
        private string CheckChineseSimplifiedTranslation(string modPath)
        {
            string chinesePath = Path.Combine(modPath, "Languages", "ChineseSimplified");
            bool hasTranslation = Directory.Exists(chinesePath);
            return hasTranslation ? "有" : "無";
        }
        
        private string CheckTranslationPatch(string modPath)
        {
            // 這個方法現在只檢查模組本身是否有翻譯內容
            // 翻譯補丁的檢測在 BuildTranslationMappings 中進行
            string chinesePath = Path.Combine(modPath, "Languages", "ChineseTraditional");
            if (Directory.Exists(chinesePath))
            {
                var xmlFiles = Directory.GetFiles(chinesePath, "*.xml", SearchOption.AllDirectories);
                if (xmlFiles.Length > 0)
                    return "有";
            }
            
            return "無";
        }
        
        private void BuildTranslationMappings()
        {
            // 1. 識別翻譯補丁模組
            var translationMods = _mods.Where(m => IsTranslationMod(m)).ToList();
            
            // 2. 建立翻譯對應關係
            _translationMappings.Clear();
            
            foreach (var transMod in translationMods)
            {
                var targetMods = GetTargetModsForTranslation(transMod);
                foreach (var targetMod in targetMods)
                {
                    if (!_translationMappings.ContainsKey(targetMod.PackageId))
                        _translationMappings[targetMod.PackageId] = new List<ModInfo>();
                    _translationMappings[targetMod.PackageId].Add(transMod);
                }
            }
            
            // 3. 更新所有模組的翻譯補丁狀態
            foreach (var mod in _mods)
            {
                if (_translationMappings.ContainsKey(mod.PackageId))
                {
                    mod.HasTranslationPatch = $"有({_translationMappings[mod.PackageId].Count})";
                }
                else
                {
                    mod.HasTranslationPatch = "無";
                }
            }
            
            // 4. 根據 ModsConfig.xml 排序
            SortModsByConfig();
            
            // 5. 更新預覽面板（如果有選中的模組）
            if (SelectedMod != null)
            {
                UpdatePreviewPanel();
            }
        }
        
        private bool IsTranslationMod(ModInfo mod)
        {
            // 檢查模組名稱是否包含翻譯關鍵字
            var name = mod.Name.ToLower();
            var keywords = new[] { "繁體中文", "繁中", "漢化", "翻譯", "簡中", "chinese", "translation", "中文" };
            
            return keywords.Any(keyword => name.Contains(keyword));
        }
        
        private List<ModInfo> GetTargetModsForTranslation(ModInfo translationMod)
        {
            var targetMods = new List<ModInfo>();
            var transModPath = Path.Combine(FolderPath, translationMod.FolderName);
            
            try
            {
                // 檢查翻譯模組的 DefInjected 內容
                var defInjectedPath = Path.Combine(transModPath, "Languages", "ChineseTraditional", "DefInjected");
                if (Directory.Exists(defInjectedPath))
                {
                    var xmlFiles = Directory.GetFiles(defInjectedPath, "*.xml", SearchOption.AllDirectories);
                    
                    foreach (var file in xmlFiles.Take(20)) // 限制檢查數量以提高效能
                    {
                        try
                        {
                            var xml = System.Xml.Linq.XDocument.Load(file);
                            var defNames = xml.Root?.Elements()
                                .Select(elem => elem.Name.LocalName.Split('.')[0])
                                .Where(name => !string.IsNullOrEmpty(name))
                                .Distinct()
                                .ToList();
                            
                            if (defNames != null)
                            {
                                foreach (var defName in defNames)
                                {
                                    // 尋找對應的目標模組
                                    var targetMod = _mods.FirstOrDefault(m => 
                                        m.PackageId.Equals(defName, StringComparison.OrdinalIgnoreCase) ||
                                        m.Name.Contains(defName, StringComparison.OrdinalIgnoreCase) ||
                                        defName.Contains(m.FolderName, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (targetMod != null && targetMod != translationMod && !targetMods.Contains(targetMod))
                                    {
                                        targetMods.Add(targetMod);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                // 如果沒找到目標，嘗試從模組名稱推斷
                if (targetMods.Count == 0)
                {
                    // 假設翻譯模組名稱格式為 "A模組 繁中翻譯"
                    var nameWithoutKeywords = translationMod.Name;
                    var keywords = new[] { "繁體中文", "繁中", "漢化", "翻譯", "簡中", "chinese", "translation", "中文" };
                    
                    foreach (var keyword in keywords)
                    {
                        nameWithoutKeywords = nameWithoutKeywords.Replace(keyword, "", StringComparison.OrdinalIgnoreCase);
                    }
                    
                    nameWithoutKeywords = nameWithoutKeywords.Trim();
                    
                    // 尋找名稱相似的模組
                    var similarMod = _mods.FirstOrDefault(m => 
                        m != translationMod &&
                        (m.Name.Contains(nameWithoutKeywords, StringComparison.OrdinalIgnoreCase) ||
                         nameWithoutKeywords.Contains(m.Name, StringComparison.OrdinalIgnoreCase)));
                    
                    if (similarMod != null && !targetMods.Contains(similarMod))
                    {
                        targetMods.Add(similarMod);
                    }
                }
            }
            catch { }
            
            return targetMods;
        }
        
        private string CheckIfTranslatable(string modPath)
        {
            // 檢查模組是否包含可翻譯的內容
            try
            {
                // 1. 檢查 Defs 資料夾
                string defsPath = Path.Combine(modPath, "Defs");
                if (Directory.Exists(defsPath))
                {
                    var xmlFiles = Directory.GetFiles(defsPath, "*.xml", SearchOption.AllDirectories);
                    if (xmlFiles.Length > 0)
                    {
                        // 檢查是否包含可翻譯的內容
                        foreach (var file in xmlFiles.Take(10)) // 只檢查前10個檔案以提高效能
                        {
                            try
                            {
                                var xml = System.Xml.Linq.XDocument.Load(file);
                                var hasTranslatableContent = xml.Root?.Elements()
                                    .Any(elem => elem.Elements("label").Any() || 
                                               elem.Elements("description").Any() ||
                                               elem.Descendants("label").Any() ||
                                               elem.Descendants("description").Any());
                                
                                if (hasTranslatableContent == true)
                                    return "是";
                            }
                            catch { }
                        }
                    }
                }
                
                // 2. 檢查是否有組件（可能包含需要翻譯的字串）
                string assembliesPath = Path.Combine(modPath, "Assemblies");
                if (Directory.Exists(assembliesPath))
                {
                    var dllFiles = Directory.GetFiles(assembliesPath, "*.dll");
                    if (dllFiles.Length > 0)
                        return "是"; // DLL 可能包含需要翻譯的字串
                }
                
                return "否";
            }
            catch
            {
                return "未知";
            }
        }

        private void ModsDataGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridRow) && !(dep is DataGrid))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null || !(dep is DataGridRow row))
                return;

            var modInfo = row.DataContext as ModInfo;
            if (modInfo == null) return;

            var contextMenu = new ContextMenu();
            var openFolderItem = new MenuItem { Header = "在檔案總管中開啟" };
            openFolderItem.Click += (s, args) => OpenModFolder(modInfo);
            
            contextMenu.Items.Add(openFolderItem);
            contextMenu.IsOpen = true;
        }
        
        private void OpenModFolder(ModInfo modInfo)
        {
            try
            {
                string modPath = Path.Combine(FolderPath, modInfo.FolderName);
                if (Directory.Exists(modPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = modPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    ShowErrorWithCopy("目錄錯誤", "模組目錄不存在", "請檢查設定的路徑是否正確");
                }
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("開啟目錄失敗", $"無法開啟模組目錄", ex.ToString());
            }
        }

        private void SelectModsConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "選擇 ModsConfig.xml 檔案",
                Filter = "XML 檔案|*.xml|所有檔案|*.*",
                CheckFileExists = true,
                InitialDirectory = !string.IsNullOrEmpty(_modsConfigPath) ? 
                    Path.GetDirectoryName(_modsConfigPath) : 
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            };
            
            if (dialog.ShowDialog() == true)
            {
                _modsConfigPath = dialog.FileName;
                ModsConfigPathText.Text = Path.GetFileName(_modsConfigPath);
                SaveSettings(); // 自動儲存設定
                
                // 檢查是否有模組，如果有才立即載入
                if (_mods.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("已選擇 ModsConfig.xml 且有模組，立即載入...");
                    LoadModsConfig();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("已選擇 ModsConfig.xml 但尚無模組，等待掃描完成後載入");
                    ShowInfoMessage("提示", "ModsConfig.xml 已選擇，將在模組掃描完成後自動載入");
                }
            }
        }
        
        private void LoadModsConfig()
        {
            System.Diagnostics.Debug.WriteLine("=== LoadModsConfig 開始 ===");
            
            try
            {
                if (string.IsNullOrEmpty(_modsConfigPath))
                {
                    System.Diagnostics.Debug.WriteLine("ModsConfig 路徑為空");
                    return;
                }
                
                if (!File.Exists(_modsConfigPath))
                {
                    System.Diagnostics.Debug.WriteLine($"ModsConfig 檔案不存在: {_modsConfigPath}");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"正在載入 ModsConfig: {_modsConfigPath}");
                
                var xml = System.Xml.Linq.XDocument.Load(_modsConfigPath);
                var activeMods = xml.Root?.Element("activeMods")?.Elements("li")
                    .Select(li => li.Value)
                    .ToList();
                
                if (activeMods == null)
                {
                    System.Diagnostics.Debug.WriteLine("無法解析 activeMods 元素");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"=== ModsConfig.xml 載入開始 ===");
                System.Diagnostics.Debug.WriteLine($"啟用模組數量: {activeMods.Count}");
                System.Diagnostics.Debug.WriteLine($"程式模組數量: {_mods.Count}");
                
                // 詳細調試：輸出前10個啟用的模組ID
                System.Diagnostics.Debug.WriteLine("=== 前10個啟用的模組ID ===");
                foreach (var modId in activeMods.Take(10))
                {
                    System.Diagnostics.Debug.WriteLine($"  啟用ID: '{modId}'");
                }
                
                // 詳細調試：輸出前10個程式模組的PackageId和FolderName
                System.Diagnostics.Debug.WriteLine("=== 前10個程式模組 ===");
                foreach (var mod in _mods.Take(10))
                {
                    System.Diagnostics.Debug.WriteLine($"  程式模組: {mod.Name}");
                    System.Diagnostics.Debug.WriteLine($"    PackageId: '{mod.PackageId}'");
                    System.Diagnostics.Debug.WriteLine($"    FolderName: '{mod.FolderName}'");
                }
                
                // 標記已啟用的模組
                int matchedCount = 0;
                foreach (var mod in _mods)
                {
                    bool wasEnabled = mod.IsEnabled;
                    
                    // 更嚴格的匹配邏輯
                    bool packageIdMatch = !string.IsNullOrEmpty(mod.PackageId) && 
                                        activeMods.Contains(mod.PackageId);
                    bool folderNameMatch = !string.IsNullOrEmpty(mod.FolderName) && 
                                         activeMods.Contains(mod.FolderName);
                    
                    mod.IsEnabled = packageIdMatch || folderNameMatch;
                        
                    if (mod.IsEnabled)
                    {
                        matchedCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ 啟用: {mod.Name} (PackageId: '{mod.PackageId}', Folder: '{mod.FolderName}')");
                        System.Diagnostics.Debug.WriteLine($"    匹配方式: {(packageIdMatch ? "PackageId" : "FolderName")}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ 未啟用: {mod.Name} (PackageId: '{mod.PackageId}', Folder: '{mod.FolderName}')");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"匹配到的啟用模組: {matchedCount}");
                System.Diagnostics.Debug.WriteLine($"=== ModsConfig.xml 載入完成 ===");
                
                // 刷新顯示
                ModsDataGrid.Items.Refresh();
                UpdateModManagementLists();
                
                // 強制更新所有相關UI
                if (ModPoolListBox != null)
                {
                    ModPoolListBox.Items.Refresh();
                }
                if (EnabledModsListBox != null)
                {
                    EnabledModsListBox.Items.Refresh();
                }
                
                StatusTextBlock.Text = $"已載入 ModsConfig.xml，{activeMods.Count} 個已啟用模組，{matchedCount} 個匹配";
                
                // 顯示匹配結果給用戶
                System.Diagnostics.Debug.WriteLine("準備顯示匹配結果給用戶...");
                
                if (matchedCount < activeMods.Count)
                {
                    var missingCount = activeMods.Count - matchedCount;
                    System.Diagnostics.Debug.WriteLine($"顯示部分匹配訊息，缺少 {missingCount} 個模組");
                    
                    // 找出未匹配的模組ID
                    var unmatchedIds = activeMods.Where(id => 
                        !_mods.Any(mod => 
                            (!string.IsNullOrEmpty(mod.PackageId) && mod.PackageId == id) ||
                            (!string.IsNullOrEmpty(mod.FolderName) && mod.FolderName == id))).Take(20);
                    
                    var details = $"啟用模組數量: {activeMods.Count}\n" +
                                 $"匹配到的模組: {matchedCount}\n" +
                                 $"缺少的模組: {missingCount}\n\n" +
                                 $"程式模組數量: {_mods.Count}\n" +
                                 $"ModsConfig 路徑: {_modsConfigPath}\n\n" +
                                 $"未匹配的模組ID（前20個）:\n" +
                                 string.Join("\n", unmatchedIds);
                    
                    ShowErrorWithCopy("部分模組未匹配", 
                        $"ModsConfig.xml 已載入，但 {missingCount} 個模組在程式中找不到。\n\n" +
                        $"這可能是因為：\n" +
                        $"• 模組目錄路徑不對\n" +
                        $"• 模組的 PackageId 讀取失敗\n" +
                        $"• 模組資料夾名稱不匹配\n\n" +
                        $"請檢查模組目錄設置是否正確。\n\n" +
                        $"詳細資訊中包含未匹配的模組ID，請檢查是否對應正確的模組。", details);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("顯示完全匹配訊息");
                    
                    var details = $"啟用模組數量: {activeMods.Count}\n" +
                                 $"匹配到的模組: {matchedCount}\n" +
                                 $"程式模組數量: {_mods.Count}\n" +
                                 $"ModsConfig 路徑: {_modsConfigPath}";
                    
                    ShowErrorWithCopy("載入成功", 
                        $"ModsConfig.xml 載入成功！\n\n" +
                        $"✅ {activeMods.Count} 個啟用模組全部匹配", details);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadModsConfig 發生錯誤: {ex.Message}");
                ShowErrorWithCopy("載入 ModsConfig 失敗", $"載入 ModsConfig.xml 時發生錯誤", ex.ToString());
            }
            
            System.Diagnostics.Debug.WriteLine("=== LoadModsConfig 結束 ===");
        }

        private void SortModsByConfig()
        {
            try
            {
                // 檢查是否有模組，如果沒有就先不排序
                if (_mods.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("模組列表為空，跳過排序");
                    return;
                }
                
                if (string.IsNullOrEmpty(_modsConfigPath) || !File.Exists(_modsConfigPath))
                {
                    // 如果沒有 ModsConfig.xml，按字母排序
                    SortModsAlphabetically();
                    return;
                }
                
                var xml = System.Xml.Linq.XDocument.Load(_modsConfigPath);
                var activeMods = xml.Root?.Element("activeMods")?.Elements("li")
                    .Select(li => li.Value)
                    .ToList();
                
                if (activeMods == null || activeMods.Count == 0)
                {
                    SortModsAlphabetically();
                    return;
                }
                
                // 建立排序順序：已啟用的模組在前，按照 ModsConfig.xml 的順序
                var sortedMods = _mods
                    .OrderByDescending(mod => activeMods.Contains(mod.PackageId) || activeMods.Contains(mod.FolderName))
                    .ThenBy(mod => 
                    {
                        var index = activeMods.IndexOf(mod.PackageId);
                        return index >= 0 ? index : activeMods.IndexOf(mod.FolderName);
                    })
                    .ThenBy(mod => mod.Name)
                    .ToList();
                
                _mods = sortedMods;
                ModsDataGrid.ItemsSource = null;
                ModsDataGrid.ItemsSource = _mods;
                
                // 更新模組管理列表
                UpdateModManagementLists();
                
                System.Diagnostics.Debug.WriteLine($"模組已按 ModsConfig.xml 排序，總計 {_mods.Count} 個模組");
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("排序模組失敗", $"排序模組時發生錯誤", ex.ToString());
                SortModsAlphabetically();
            }
        }
        
        private void SortModsAlphabetically()
        {
            // 檢查是否有模組，如果沒有就先不排序
            if (_mods.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("模組列表為空，跳過字母排序");
                return;
            }
            
            var sortedMods = _mods.OrderBy(mod => mod.Name).ToList();
            _mods = sortedMods;
            ModsDataGrid.ItemsSource = null;
            ModsDataGrid.ItemsSource = _mods;
            
            // 更新模組管理列表
            UpdateModManagementLists();
            
            System.Diagnostics.Debug.WriteLine($"模組已按字母排序，總計 {_mods.Count} 個模組");
        }
        
        private void UpdateModManagementLists()
        {
            // 檢查 UI 元素是否已初始化
            if (ModPoolListBox == null || EnabledModsListBox == null)
                return;
            
            // 檢查是否有模組，如果沒有就先不排序
            if (_mods.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("模組列表為空，跳過更新模組管理列表");
                return;
            }
                
            // 更新模組池（所有模組，按字母排序）
            _modPool = _mods.OrderBy(mod => mod.Name).ToList();
            ModPoolListBox.ItemsSource = null;
            ModPoolListBox.ItemsSource = _modPool;
            
            // 更新啟用列表（已啟用的模組，按載入順序）
            _enabledMods = _mods.Where(mod => mod.IsEnabled).ToList();
            
            // 如果有 ModsConfig.xml，按其順序排序
            if (!string.IsNullOrEmpty(_modsConfigPath) && File.Exists(_modsConfigPath))
            {
                try
                {
                    var xml = System.Xml.Linq.XDocument.Load(_modsConfigPath);
                    var activeMods = xml.Root?.Element("activeMods")?.Elements("li")
                        .Select(li => li.Value)
                        .ToList();
                    
                    if (activeMods != null && activeMods.Count > 0)
                    {
                        _enabledMods = _enabledMods
                            .OrderBy(mod => 
                            {
                                var index = activeMods.IndexOf(mod.PackageId);
                                return index >= 0 ? index : activeMods.IndexOf(mod.FolderName);
                            })
                            .ToList();
                    }
                }
                catch { }
            }
            
            EnabledModsListBox.ItemsSource = null;
            EnabledModsListBox.ItemsSource = _enabledMods;
            
            System.Diagnostics.Debug.WriteLine($"模組管理列表已更新 - 模組池: {_modPool.Count}, 啟用列表: {_enabledMods.Count}");
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        
        private void ShowInfoMessage(string title, string message)
        {
            ShowErrorWithCopy(title, message, null);
        }
        
        private void ShowErrorWithCopy(string title, string message, string? details = null)
        {
            // 根據標題決定圖標
            string icon = title.Contains("成功") || title.Contains("載入成功") ? "✅" : 
                         title.Contains("警告") ? "⚠️" : "❌";
            
            var errorWindow = new Window
            {
                Title = title,
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // 主要內容區域
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(20)
            };
            
            var stackPanel = new StackPanel();
            
            // 錯誤標題
            var titleBlock = new TextBlock
            {
                Text = $"{icon} {title}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = title.Contains("成功") || title.Contains("載入成功") ? 
                    new SolidColorBrush(Color.FromRgb(34, 197, 94)) : 
                    title.Contains("警告") ? 
                    new SolidColorBrush(Color.FromRgb(245, 158, 11)) :
                    new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackPanel.Children.Add(titleBlock);
            
            // 錯誤訊息
            var messageBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(messageBlock);
            
            // 詳細資訊（如果有）
            if (!string.IsNullOrEmpty(details))
            {
                var detailsTitle = new TextBlock
                {
                    Text = "📋 詳細資訊：",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    Margin = new Thickness(0, 10, 0, 5)
                };
                stackPanel.Children.Add(detailsTitle);
                
                var detailsBlock = new TextBox
                {
                    Text = details,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap,
                    IsReadOnly = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    MinHeight = 200,
                    MaxHeight = 300
                };
                stackPanel.Children.Add(detailsBlock);
            }
            
            scrollViewer.Content = stackPanel;
            grid.Children.Add(scrollViewer);
            
            // 按鈕區域
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 10, 20, 20)
            };
            
            var copyButton = new Button
            {
                Content = "📋 複製到剪貼簿",
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 12,
                Cursor = Cursors.Hand
            };
            
            var fullText = $"[{title}]\n{message}";
            if (!string.IsNullOrEmpty(details))
            {
                fullText += $"\n\n詳細資訊：\n{details}";
            }
            
            copyButton.Click += (s, e) =>
            {
                Clipboard.SetText(fullText);
                var notification = new TextBlock
                {
                    Text = "✅ 已複製到剪貼簿！",
                    Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                    FontSize = 12,
                    Margin = new Thickness(10)
                };
                buttonPanel.Children.Insert(0, notification);
                
                // 使用非阻塞方式移除通知
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (sender, args) =>
                {
                    timer.Stop();
                    if (buttonPanel.Children.Contains(notification))
                    {
                        buttonPanel.Children.Remove(notification);
                    }
                };
                timer.Start();
            };
            
            var closeButton = new Button
            {
                Content = "關閉",
                Background = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(15, 8, 15, 8),
                FontSize = 12,
                Cursor = Cursors.Hand
            };
            
            closeButton.Click += (s, e) => errorWindow.Close();
            
            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(closeButton);
            
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);
            
            errorWindow.Content = grid;
            errorWindow.Owner = this;
            errorWindow.ShowDialog();
        }
        
        private MessageBoxResult ShowConfirmDialog(string title, string message, string yesButtonText = "確定", string noButtonText = "取消")
        {
            var confirmWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // 內容區域
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var messageBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            stackPanel.Children.Add(messageBlock);
            
            grid.Children.Add(stackPanel);
            
            // 按鈕區域
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 10, 20, 20)
            };
            
            var yesButton = new Button
            {
                Content = yesButtonText,
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 12,
                Cursor = Cursors.Hand
            };
            
            var noButton = new Button
            {
                Content = noButtonText,
                Background = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 8, 20, 8),
                FontSize = 12,
                Cursor = Cursors.Hand
            };
            
            var result = MessageBoxResult.No;
            
            yesButton.Click += (s, e) => 
            {
                result = MessageBoxResult.Yes;
                confirmWindow.Close();
            };
            
            noButton.Click += (s, e) => 
            {
                result = MessageBoxResult.No;
                confirmWindow.Close();
            };
            
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);
            
            confirmWindow.Content = grid;
            confirmWindow.Owner = this;
            confirmWindow.ShowDialog();
            
            return result;
        }
        
        // 模組管理事件處理器
        private void MoveToEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (ModPoolListBox == null) return;
            
            try
            {
                var selectedMods = ModPoolListBox.SelectedItems.Cast<ModInfo>().ToList();
                foreach (var mod in selectedMods)
                {
                    if (!mod.IsEnabled)
                    {
                        mod.IsEnabled = true;
                    }
                }
                UpdateModManagementLists();
                ModsDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("移動模組失敗", $"移動模組到啟用列表時發生錯誤", ex.ToString());
            }
        }
        
        private void MoveToPool_Click(object sender, RoutedEventArgs e)
        {
            if (EnabledModsListBox == null) return;
            
            try
            {
                var selectedMods = EnabledModsListBox.SelectedItems.Cast<ModInfo>().ToList();
                foreach (var mod in selectedMods)
                {
                    if (mod.IsEnabled)
                    {
                        mod.IsEnabled = false;
                    }
                }
                UpdateModManagementLists();
                ModsDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("移動模組失敗", $"移動模組到模組池時發生錯誤", ex.ToString());
            }
        }
        
        private void SaveModsConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_modsConfigPath))
                {
                    ShowErrorWithCopy("檔案未選擇", "請先選擇 ModsConfig.xml 檔案", "請在設定頁籤中選擇 ModsConfig.xml 檔案");
                    return;
                }
                
                // 二次確認
                var result = ShowConfirmDialog(
                    "確認儲存",
                    $"確定要儲存模組配置嗎？\n\n將更新 {_enabledMods.Count} 個已啟用模組的載入順序。\n\n檔案位置：{_modsConfigPath}",
                    "儲存", "取消");
                
                if (result != MessageBoxResult.Yes)
                    return;
                
                var enabledModIds = _enabledMods
                    .Select(mod => !string.IsNullOrEmpty(mod.PackageId) ? mod.PackageId : mod.FolderName)
                    .ToList();
                
                var xml = new System.Xml.Linq.XDocument(
                    new System.Xml.Linq.XElement("ModsConfigData",
                        new System.Xml.Linq.XElement("activeMods",
                            enabledModIds.Select(id => new System.Xml.Linq.XElement("li", id))
                        )
                    )
                );
                
                xml.Save(_modsConfigPath);
                ShowInfoMessage("成功", "ModsConfig.xml 已儲存成功！");
                StatusTextBlock.Text = "配置已儲存";
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("儲存失敗", $"儲存 ModsConfig.xml 時發生錯誤", ex.ToString());
            }
        }
        
        private void RefreshModLists_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== 重新整理按鈕被點擊 ===");
            
            try
            {
                // 重新載入 ModsConfig.xml 並更新列表
                if (!string.IsNullOrEmpty(_modsConfigPath) && File.Exists(_modsConfigPath))
                {
                    LoadModsConfig();
                    ShowInfoMessage("成功", "模組列表已重新整理！");
                }
                else
                {
                    ShowInfoMessage("提示", "請先選擇 ModsConfig.xml 檔案");
                }
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("重新整理失敗", $"重新整理模組列表時發生錯誤", ex.ToString());
            }
        }
        
        private void DiagnoseModsConfig_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== 診斷按鈕被點擊 ===");
            
            // 先測試路徑驗證
            System.Diagnostics.Debug.WriteLine("=== 路徑診斷測試 ===");
            var isValid = IsValidModDirectory(GamePath);
            System.Diagnostics.Debug.WriteLine($"路徑驗證結果: {isValid}");
            
            // 顯示診斷結果
            var diagnosticInfo = $"遊戲路徑: {GamePath}\n" +
                                $"工作坊路徑: {WorkshopPath}\n" +
                                $"設定路徑: {ConfigPath}\n" +
                                $"路徑驗證結果: {(isValid ? "✅ 通過" : "❌ 失敗")}\n\n" +
                                $"請查看 Debug 輸出視窗獲取詳細資訊。";
            
            ShowErrorWithCopy("路徑診斷結果", 
                "路徑診斷完成！\n\n詳細資訊請查看 Debug 輸出視窗。", 
                diagnosticInfo);
        }
        
        private void ModPoolListBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                // 實現拖拽功能（從啟用列表拖到模組池）
                if (e.Data.GetDataPresent(typeof(ModInfo)))
                {
                    var mod = e.Data.GetData(typeof(ModInfo)) as ModInfo;
                    if (mod != null && mod.IsEnabled)
                    {
                        mod.IsEnabled = false;
                        UpdateModManagementLists();
                        ModsDataGrid.Items.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                // 拖拽操作失敗，靜默處理
                System.Diagnostics.Debug.WriteLine($"拖拽失敗：{ex.Message}");
            }
        }
        
        private void EnabledModsListBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                // 實現拖拽功能（從模組池拖到啟用列表）
                if (e.Data.GetDataPresent(typeof(ModInfo)))
                {
                    var mod = e.Data.GetData(typeof(ModInfo)) as ModInfo;
                    if (mod != null && !mod.IsEnabled)
                    {
                        mod.IsEnabled = true;
                        UpdateModManagementLists();
                        ModsDataGrid.Items.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                // 拖拽操作失敗，靜默處理
                System.Diagnostics.Debug.WriteLine($"拖拽失敗：{ex.Message}");
            }
        }
    }

    public class AppSettings
    {
        public string WorkshopPath { get; set; } = "";
        public string GamePath { get; set; } = "";
        public string ConfigPath { get; set; } = "";
        public string ModsConfigPath { get; set; } = "";
        public string GameVersion { get; set; } = "1.6";
        public string Language { get; set; } = "zh-TW";
        
        // 保持向後相容性
        public string ModsDirectory 
        { 
            get => GamePath; 
            set => GamePath = value; 
        }
    }

    public class ModInfo
    {
        public string FolderName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string PackageId { get; set; } = "";
        public string SupportedVersions { get; set; } = "";
        public string HasChineseTraditional { get; set; } = "無";
        public string HasChineseSimplified { get; set; } = "無";
        public string HasTranslationPatch { get; set; } = "無";
        public string CanTranslate { get; set; } = "否";
        public bool IsVersionCompatible { get; set; } = true;
        public bool IsEnabled { get; set; } = false;
        public BitmapImage? PreviewImage { get; set; }
        
        // 顏色屬性
        public Brush HasChineseTraditionalColor => GetStatusColor(HasChineseTraditional);
        public Brush HasChineseSimplifiedColor => GetStatusColor(HasChineseSimplified);
        public Brush HasTranslationPatchColor => GetStatusColor(HasTranslationPatch);
        public Brush CanTranslateColor => GetStatusColor(CanTranslate);
        public Brush VersionCompatibilityColor => IsVersionCompatible ? 
            new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)) : new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));
        
        // 背景色屬性
        public Brush HasChineseTraditionalBackground => GetStatusBackground(HasChineseTraditional);
        public Brush HasChineseSimplifiedBackground => GetStatusBackground(HasChineseSimplified);
        public Brush HasTranslationPatchBackground => GetStatusBackground(HasTranslationPatch);
        public Brush CanTranslateBackground => GetStatusBackground(CanTranslate);
        public Brush VersionCompatibilityBackground => IsVersionCompatible ? 
            new SolidColorBrush(Colors.Transparent) : new SolidColorBrush(Color.FromArgb(50, 255, 255, 0));
        
        private Brush GetStatusColor(string status)
        {
            return status switch
            {
                "有" or "是" => new SolidColorBrush(Color.FromArgb(128, 0, 128, 0)),
                "無" or "否" => new SolidColorBrush(Color.FromArgb(128, 128, 0, 0)),
                _ => new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
            };
        }
        
        private Brush GetStatusBackground(string status)
        {
            return status switch
            {
                "有" or "是" => new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)),
                "無" or "否" => new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)),
                _ => new SolidColorBrush(Colors.Transparent) // 正常狀態無底色
            };
        }
        
        private void TestI18nButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 測試 C# 中的本地化
                var title = LocalizationManager.GetString("WindowTitle");
                var settings = LocalizationManager.GetString("TabSettings");
                var browse = LocalizationManager.GetString("Browse");
                
                var message = $"C# 本地化測試結果：\n\n" +
                             $"WindowTitle: '{title}'\n" +
                             $"TabSettings: '{settings}'\n" +
                             $"Browse: '{browse}'\n\n" +
                             $"如果看到的是 key 而不是實際文字，\n" +
                             "說明資源檔案沒有正確載入。";
                
                if (title.Contains("WindowTitle") || title.Contains("["))
                {
                    MessageBox.Show(message, "❌ i18n 測試失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(message, "✅ i18n 測試成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"測試發生錯誤：{ex.Message}\n\n{ex}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
