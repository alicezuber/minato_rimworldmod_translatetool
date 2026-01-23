using System.Globalization;

namespace RimWorldTranslationTool
{
    /// <summary>
    /// 靜態本地化輔助類別 - 用於 C# 程式碼中取得本地化字串
    /// </summary>
    public static class LocalizationManager
    {
        /// <summary>
        /// 取得本地化字串
        /// </summary>
        public static string GetString(string key)
        {
            return LocalizationService.Instance.GetString(key);
        }

        /// <summary>
        /// 取得格式化的本地化字串
        /// </summary>
        public static string GetString(string key, params object[] args)
        {
            return LocalizationService.Instance.GetString(key, args);
        }

        /// <summary>
        /// 設定當前語言文化
        /// </summary>
        public static void SetCulture(CultureInfo culture)
        {
            LocalizationService.Instance.CurrentCulture = culture;
        }
        
        /// <summary>
        /// 設定當前語言
        /// </summary>
        public static void SetLanguage(string cultureName)
        {
            LocalizationService.Instance.SetLanguage(cultureName);
        }
    }
}
