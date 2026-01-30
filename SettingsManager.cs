using System;
using System.Threading.Tasks;
using RimWorldTranslationTool.Models;

namespace RimWorldTranslationTool
{
    /// <summary>
    /// (已過時) 設定管理器 - 請改用 ISettingsService。
    /// 為了向後相容暫時保留。
    /// </summary>
    [Obsolete("請使用 ISettingsService 透過 DI 注入。")]
    public class SettingsManager
    {
        private static readonly Lazy<SettingsManager> _instance = new Lazy<SettingsManager>(() => new SettingsManager());

        private SettingsManager()
        {
        }

        public static SettingsManager Instance => _instance.Value;

        public Task<AppSettings> LoadSettingsAsync() => throw new NotSupportedException("請使用 ISettingsService");
        public void TriggerAutoSave() => throw new NotSupportedException("請使用 ISettingsService");
        public Task ManualSaveAsync() => throw new NotSupportedException("請使用 ISettingsService");
        public void EnableAutoSave(int delaySeconds = 3) => throw new NotSupportedException("請使用 ISettingsService");
        public void EnableManualSaveMode() => throw new NotSupportedException("請使用 ISettingsService");
        public void DisableAutoSave() => throw new NotSupportedException("請使用 ISettingsService");
        public bool IsManualSaveMode() => throw new NotSupportedException("請使用 ISettingsService");
        public Task SaveSettingsAsync(AppSettings? settings = null) => throw new NotSupportedException("請使用 ISettingsService");
        public void UpdateSetting(Action<AppSettings> updateAction) => throw new NotSupportedException("請使用 ISettingsService");
        public AppSettings GetCurrentSettings() => throw new NotSupportedException("請使用 ISettingsService");
        public bool IsLoadingSettings() => throw new NotSupportedException("請使用 ISettingsService");
        public void Dispose()
        {
        }
    }

    public class SettingsLoadedEventArgs : EventArgs
    {
        public SettingsLoadedEventArgs(AppSettings settings)
        {
            Settings = settings;
        }

        public AppSettings Settings { get; }
    }

    public class SettingsSavedEventArgs : EventArgs
    {
        public SettingsSavedEventArgs(AppSettings settings)
        {
            Settings = settings;
        }

        public AppSettings Settings { get; }
    }
}
