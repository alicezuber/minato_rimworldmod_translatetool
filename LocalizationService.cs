using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace RimWorldTranslationTool
{
    /// <summary>
    /// 原生 .NET 本地化服務，支援 WPF 綁定和動態語言切換
    /// </summary>
    public class LocalizationService : INotifyPropertyChanged
    {
        private static readonly Lazy<LocalizationService> _instance = 
            new Lazy<LocalizationService>(() => new LocalizationService());
        
        public static LocalizationService Instance => _instance.Value;
        
        private readonly ResourceManager _resourceManager;
        private CultureInfo _currentCulture;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        private LocalizationService()
        {
            _resourceManager = new ResourceManager(
                "RimWorldTranslationTool.Resources.Resources", 
                typeof(LocalizationService).Assembly);
            _currentCulture = CultureInfo.CurrentUICulture;
        }
        
        /// <summary>
        /// 當前語言文化
        /// </summary>
        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture.Name != value.Name)
                {
                    _currentCulture = value;
                    CultureInfo.CurrentCulture = value;
                    CultureInfo.CurrentUICulture = value;
                    
                    // 通知所有綁定的屬性更新
                    OnPropertyChanged(string.Empty);
                }
            }
        }
        
        /// <summary>
        /// 索引器 - 用於 XAML 綁定
        /// </summary>
        public string this[string key] => GetString(key);
        
        /// <summary>
        /// 取得本地化字串
        /// </summary>
        public string GetString(string key)
        {
            try
            {
                var value = _resourceManager.GetString(key, _currentCulture);
                return value ?? $"[{key}]";
            }
            catch
            {
                return $"[{key}]";
            }
        }
        
        /// <summary>
        /// 取得格式化的本地化字串
        /// </summary>
        public string GetString(string key, params object[] args)
        {
            try
            {
                var format = _resourceManager.GetString(key, _currentCulture);
                if (format == null) return $"[{key}]";
                return string.Format(format, args);
            }
            catch
            {
                return $"[{key}]";
            }
        }
        
        /// <summary>
        /// 切換語言
        /// </summary>
        public void SetLanguage(string cultureName)
        {
            CurrentCulture = new CultureInfo(cultureName);
        }
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #region 所有本地化字串屬性 - 用於 XAML 綁定
        
        // Window
        public string WindowTitle => GetString("WindowTitle");
        public string Subtitle => GetString("Subtitle");
        
        // Tab Headers
        public string TabSettings => GetString("TabSettings");
        public string TabModBrowser => GetString("TabModBrowser");
        public string TabModManager => GetString("TabModManager");
        
        // Settings Tab
        public string RimWorldPathSettings => GetString("RimWorldPathSettings");
        public string PathSettingsDescription => GetString("PathSettingsDescription");
        public string GameDirectory => GetString("GameDirectory");
        public string GameDirectoryDescription => GetString("GameDirectoryDescription");
        public string Browse => GetString("Browse");
        public string AutoDetectedPaths => GetString("AutoDetectedPaths");
        public string WorkshopPath => GetString("WorkshopPath");
        public string ConfigPath => GetString("ConfigPath");
        public string AutoDetectPaths => GetString("AutoDetectPaths");
        
        // ModsConfig Settings
        public string ModsConfigSettings => GetString("ModsConfigSettings");
        public string ModsConfigDescription => GetString("ModsConfigDescription");
        public string NoFileSelected => GetString("NoFileSelected");
        public string SelectFile => GetString("SelectFile");
        
        // Game Version
        public string GameVersion => GetString("GameVersion");
        public string GameVersionDescription => GetString("GameVersionDescription");
        public string CurrentVersion => GetString("CurrentVersion");
        
        // Mod Browser Tab
        public string ScanMods => GetString("ScanMods");
        public string ReadyToScan => GetString("ReadyToScan");
        public string PreparingScan => GetString("PreparingScan");
        
        // DataGrid Headers
        public string ColumnName => GetString("ColumnName");
        public string ColumnAuthor => GetString("ColumnAuthor");
        public string ColumnVersion => GetString("ColumnVersion");
        public string ColumnTraditionalChinese => GetString("ColumnTraditionalChinese");
        public string ColumnSimplifiedChinese => GetString("ColumnSimplifiedChinese");
        public string ColumnPatch => GetString("ColumnPatch");
        public string ColumnTranslatable => GetString("ColumnTranslatable");
        public string ColumnEnabled => GetString("ColumnEnabled");
        
        // Mod Details Panel
        public string ModDetails => GetString("ModDetails");
        public string TranslationStatus => GetString("TranslationStatus");
        public string RelatedTranslationPatches => GetString("RelatedTranslationPatches");
        public string SelectModToViewDetails => GetString("SelectModToViewDetails");
        
        // Mod Manager Tab
        public string ModPool => GetString("ModPool");
        public string ModPoolDescription => GetString("ModPoolDescription");
        public string EnabledList => GetString("EnabledList");
        public string EnabledListDescription => GetString("EnabledListDescription");
        
        // Action Buttons
        public string EnableSelected => GetString("EnableSelected");
        public string DisableSelected => GetString("DisableSelected");
        public string Refresh => GetString("Refresh");
        public string Diagnose => GetString("Diagnose");
        public string SaveConfig => GetString("SaveConfig");
        
        // Status Bar
        public string AppVersion => GetString("AppVersion");
        
        #endregion
    }
}
