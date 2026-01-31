using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace RimWorldTranslationTool.Services.Localization
{
    /// <summary>
    /// 本地化服務實作
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private static readonly Lazy<LocalizationService> _instance =
            new Lazy<LocalizationService>(() => new LocalizationService());

        private readonly ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        private LocalizationService()
        {
            _resourceManager = new ResourceManager(
                "RimWorldTranslationTool.Resources.Resources",
                typeof(LocalizationService).Assembly);
            _currentCulture = CultureInfo.CurrentUICulture;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static LocalizationService Instance => _instance.Value;

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                if (_currentCulture.Name != value.Name)
                {
                    _currentCulture = value;
                    CultureInfo.CurrentCulture = value;
                    CultureInfo.CurrentUICulture = value;
                    OnPropertyChanged(string.Empty);
                }
            }
        }

        public void SetLanguage(string languageCode)
        {
            try
            {
                CurrentCulture = new CultureInfo(languageCode);
            }
            catch (CultureNotFoundException)
            {
                CurrentCulture = new CultureInfo("zh-TW");
            }
        }

        public string GetString(string key)
        {
            return GetString(key, key);
        }

        public string GetString(string key, string defaultValue)
        {
            try
            {
                return _resourceManager.GetString(key, _currentCulture) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Localized Properties
        public string AppTitle => GetString("AppTitle", "RimWorld 翻譯工具");
        public string TabSettings => GetString("TabSettings", "設定");
        public string TabModBrowser => GetString("TabModBrowser", "模組瀏覽器");
        public string TabLocalMods => GetString("TabLocalMods", "本地模組");
        public string GamePath => GetString("GamePath", "遊戲路徑");
        public string ModsConfigPath => GetString("ModsConfigPath", "ModsConfig 路徑");
        public string Browse => GetString("Browse", "瀏覽");
        public string AutoDetect => GetString("AutoDetect", "自動偵測");
        public string ScanMods => GetString("ScanMods", "掃描模組");
        public string Save => GetString("Save", "儲存");
        public string Reset => GetString("Reset", "重設");
        public string ColumnModName => GetString("ColumnModName", "模組名稱");
        public string ColumnAuthor => GetString("ColumnAuthor", "作者");
        public string ColumnVersion => GetString("ColumnVersion", "版本");
        public string ColumnTraditionalChinese => GetString("ColumnTraditionalChinese", "繁中");
        public string ColumnSimplifiedChinese => GetString("ColumnSimplifiedChinese", "簡中");
        public string ColumnPatch => GetString("ColumnPatch", "補丁");
        public string ColumnTranslatable => GetString("ColumnTranslatable", "可翻譯");
        public string ColumnEnabled => GetString("ColumnEnabled", "啟用");
        public string ModDetails => GetString("ModDetails", "模組詳情");
        public string TranslationStatus => GetString("TranslationStatus", "翻譯狀態");
        public string SelectModToViewDetails => GetString("SelectModToViewDetails", "選擇模組以查看詳情");
        public string ScanLocalMods => GetString("ScanLocalMods", "掃描本地模組");
        public string ReadyToScanLocalMods => GetString("ReadyToScanLocalMods", "準備掃描本地模組");
        public string PreparingLocalModsScan => GetString("PreparingLocalModsScan", "準備掃描中...");
        public string RelatedTranslationPatches => GetString("RelatedTranslationPatches", "相關翻譯補丁");
        #endregion
    }
}
