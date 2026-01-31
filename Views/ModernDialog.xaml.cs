using System.Windows;
using System.Windows.Media;

namespace RimWorldTranslationTool.Views
{
    public enum ModernDialogType
    {
        Success,
        Info,
        Warning,
        Error,
        Critical,
        Confirmation
    }

    public partial class ModernDialog : Window
    {
        public bool Result { get; private set; }

        public ModernDialog(string message, string title, ModernDialogType type, bool showCancel = false)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;

            if (showCancel)
            {
                SecondaryButton.Visibility = Visibility.Visible;
            }

            ApplyDialogStyle(type);
        }

        private void ApplyDialogStyle(ModernDialogType type)
        {
            switch (type)
            {
                case ModernDialogType.Success:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                    IconText.Text = "✓";
                    PrimaryButton.Background = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                    break;

                case ModernDialogType.Info:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
                    IconText.Text = "ℹ";
                    PrimaryButton.Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
                    break;

                case ModernDialogType.Warning:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                    IconText.Text = "⚠";
                    PrimaryButton.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                    break;

                case ModernDialogType.Error:
                case ModernDialogType.Critical:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                    IconText.Text = "✕";
                    PrimaryButton.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                    break;

                case ModernDialogType.Confirmation:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1));
                    IconText.Text = "?";
                    SecondaryButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        public static void ShowSuccess(string message, string title = "成功")
        {
            var dialog = new ModernDialog(message, title, ModernDialogType.Success);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        public static void ShowInfo(string message, string title = "資訊")
        {
            var dialog = new ModernDialog(message, title, ModernDialogType.Info);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        public static void ShowWarning(string message, string title = "警告")
        {
            var dialog = new ModernDialog(message, title, ModernDialogType.Warning);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        public static void ShowError(string message, string title = "錯誤")
        {
            var dialog = new ModernDialog(message, title, ModernDialogType.Error);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        public static bool ShowConfirmation(string message, string title = "確認")
        {
            var dialog = new ModernDialog(message, title, ModernDialogType.Confirmation, true);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}
