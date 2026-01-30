using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RimWorldTranslationTool.Services.Paths;
using RimWorldTranslationTool.ViewModels;

namespace RimWorldTranslationTool
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly Controllers.SettingsController _settingsController;
        private readonly IPathService _pathService;

        public MainWindow(
            MainViewModel viewModel,
            Controllers.SettingsController settingsController,
            IPathService pathService)
        {
            InitializeComponent();
            
            _viewModel = viewModel;
            _settingsController = settingsController;
            _pathService = pathService;
            
            _settingsController.SetViewModel(_viewModel);
            
            DataContext = _viewModel;
            
            // 延遲初始化設定
            this.Loaded += MainWindow_Loaded;
            
            // 設置選擇變更事件 (如果 XAML 沒綁定)
            ModsDataGrid.SelectionChanged += ModsDataGrid_SelectionChanged;
            LocalModsDataGrid.SelectionChanged += LocalModsDataGrid_SelectionChanged;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_settingsController != null)
            {
                await _settingsController.InitializeAsync();
                
                // 將設定路徑同步到 ViewModel
                _viewModel.GamePath = _settingsController.GetCurrentGamePath();
            }
        }

        private void ModsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModsDataGrid.SelectedItem is ModViewModel selectedMod)
            {
                _viewModel.SelectedMod = selectedMod;
            }
        }

        private void LocalModsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LocalModsDataGrid.SelectedItem is ModViewModel selectedMod)
            {
                // 注意：MainViewModel 目前沒有單獨的 SelectedLocalMod，暫時共用
                _viewModel.SelectedMod = selectedMod;
            }
        }

        // 拖放支援
        private void GamePathTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string path = files[0];
                    if (System.IO.Directory.Exists(path))
                    {
                        _viewModel.GamePath = path;
                    }
                }
            }
        }

        private void GamePathTextBox_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void BrowseGameButton_Click(object sender, RoutedEventArgs e)
        {
            _settingsController?.HandleBrowseGamePath();
            _viewModel.GamePath = _settingsController?.GetCurrentGamePath() ?? "";
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ToggleTheme();
            if (ThemeIcon != null)
            {
                ThemeIcon.Text = ThemeManager.Instance.IsDarkMode ? "☀️" : "🌙";
            }
        }

        // 懸停預覽功能 (暫時保留在 Code-behind，因為涉及 ToolTip 的動態生成)
        private void DataGridRow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ModViewModel mod)
            {
                ShowHoverPreview(mod);
            }
        }

        private void DataGridRow_MouseLeave(object sender, MouseEventArgs e)
        {
            HideHoverPreview();
        }

        private void ShowHoverPreview(ModViewModel mod)
        {
            // 實作懸停預覽邏輯...
        }

        private void HideHoverPreview()
        {
            // 隱藏懸停預覽邏輯...
        }
        
        private async void AutoDetectPaths_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsController != null)
            {
                await _settingsController.HandleAutoDetectModsConfig();
            }
        }
        
        private async void ManualSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsController != null)
            {
                await _settingsController.HandleManualSave();
            }
        }
        
        private void SelectModsConfigButton_Click(object sender, RoutedEventArgs e)
        {
            _settingsController?.HandleSelectModsConfig();
        }
        
        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (LanguageComboBox?.SelectedItem as string) ?? "";
            if (!string.IsNullOrEmpty(selected))
            {
                LocalizationService.Instance.SetLanguage(selected);
            }
        }
        
        private void GameVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
        
        private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private async void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsController != null)
            {
                await _settingsController.HandleExportSettings();
            }
        }
        
        private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsController != null)
            {
                await _settingsController.HandleImportSettings();
            }
        }
        
        private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsController != null)
            {
                await _settingsController.HandleResetSettings();
            }
        }
        
        private void AutoSaveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _settingsController?.HandleAutoSaveChanged(true);
        }
        
        private void AutoSaveCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _settingsController?.HandleAutoSaveChanged(false);
        }
        
        private void ModsDataGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        }
        
        private void LocalModsDataGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        }
        
        private void ScanLocalModsButton_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private void MoveToEnabled_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private void MoveToPool_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private void RefreshModLists_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private void DiagnoseModsConfig_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private void SaveModsConfig_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private void ModPoolListBox_Drop(object sender, DragEventArgs e)
        {
        }
        
        private void EnabledModsListBox_Drop(object sender, DragEventArgs e)
        {
        }
        
        private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
        {
        }
        
        private void ListBoxItem_MouseLeave(object sender, MouseEventArgs e)
        {
        }
    }
}
