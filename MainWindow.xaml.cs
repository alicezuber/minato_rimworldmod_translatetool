using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RimWorldTranslationTool.ViewModels;
using RimWorldTranslationTool.Services.Paths;

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
            _viewModel.GamePath = _settingsController.GetCurrentGamePath();
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
    }
}
