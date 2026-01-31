using System.Windows;
using System.Windows.Controls;

namespace RimWorldTranslationTool.Views
{
    public partial class MainShell : Window
    {
        private readonly SettingsView _settingsView;
        private readonly ModBrowserView _modBrowserView;
        private readonly ModManagerView _modManagerView;

        public MainShell(SettingsView settingsView, ModBrowserView modBrowserView, ModManagerView modManagerView)
        {
            InitializeComponent();
            
            _settingsView = settingsView;
            _modBrowserView = modBrowserView;
            _modManagerView = modManagerView;
            
            ContentArea.Content = _settingsView;
        }

        private void NavButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string page)
            {
                ContentArea.Content = page switch
                {
                    "Settings" => _settingsView,
                    "ModBrowser" => _modBrowserView,
                    "ModManager" => _modManagerView,
                    _ => _settingsView
                };
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
