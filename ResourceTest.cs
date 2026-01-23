using System;
using System.Resources;
using System.Reflection;
using System.Globalization;

namespace RimWorldTranslationTool
{
    public class ResourceTest
    {
        public static void TestResources()
        {
            Console.WriteLine("=== 資源檔案測試 ===");
            
            try
            {
                // 測試預設資源
                var defaultManager = new ResourceManager("RimWorldTranslationTool.Resources.Resources", Assembly.GetExecutingAssembly());
                
                // 測試繁體中文
                var zhTW = new CultureInfo("zh-TW");
                var titleZhTW = defaultManager.GetString("WindowTitle", zhTW);
                var settingsZhTW = defaultManager.GetString("TabSettings", zhTW);
                
                Console.WriteLine($"zh-TW WindowTitle: '{titleZhTW}'");
                Console.WriteLine($"zh-TW TabSettings: '{settingsZhTW}'");
                
                // 測試英文
                var enUS = new CultureInfo("en-US");
                var titleEnUS = defaultManager.GetString("WindowTitle", enUS);
                var settingsEnUS = defaultManager.GetString("TabSettings", enUS);
                
                Console.WriteLine($"en-US WindowTitle: '{titleEnUS}'");
                Console.WriteLine($"en-US TabSettings: '{settingsEnUS}'");
                
                // 檢查是否載入成功
                if (string.IsNullOrEmpty(titleZhTW) || titleZhTW.Contains("WindowTitle"))
                {
                    Console.WriteLine("❌ 資源檔案載入失敗");
                }
                else
                {
                    Console.WriteLine("✅ 資源檔案載入成功");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 測試發生錯誤: {ex.Message}");
                Console.WriteLine($"錯誤詳情: {ex}");
            }
            
            Console.WriteLine("=== 測試完成 ===");
        }
    }
}
