using System.Resources;
using System.Reflection;
using System.Globalization;

namespace RimWorldTranslationTool
{
    public static class LocalizationManager
    {
        private static readonly ResourceManager _resourceManager = 
            new ResourceManager("RimWorldTranslationTool.Resources.Resources", typeof(LocalizationManager).Assembly);

        public static string GetString(string key)
        {
            try
            {
                var value = _resourceManager.GetString(key, CultureInfo.CurrentCulture);
                return value ?? $"[{key}]";
            }
            catch
            {
                return $"[{key}]";
            }
        }

        public static string GetString(string key, CultureInfo culture)
        {
            try
            {
                var value = _resourceManager.GetString(key, culture);
                return value ?? $"[{key}]";
            }
            catch
            {
                return $"[{key}]";
            }
        }

        public static void SetCulture(CultureInfo culture)
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            
            // 通知 WPFLocalizeExtension 更新文化
            WPFLocalizeExtension.Engine.LocalizeDictionary.Instance.Culture = culture;
        }
    }
}
