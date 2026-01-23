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
        
        // 模組管理相關
        private List<ModInfo> _modPool = new List<ModInfo>();
        private List<ModInfo> _enabledMods = new List<ModInfo>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // 載入設定
            LoadSettings();
            
            // 初始化版本選項
            InitializeGameVersions();
            
            // 設置選擇變更事件
            ModsDataGrid.SelectionChanged += ModsDataGrid_SelectionChanged;
            
            // 延遲更新 UI，確保控制項已初始化
            this.Loaded += MainWindow_Loaded;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 一次性更新所有 UI 元素
            UpdateAllUI();
            
            // 延遲載入 ModsConfig.xml，確保 UI 完全準備好
            System.Threading.Tasks.Task.Delay(500).ContinueWith(async _ =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    // 載入 ModsConfig.xml（如果設定中存在）
                    if (!string.IsNullOrEmpty(_modsConfigPath) && File.Exists(_modsConfigPath))
                    {
                        LoadModsConfig();
                    }
                });
            });
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
            var versions = new[] { "1.0", "1.1", "1.2", "1.3", "1.4", "1.5", "1.6" };
            GameVersionComboBox.ItemsSource = versions;
            GameVersionComboBox.SelectedItem = _selectedGameVersion;
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
                    System.Diagnostics.Debug.WriteLine($"GameVersion: {_settings.GameVersion}");
                    
                    // 恢復設定
                    if (!string.IsNullOrEmpty(_settings.ModsDirectory))
                    {
                        FolderPath = _settings.ModsDirectory;
                        System.Diagnostics.Debug.WriteLine($"設定模組目錄: {FolderPath}");
                    }
                    
                    if (!string.IsNullOrEmpty(_settings.ModsConfigPath))
                    {
                        _modsConfigPath = _settings.ModsConfigPath;
                        System.Diagnostics.Debug.WriteLine($"設定 ModsConfigPath: {_modsConfigPath}");
                        System.Diagnostics.Debug.WriteLine($"檔案存在: {File.Exists(_modsConfigPath)}");
                        
                        // 不再在這裡更新 UI，改為在 MainWindow_Loaded 中統一處理
                    }
                    
                    if (!string.IsNullOrEmpty(_settings.GameVersion))
                    {
                        _selectedGameVersion = _settings.GameVersion;
                        System.Diagnostics.Debug.WriteLine($"設定遊戲版本: {_selectedGameVersion}");
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
                MessageBox.Show($"載入設定失敗：{ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                _settings.ModsDirectory = FolderPath;
                _settings.ModsConfigPath = _modsConfigPath;
                _settings.GameVersion = _selectedGameVersion;
                
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFileName, json);
                
                // 調試信息
                System.Diagnostics.Debug.WriteLine($"儲存設定 - ModsDirectory: {_settings.ModsDirectory}");
                System.Diagnostics.Debug.WriteLine($"儲存設定 - ModsConfigPath: {_settings.ModsConfigPath}");
                System.Diagnostics.Debug.WriteLine($"儲存設定 - GameVersion: {_settings.GameVersion}");
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("儲存設定失敗", $"儲存設定時發生錯誤", ex.ToString());
            }
        }

        private string _folderPath = "";
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
        public string FolderPath 
        { 
            get => _folderPath;
            set
            {
                if (_folderPath != value)
                {
                    _folderPath = value;
                    OnPropertyChanged(nameof(FolderPath));
                    
                    // 只有在不是載入設定時才自動保存
                    if (!_isLoadingSettings)
                    {
                        SaveSettings();
                    }
                }
            }
        }

        private void GameVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameVersionComboBox.SelectedItem is string selectedVersion)
            {
                _selectedGameVersion = selectedVersion;
                SaveSettings(); // 自動儲存設定
                RefreshVersionCompatibility();
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

        private void FolderPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 避免無限遞迴：只有當值真正改變時才更新
                if (textBox.Text != FolderPath)
                {
                    FolderPath = textBox.Text;
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "選擇 RimWorld 模組目錄",
                ShowNewFolderButton = false,
                SelectedPath = !string.IsNullOrEmpty(FolderPath) ? FolderPath : 
                    (!string.IsNullOrEmpty(_settings.ModsDirectory) ? _settings.ModsDirectory : "")
            };
            
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FolderPath = dialog.SelectedPath; // 會自動觸發 SaveSettings
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
            // 確保文字框顯示當前路徑，但避免觸發 TextChanged 事件
            if (FolderPathTextBox != null && FolderPathTextBox.Text != FolderPath)
            {
                // 移除事件處理器以避免無限遞迴
                FolderPathTextBox.TextChanged -= FolderPathTextBox_TextChanged;
                FolderPathTextBox.Text = FolderPath;
                FolderPathTextBox.TextChanged += FolderPathTextBox_TextChanged;
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
                MessageBox.Show("請先選擇有效的模組目錄", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await ScanModsAsync();
        }

        private bool IsValidModDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return false;
            
            // 檢查是否至少包含一個 About/About.xml 檔案
            return Directory.GetDirectories(path)
                .Any(dir => File.Exists(Path.Combine(dir, "About", "About.xml")));
        }

        private async Task ScanModsAsync()
        {
            try
            {
                // 顯示進度條
                ProgressPanel.Visibility = Visibility.Visible;
                ScanButton.IsEnabled = false;
                BrowseButton.IsEnabled = false;
                
                StatusTextBlock.Text = "正在掃描模組...";
                ProgressTextBlock.Text = "準備掃描...";
                ScanProgressBar.Value = 0;
                
                _mods.Clear();
                ModsDataGrid.ItemsSource = null;

                var directories = Directory.GetDirectories(FolderPath);
                int total = directories.Length;
                int processed = 0;
                
                var modInfos = new List<ModInfo>();
                
                await Task.Run(() =>
                {
                    foreach (var dir in directories)
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
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorWithCopy("權限錯誤", "沒有權限存取此目錄", ex.ToString());
                StatusTextBlock.Text = "掃描失敗 - 權限不足";
            }
            catch (DirectoryNotFoundException ex)
            {
                ShowErrorWithCopy("目錄錯誤", "指定的目錄不存在", "檢查模組目錄路徑是否正確");
                StatusTextBlock.Text = "掃描失敗 - 目錄不存在";
            }
            catch (Exception ex)
            {
                ShowErrorWithCopy("掃描錯誤", "掃描模組時發生錯誤", ex.ToString());
                StatusTextBlock.Text = "掃描失敗";
            }
            finally
            {
                // 隱藏進度條
                ProgressPanel.Visibility = Visibility.Collapsed;
                ScanButton.IsEnabled = true;
                BrowseButton.IsEnabled = true;
            }
        }

        private ModInfo LoadModInfo(string modPath)
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

                var modInfo = new ModInfo
                {
                    FolderName = Path.GetFileName(modPath),
                    Name = GetXmlElementValue(meta, "name"),
                    Author = GetXmlElementValue(meta, "author"),
                    PackageId = GetXmlElementValue(meta, "packageId"),
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
            catch
            {
                // 忽略無法載入的模組
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
                    MessageBox.Show("模組目錄不存在", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Multiselect = false,
                InitialDirectory = !string.IsNullOrEmpty(_modsConfigPath) ? 
                    Path.GetDirectoryName(_modsConfigPath) : 
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            if (dialog.ShowDialog() == true)
            {
                _modsConfigPath = dialog.FileName;
                ModsConfigPathText.Text = Path.GetFileName(_modsConfigPath);
                SaveSettings(); // 自動儲存設定
                LoadModsConfig();
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
                
                // 標記已啟用的模組
                int matchedCount = 0;
                foreach (var mod in _mods)
                {
                    bool wasEnabled = mod.IsEnabled;
                    mod.IsEnabled = activeMods.Contains(mod.PackageId) || 
                                 activeMods.Contains(mod.FolderName);
                        
                    if (mod.IsEnabled)
                    {
                        matchedCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ 啟用: {mod.Name} (PackageId: '{mod.PackageId}', Folder: '{mod.FolderName}')");
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
                    
                    var details = $"啟用模組數量: {activeMods.Count}\n" +
                                 $"匹配到的模組: {matchedCount}\n" +
                                 $"缺少的模組: {missingCount}\n\n" +
                                 $"程式模組數量: {_mods.Count}\n" +
                                 $"ModsConfig 路徑: {_modsConfigPath}";
                    
                    ShowErrorWithCopy("部分模組未匹配", 
                        $"ModsConfig.xml 已載入，但 {missingCount} 個模組在程式中找不到。\n\n" +
                        $"這可能是因為：\n" +
                        $"• 模組目錄路徑不對\n" +
                        $"• 模組的 PackageId 讀取失敗\n" +
                        $"• 模組資料夾名稱不匹配\n\n" +
                        $"請檢查模組目錄設置是否正確。", details);
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
                    MessageBox.Show("請先選擇 ModsConfig.xml 檔案", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // 二次確認
                var result = MessageBox.Show(
                    $"確定要儲存模組配置嗎？\n\n將更新 {_enabledMods.Count} 個已啟用模組的載入順序。\n\n檔案位置：{_modsConfigPath}",
                    "確認儲存",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
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
            
            // 最簡單的測試
            try
            {
                var result = MessageBox.Show(
                    "診斷按鈕測試！\n\n你看到了這個訊息嗎？", 
                    "按鈕測試", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                System.Diagnostics.Debug.WriteLine($"用戶選擇了: {result}");
                
                if (result == MessageBoxResult.Yes)
                {
                    ShowInfoMessage("成功", "太好了！按鈕和 MessageBox 都正常工作！");
                }
                else
                {
                    ShowInfoMessage("確認", "看起來按鈕和 MessageBox 都能正常顯示");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"測試過程發生錯誤: {ex.Message}");
                ShowErrorWithCopy("測試失敗", $"測試失敗：{ex.Message}", ex.ToString());
            }
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
        public string ModsDirectory { get; set; } = "";
        public string ModsConfigPath { get; set; } = "";
        public string GameVersion { get; set; } = "1.6";
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
    }
}
