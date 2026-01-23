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

namespace RimWorldTranslationTool
{
    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        private List<ModInfo> _mods = new List<ModInfo>();
        private ModInfo? _selectedMod;
        private Dictionary<string, List<ModInfo>> _translationMappings = new();
        private string _selectedGameVersion = "1.6";
        private string _modsConfigPath = "";
        private AppSettings _settings = new AppSettings();
        private const string SettingsFileName = "RimWorldTranslationTool_Settings.json";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // 載入設定
            LoadSettings();
            
            // 初始化版本選項
            InitializeGameVersions();
            
            // 初始化時更新路徑顯示
            UpdatePathDisplay();
            
            // 設置選擇變更事件
            ModsDataGrid.SelectionChanged += ModsDataGrid_SelectionChanged;
        }
        
        private void InitializeGameVersions()
        {
            var versions = new[] { "1.0", "1.1", "1.2", "1.3", "1.4", "1.5", "1.6" };
            GameVersionComboBox.ItemsSource = versions;
            GameVersionComboBox.SelectedItem = _selectedGameVersion;
        }
        
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFileName))
                {
                    var json = File.ReadAllText(SettingsFileName);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    
                    // 恢復設定
                    if (!string.IsNullOrEmpty(_settings.ModsDirectory))
                    {
                        FolderPath = _settings.ModsDirectory;
                    }
                    
                    if (!string.IsNullOrEmpty(_settings.ModsConfigPath))
                    {
                        _modsConfigPath = _settings.ModsConfigPath;
                        ModsConfigPathText.Text = Path.GetFileName(_modsConfigPath);
                    }
                    
                    if (!string.IsNullOrEmpty(_settings.GameVersion))
                    {
                        _selectedGameVersion = _settings.GameVersion;
                        GameVersionComboBox.SelectedItem = _selectedGameVersion;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入設定失敗：{ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存設定失敗：{ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        public string FolderPath 
        { 
            get => _folderPath;
            set
            {
                if (_folderPath != value)
                {
                    _folderPath = value;
                    OnPropertyChanged(nameof(FolderPath));
                    SaveSettings(); // 自動儲存設定
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
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("沒有權限存取此目錄", "權限錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusTextBlock.Text = "掃描失敗 - 權限不足";
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("指定的目錄不存在", "目錄錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusTextBlock.Text = "掃描失敗 - 目錄不存在";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"掃描模組時發生錯誤：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
            
            // 4. 更新預覽面板（如果有選中的模組）
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
                MessageBox.Show($"無法開啟目錄：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
            try
            {
                if (string.IsNullOrEmpty(_modsConfigPath) || !File.Exists(_modsConfigPath))
                    return;
                
                var xml = System.Xml.Linq.XDocument.Load(_modsConfigPath);
                var activeMods = xml.Root?.Element("activeMods")?.Elements("li")
                    .Select(li => li.Value)
                    .ToList();
                
                if (activeMods != null)
                {
                    // 標記已啟用的模組
                    foreach (var mod in _mods)
                    {
                        mod.IsEnabled = activeMods.Contains(mod.PackageId) || 
                                     activeMods.Contains(mod.FolderName);
                    }
                    
                    // 刷新顯示
                    ModsDataGrid.Items.Refresh();
                    StatusTextBlock.Text = $"已載入 ModsConfig.xml，{activeMods.Count} 個已啟用模組";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入 ModsConfig.xml 失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
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
            new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)) : new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));
        
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
                _ => new SolidColorBrush(Color.FromArgb(50, 0, 0, 0))
            };
        }
    }
}
