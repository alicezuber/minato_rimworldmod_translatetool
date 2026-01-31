using System;

namespace RimWorldTranslationTool.Services.Theme
{
    /// <summary>
    /// 主題類型
    /// </summary>
    public enum AppTheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// 主題服務介面
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// 主題變更事件
        /// </summary>
        event EventHandler? ThemeChanged;

        /// <summary>
        /// 當前主題
        /// </summary>
        AppTheme CurrentTheme { get; }

        /// <summary>
        /// 是否為深色模式
        /// </summary>
        bool IsDarkMode { get; }

        /// <summary>
        /// 設定主題
        /// </summary>
        void SetTheme(AppTheme theme);

        /// <summary>
        /// 切換主題
        /// </summary>
        void ToggleTheme();

        /// <summary>
        /// 從設定載入主題
        /// </summary>
        void LoadThemeFromSettings();
    }
}
