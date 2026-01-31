using System;
using System.Windows;
using System.Windows.Media;

namespace RimWorldTranslationTool.Services.Theme
{
    /// <summary>
    /// 主題服務實作
    /// </summary>
    public class ThemeService : IThemeService
    {
        private AppTheme _currentTheme = AppTheme.Dark;

        public event EventHandler? ThemeChanged;

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

        public void LoadThemeFromSettings()
        {
            SetTheme(AppTheme.Dark);
        }

        private void ApplyTheme(AppTheme theme)
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            var resources = app.Resources;

            if (theme == AppTheme.Dark)
            {
                ApplyDarkTheme(resources);
            }
            else
            {
                ApplyLightTheme(resources);
            }
        }

        private static void ApplyDarkTheme(ResourceDictionary resources)
        {
            resources["BgPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x0F, 0x23));
            resources["BgSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E));
            resources["BgTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x42));
            resources["TextPrimaryBrush"] = new SolidColorBrush(Colors.White);
            resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
        }

        private static void ApplyLightTheme(ResourceDictionary resources)
        {
            resources["BgPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            resources["BgSecondaryBrush"] = new SolidColorBrush(Colors.White);
            resources["BgTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
            resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
            resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
        }
    }
}
