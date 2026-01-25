using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Scanning;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool
{
    /// <summary>
    /// 翻譯映射功能測試
    /// </summary>
    public static class TestTranslationMapping
    {
        public static async Task RunTest()
        {
            Console.WriteLine("=== 翻譯映射功能測試 ===");
            
            // 初始化服務
            var loggerService = new LoggerService();
            var pathService = new PathService();
            var translationMappingService = new TranslationMappingService(pathService, loggerService);
            
            // 創建測試模組數據
            var testMods = CreateTestMods();
            
            // 測試翻譯模組識別
            TestTranslationModDetection(translationMappingService, testMods);
            
            // 測試映射建立
            await TestMappingBuilding(translationMappingService, testMods);
            
            Console.WriteLine("=== 測試完成 ===");
        }
        
        private static List<ModInfo> CreateTestMods()
        {
            var mods = new List<ModInfo>();
            
            // 模擬原始模組 A
            mods.Add(new ModInfo
            {
                Name = "Core",
                PackageId = "Core",
                FolderName = "Core",
                SupportedLanguages = "unknown"
            });
            
            // 模擬原始模組 B
            mods.Add(new ModInfo
            {
                Name = "JecsTools",
                PackageId = "JecsTools.JecsTools",
                FolderName = "JecsTools",
                SupportedLanguages = "unknown"
            });
            
            // 模擬翻譯模組 A' (翻譯 Core)
            mods.Add(new ModInfo
            {
                Name = "Core Chinese Translation",
                PackageId = "Core.Chinese",
                FolderName = "Core.Chinese",
                SupportedLanguages = "ChineseTraditional",
                IsTranslationPatch = true
            });
            
            // 模擬翻譯模組 B' (翻譯 JecsTools)
            mods.Add(new ModInfo
            {
                Name = "JecsTools Chinese Translation",
                PackageId = "JecsTools.Chinese",
                FolderName = "JecsTools.Chinese",
                SupportedLanguages = "ChineseTraditional",
                IsTranslationPatch = true
            });
            
            return mods;
        }
        
        private static void TestTranslationModDetection(ITranslationMappingService service, List<ModInfo> mods)
        {
            Console.WriteLine("\n1. 測試翻譯模組識別:");
            
            foreach (var mod in mods)
            {
                bool isTranslation = service.IsTranslationMod(mod);
                Console.WriteLine($"   {mod.Name} (PackageId: {mod.PackageId}): {(isTranslation ? "✅ 翻譯模組" : "❌ 原始模組")}");
            }
        }
        
        private static async Task TestMappingBuilding(ITranslationMappingService service, List<ModInfo> mods)
        {
            Console.WriteLine("\n2. 測試映射建立:");
            
            try
            {
                var mappings = await service.BuildTranslationMappingsAsync(mods);
                
                Console.WriteLine($"   建立了 {mappings.Count} 個映射關係:");
                
                foreach (var mapping in mappings)
                {
                    var targetMod = mods.FirstOrDefault(m => m.PackageId == mapping.Key);
                    if (targetMod != null)
                    {
                        Console.WriteLine($"   目標模組: {targetMod.Name}");
                        foreach (var translationMod in mapping.Value)
                        {
                            Console.WriteLine($"     ← 翻譯補丁: {translationMod.Name}");
                        }
                    }
                }
                
                // 測試獲取翻譯補丁
                Console.WriteLine("\n3. 測試獲取翻譯補丁:");
                foreach (var mod in mods.Where(m => !m.IsTranslationPatch))
                {
                    var patches = service.GetTranslationPatchesForMod(mod, mappings);
                    Console.WriteLine($"   {mod.Name}: {patches.Count} 個翻譯補丁");
                    foreach (var patch in patches)
                    {
                        Console.WriteLine($"     - {patch.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 映射建立失敗: {ex.Message}");
            }
        }
    }
}
