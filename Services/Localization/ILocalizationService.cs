using System;
using System.ComponentModel;
using System.Globalization;

namespace RimWorldTranslationTool.Services.Localization
{
    /// <summary>
    /// 本地化服務介面
    /// </summary>
    public interface ILocalizationService : INotifyPropertyChanged
    {
        /// <summary>
        /// 當前語言文化
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// 設定語言
        /// </summary>
        void SetLanguage(string languageCode);

        /// <summary>
        /// 取得本地化字串
        /// </summary>
        string GetString(string key);

        /// <summary>
        /// 取得本地化字串，帶預設值
        /// </summary>
        string GetString(string key, string defaultValue);
    }
}
