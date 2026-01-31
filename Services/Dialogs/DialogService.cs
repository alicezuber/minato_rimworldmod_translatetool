using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool.Services.Dialogs
{
    /// <summary>
    /// 彈窗服務實現
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly IPathService _pathService;
        
        public DialogService(IPathService pathService)
        {
            _pathService = pathService;
        }
        public async Task ShowSuccessAsync(string message, string title = "成功")
        {
            await ShowCustomDialogAsync(message, title, DialogType.Success);
        }
        
        public async Task ShowInfoAsync(string message, string title = "資訊")
        {
            await ShowCustomDialogAsync(message, title, DialogType.Info);
        }
        
        public async Task ShowWarningAsync(string message, string title = "警告")
        {
            await ShowCustomDialogAsync(message, title, DialogType.Warning);
        }
        
        public async Task ShowErrorAsync(string message, Exception? exception = null, string title = "錯誤")
        {
            var fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\n\n詳細資訊:\n{exception.Message}";
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    fullMessage += $"\n\n堆疊追蹤:\n{exception.StackTrace}";
                }
            }
            
            await ShowCustomDialogAsync(fullMessage, title, DialogType.Error);
        }
        
        public async Task ShowCriticalErrorAsync(string message, Exception? exception = null, string title = "嚴重錯誤")
        {
            var fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\n\n詳細資訊:\n{exception.Message}";
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    fullMessage += $"\n\n堆疊追蹤:\n{exception.StackTrace}";
                }
            }
            
            await ShowCustomDialogAsync(fullMessage, title, DialogType.Critical);
        }
        
        public async Task<bool> ShowConfirmationAsync(string message, string title = "確認")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            }).Task.ConfigureAwait(false);
        }
        
        public async Task<DialogResult> ShowYesNoCancelAsync(string message, string title = "選擇")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                
                return result switch
                {
                    MessageBoxResult.Yes => DialogResult.Yes,
                    MessageBoxResult.No => DialogResult.No,
                    MessageBoxResult.Cancel => DialogResult.Cancel,
                    _ => DialogResult.None
                };
            }).Task.ConfigureAwait(false);
        }
        
        public async Task<string?> ShowInputDialogAsync(string message, string title = "輸入", string defaultValue = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new InputDialog(message, title, defaultValue);
                var result = dialog.ShowDialog();
                return result == true ? dialog.InputText : null;
            }).Task.ConfigureAwait(false);
        }
        
        public async Task<T?> ShowSelectionDialogAsync<T>(string message, string title, T[] options, T? defaultOption = default)
            where T : class
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new SelectionDialog<T>(message, title, options, defaultOption);
                var result = dialog.ShowDialog();
                return result == true ? dialog.SelectedItem : null;
            }).Task.ConfigureAwait(false);
        }
        
        public async Task ShowProgressDialogAsync(string title, string message, IProgress<ProgressReport> progress)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new ProgressDialog(title, message, progress);
                dialog.ShowDialog();
            }).Task.ConfigureAwait(false);
        }
        
        public async Task ShowAboutAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new AboutDialog();
                dialog.ShowDialog();
            }).Task.ConfigureAwait(false);
        }
        
        public async Task ShowLogViewerAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new LogViewerDialog(_pathService);
                dialog.ShowDialog();
            }).Task.ConfigureAwait(false);
        }
        
        public async Task ShowSettingsAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new SettingsDialog();
                dialog.ShowDialog();
            }).Task.ConfigureAwait(false);
        }
        
        private async Task ShowCustomDialogAsync(string message, string title, DialogType type)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialogType = type switch
                {
                    DialogType.Success => Views.ModernDialogType.Success,
                    DialogType.Info => Views.ModernDialogType.Info,
                    DialogType.Warning => Views.ModernDialogType.Warning,
                    DialogType.Error => Views.ModernDialogType.Error,
                    DialogType.Critical => Views.ModernDialogType.Critical,
                    _ => Views.ModernDialogType.Info
                };

                var dialog = new Views.ModernDialog(message, title, dialogType);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }).Task.ConfigureAwait(false);
        }
    }
    
    /// <summary>
    /// 對話框類型
    /// </summary>
    internal enum DialogType
    {
        Success,
        Info,
        Warning,
        Error,
        Critical
    }
    
    /// <summary>
    /// 輸入對話框
    /// </summary>
    public class InputDialog : Window
    {
        public InputDialog(string message, string title, string defaultValue = "")
        {
            Title = title;
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            var grid = new Grid();
            grid.Margin = new Thickness(20);
            
            var messageLabel = new Label
            {
                Content = message,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var inputTextBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 0, 0, 20)
            };
            inputTextBox.TextChanged += (s, e) => InputText = inputTextBox.Text;
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var okButton = new Button { Content = "確定", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "取消", Width = 80 };
            
            okButton.Click += (s, e) =>
            {
                DialogResult = true;
                Close();
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            grid.Children.Add(messageLabel);
            grid.Children.Add(inputTextBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }

        public string InputText { get; private set; } = "";
    }
    
    /// <summary>
    /// 選擇對話框
    /// </summary>
    public class SelectionDialog<T> : Window
        where T : class
    {
        public SelectionDialog(string message, string title, T[] options, T? defaultOption = default)
        {
            Title = title;
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            var grid = new Grid();
            grid.Margin = new Thickness(20);
            
            var messageLabel = new Label
            {
                Content = message,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var listBox = new ListBox
            {
                Margin = new Thickness(0, 0, 0, 20),
                Height = 150
            };
            
            foreach (var option in options)
            {
                var item = new ListBoxItem { Content = option.ToString(), Tag = option };
                if (option?.Equals(defaultOption) == true)
                {
                    item.IsSelected = true;
                }
                listBox.Items.Add(item);
            }
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var okButton = new Button { Content = "確定", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "取消", Width = 80 };
            
            okButton.Click += (s, e) => 
            { 
                if (listBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is T selected)
                {
                    SelectedItem = selected;
                    DialogResult = true;
                }
                Close();
            };
            
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            grid.Children.Add(messageLabel);
            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }

        public T? SelectedItem { get; private set; }
    }
    
    /// <summary>
    /// 進度對話框
    /// </summary>
    public class ProgressDialog : Window
    {
        private readonly IProgress<ProgressReport> _progress;
        private readonly Label _messageLabel;
        private readonly ProgressBar _progressBar;
        
        public ProgressDialog(string title, string message, IProgress<ProgressReport> progress)
        {
            Title = title;
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            _progress = progress;
            
            var grid = new Grid();
            grid.Margin = new Thickness(20);
            
            _messageLabel = new Label
            {
                Content = message,
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            _progressBar = new ProgressBar
            {
                Height = 20,
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            grid.Children.Add(_messageLabel);
            grid.Children.Add(_progressBar);
            
            Content = grid;
            
            // 建立一個自訂的 Progress 類別來處理進度更新
            var customProgress = new CustomProgress(this);
            // 注意：這裡需要重新設計進度報告機制
        }
        
        private void UpdateProgress(ProgressReport report)
        {
            Dispatcher.Invoke(() =>
            {
                _messageLabel.Content = report.Message;
                
                if (report.IsIndeterminate)
                {
                    _progressBar.IsIndeterminate = true;
                }
                else
                {
                    _progressBar.IsIndeterminate = false;
                    _progressBar.Value = report.Percentage;
                }
                
                if (report.IsCancelled)
                {
                    DialogResult = false;
                    Close();
                }
            });
        }
        
        private class CustomProgress : IProgress<ProgressReport>
        {
            private readonly ProgressDialog _dialog;
            
            public CustomProgress(ProgressDialog dialog)
            {
                _dialog = dialog;
            }
            
            public void Report(ProgressReport value)
            {
                _dialog.UpdateProgress(value);
            }
        }
    }
    
    /// <summary>
    /// 關於對話框
    /// </summary>
    public class AboutDialog : Window
    {
        private static readonly System.Reflection.Assembly _executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        
        public AboutDialog()
        {
            Title = "關於 RimWorld 翻譯工具";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            var grid = new Grid();
            grid.Margin = new Thickness(20);
            
            var titleText = new TextBlock
            {
                Text = "RimWorld 翻譯工具",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var versionText = new TextBlock
            {
                Text = $"版本: {_executingAssembly.GetName().Version}",
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var descriptionText = new TextBlock
            {
                Text = "一個專為 RimWorld 模組翻譯設計的工具，支援多語言翻譯管理、模組掃描、翻譯導入等功能。",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            var okButton = new Button
            {
                Content = "確定",
                Width = 80,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            okButton.Click += (s, e) => { Close(); };
            
            var stackPanel = new StackPanel();
            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(versionText);
            stackPanel.Children.Add(descriptionText);
            stackPanel.Children.Add(okButton);
            
            grid.Children.Add(stackPanel);
            Content = grid;
        }
    }
    
    /// <summary>
    /// 日誌檢視器對話框
    /// </summary>
    public class LogViewerDialog : Window
    {
        private readonly IPathService _pathService;
        
        public LogViewerDialog(IPathService pathService)
        {
            _pathService = pathService;
            Initialize();
        }
        
        public LogViewerDialog()
        {
            _pathService = null!;
            Initialize();
        }
        
        private void Initialize()
        {
            Title = "日誌檢視器";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var grid = new Grid();
            grid.Margin = new Thickness(10);
            
            var logTextBox = new TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.NoWrap
            };
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var refreshButton = new Button { Content = "重新整理", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var clearButton = new Button { Content = "清空", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var closeButton = new Button { Content = "關閉", Width = 80 };
            
            refreshButton.Click += async (s, e) => await LoadLogsAsync(logTextBox);
            clearButton.Click += (s, e) => { logTextBox.Clear(); };
            closeButton.Click += (s, e) => { Close(); };
            
            buttonPanel.Children.Add(refreshButton);
            buttonPanel.Children.Add(clearButton);
            buttonPanel.Children.Add(closeButton);
            
            Grid.SetRow(logTextBox, 0);
            Grid.SetRow(buttonPanel, 1);
            Grid.SetRowSpan(logTextBox, 1);
            
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            grid.Children.Add(logTextBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
            
            // 載入日誌
            Loaded += async (s, e) => await LoadLogsAsync(logTextBox);
        }
        
        private async Task LoadLogsAsync(TextBox logTextBox)
        {
            try
            {
                var logRoot = _pathService?.GetLogsDirectory() ?? System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RimWorldTranslationTool",
                    "Logs");
                
                if (System.IO.Directory.Exists(logRoot))
                {
                    var logFiles = System.IO.Directory.GetFiles(logRoot, "*.log")
                        .OrderByDescending(f => f)
                        .Take(5); // 只顯示最近5個日誌檔案
                    
                    var content = new System.Text.StringBuilder();
                    foreach (var file in logFiles)
                    {
                        content.AppendLine($"=== {System.IO.Path.GetFileName(file)} ===");
                        content.AppendLine(await System.IO.File.ReadAllTextAsync(file));
                        content.AppendLine();
                        content.AppendLine();
                    }
                    
                    logTextBox.Text = content.ToString();
                }
                else
                {
                    logTextBox.Text = "找不到日誌檔案";
                }
            }
            catch (Exception ex)
            {
                logTextBox.Text = $"載入日誌失敗: {ex.Message}";
            }
        }
    }
    
    /// <summary>
    /// 設定對話框
    /// </summary>
    public class SettingsDialog : Window
    {
        public SettingsDialog()
        {
            Title = "設定";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var grid = new Grid();
            grid.Margin = new Thickness(20);
            
            var settingsText = new TextBlock
            {
                Text = "設定功能開發中...",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var closeButton = new Button
            {
                Content = "關閉",
                Width = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 20)
            };
            closeButton.Click += (s, e) => { Close(); };
            
            grid.Children.Add(settingsText);
            grid.Children.Add(closeButton);
            
            Content = grid;
        }
    }
}
