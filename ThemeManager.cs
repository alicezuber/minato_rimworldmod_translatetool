using System;
using System.Windows;
using System.Windows.Media;

namespace RimWorldTranslationTool
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public class ThemeManager
    {
        private static ThemeManager? _instance;
        private AppTheme _currentTheme = AppTheme.Light;

        private ThemeManager()
        {
        }

        public event EventHandler? ThemeChanged;

        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsDarkMode => CurrentTheme == AppTheme.Dark;

        public void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            ApplyTheme(theme);
        }

        public void ToggleTheme()
        {
            SetTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
        }

        public string GetThemeName() => CurrentTheme.ToString();

        public void LoadThemeFromSettings(string? themeName)
        {
            if (string.IsNullOrEmpty(themeName))
            {
                SetTheme(AppTheme.Light);
                return;
            }

            if (Enum.TryParse<AppTheme>(themeName, true, out var theme))
            {
                SetTheme(theme);
            }
            else
            {
                SetTheme(AppTheme.Light);
            }
        }

        private void ApplyTheme(AppTheme theme)
        {
            var resources = Application.Current.Resources;

            if (theme == AppTheme.Dark)
            {
                // 深色模式色彩
                resources["BackgroundColor"] = Color.FromRgb(0x0F, 0x17, 0x2A);           // #0F172A
                resources["CardBackgroundColor"] = Color.FromRgb(0x1E, 0x29, 0x3B);       // #1E293B
                resources["BorderColor"] = Color.FromRgb(0x33, 0x41, 0x55);               // #334155
                resources["TextPrimaryColor"] = Color.FromRgb(0xF1, 0xF5, 0xF9);          // #F1F5F9
                resources["TextSecondaryColor"] = Color.FromRgb(0x94, 0xA3, 0xB8);        // #94A3B8
                resources["PrimaryColor"] = Color.FromRgb(0x3B, 0x82, 0xF6);              // #3B82F6
                resources["PrimaryHoverColor"] = Color.FromRgb(0x60, 0xA5, 0xFA);         // #60A5FA

                // 更新 Brush 資源
                resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
                resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
                resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
                resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
                resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                resources["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
                resources["PrimaryHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));

                // 深色模式特定色彩
                resources["HeaderBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
                resources["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
                resources["HoverBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
                resources["RowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
                resources["AlternateRowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x27, 0x33, 0x47));
                resources["SelectedRowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x5F));
                resources["InfoBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x27, 0x33, 0x47));
            }
            else
            {
                // 淺色模式色彩
                resources["BackgroundColor"] = Color.FromRgb(0xF8, 0xFA, 0xFC);           // #F8FAFC
                resources["CardBackgroundColor"] = Color.FromRgb(0xFF, 0xFF, 0xFF);       // #FFFFFF
                resources["BorderColor"] = Color.FromRgb(0xE2, 0xE8, 0xF0);               // #E2E8F0
                resources["TextPrimaryColor"] = Color.FromRgb(0x1E, 0x29, 0x3B);          // #1E293B
                resources["TextSecondaryColor"] = Color.FromRgb(0x64, 0x74, 0x8B);        // #64748B
                resources["PrimaryColor"] = Color.FromRgb(0x25, 0x63, 0xEB);              // #2563EB
                resources["PrimaryHoverColor"] = Color.FromRgb(0x1D, 0x4E, 0xD8);         // #1D4ED8

                // 更新 Brush 資源
                resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
                resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
                resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
                resources["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
                resources["PrimaryHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8));

                // 淺色模式特定色彩
                resources["HeaderBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["InputBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["HoverBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
                resources["RowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["AlternateRowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
                resources["SelectedRowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF));
                resources["InfoBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            }
        }
    }
}
