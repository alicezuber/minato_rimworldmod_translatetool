using System;
using RimWorldTranslationTool.Models;

namespace RimWorldTranslationTool.Services.Settings
{
    /// <summary>
    /// 設定載入完成事件參數
    /// </summary>
    public class SettingsLoadedEventArgs : EventArgs
    {
        public SettingsLoadedEventArgs(AppSettings settings)
        {
            Settings = settings;
        }

        public AppSettings Settings { get; }
    }

    /// <summary>
    /// 設定保存完成事件參數
    /// </summary>
    public class SettingsSavedEventArgs : EventArgs
    {
        public SettingsSavedEventArgs(AppSettings settings)
        {
            Settings = settings;
        }

        public AppSettings Settings { get; }
    }
}
