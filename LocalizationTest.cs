using System;
using System.Globalization;

namespace RimWorldTranslationTool
{
    public class LocalizationTest
    {
        public static void TestLocalization()
        {
            Console.WriteLine("=== 測試 i18n 系統 ===");
            
            // 測試中文
            Console.WriteLine("\n--- 繁體中文 ---");
            LocalizationManager.SetCulture(new CultureInfo("zh-TW"));
            Console.WriteLine($"WindowTitle: {LocalizationManager.GetString("WindowTitle")}");
            Console.WriteLine($"TabSettings: {LocalizationManager.GetString("TabSettings")}");
            Console.WriteLine($"Error_Title: {LocalizationManager.GetString("Error_Title")}");
            
            // 測試英文
            Console.WriteLine("\n--- English ---");
            LocalizationManager.SetCulture(new CultureInfo("en-US"));
            Console.WriteLine($"WindowTitle: {LocalizationManager.GetString("WindowTitle")}");
            Console.WriteLine($"TabSettings: {LocalizationManager.GetString("TabSettings")}");
            Console.WriteLine($"Error_Title: {LocalizationManager.GetString("Error_Title")}");
            
            Console.WriteLine("\n=== 測試完成 ===");
        }
    }
}
