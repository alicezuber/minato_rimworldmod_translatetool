using System;
using System.IO;
using RimWorldTranslationTool.Services.Scanning;
using RimWorldTranslationTool.Services.Infrastructure;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool
{
    /// <summary>
    /// 語言檢測功能測試
    /// </summary>
    public static class TestLanguageDetection
    {
        public static void RunTest()
        {
            Console.WriteLine("=== 語言檢測功能測試 ===");
            
            // 初始化服務
            var loggerService = new LoggerService();
            var pathService = new PathService();
            var xmlParserService = new XmlParserService(loggerService);
            var modInfoService = new ModInfoService(xmlParserService, pathService, loggerService);
            
            // 測試案例 1: 不存在的路徑
            TestNonExistentPath(modInfoService);
            
            // 測試案例 2: 沒有 Languages 目錄的模組
            TestModWithoutLanguages(modInfoService);
            
            // 測試案例 3: 有 Languages 目錄但沒有語言資料夾
            TestModWithEmptyLanguages(modInfoService);
            
            // 測試案例 4: 有多種語言的模組
            TestModWithMultipleLanguages(modInfoService);
            
            Console.WriteLine("=== 測試完成 ===");
        }
        
        private static void TestNonExistentPath(ModInfoService modInfoService)
        {
            Console.WriteLine("\n1. 測試不存在的路徑:");
            var result = modInfoService.LoadModInfo("C:\\NonExistentMod");
            Console.WriteLine($"   結果: {(result == null ? "✅ 正確返回 null" : "❌ 應該返回 null")}");
        }
        
        private static void TestModWithoutLanguages(ModInfoService modInfoService)
        {
            Console.WriteLine("\n2. 測試沒有 Languages 目錄的模組:");
            // 這需要一個真實的模組路徑來測試
            Console.WriteLine("   需要真實模組路徑來測試");
        }
        
        private static void TestModWithEmptyLanguages(ModInfoService modInfoService)
        {
            Console.WriteLine("\n3. 測試有空的 Languages 目錄:");
            Console.WriteLine("   需要真實模組路徑來測試");
        }
        
        private static void TestModWithMultipleLanguages(ModInfoService modInfoService)
        {
            Console.WriteLine("\n4. 測試有多種語言的模組:");
            Console.WriteLine("   需要真實模組路徑來測試");
        }
    }
}
