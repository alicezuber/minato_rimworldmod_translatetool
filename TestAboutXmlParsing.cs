using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Scanning;
using RimWorldTranslationTool.Services.Infrastructure;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool
{
    /// <summary>
    /// About.xml 完整解析功能測試
    /// </summary>
    public static class TestAboutXmlParsing
    {
        public static async Task RunTest()
        {
            Console.WriteLine("=== About.xml 完整解析功能測試 ===");
            
            // 初始化服務
            var loggerService = new LoggerService();
            var pathService = new PathService();
            var xmlParserService = new XmlParserService(loggerService);
            var modInfoService = new ModInfoService(xmlParserService, pathService, loggerService);
            
            // 測試完整的 About.xml 解析
            await TestCompleteAboutXmlParsing(xmlParserService, modInfoService);
            
            Console.WriteLine("=== 測試完成 ===");
        }
        
        private static async Task TestCompleteAboutXmlParsing(IXmlParserService xmlParser, IModInfoService modInfoService)
        {
            Console.WriteLine("\n1. 測試完整 About.xml 解析:");
            
            try
            {
                // 使用 RJW 的 About.xml 作為測試（如果路徑存在）
                string testModPath = @"d:\01_Game\PC_Platform\Steam\steamapps\common\RimWorld\Mods\rjw";
                
                if (System.IO.Directory.Exists(testModPath))
                {
                    Console.WriteLine($"   測試模組路徑: {testModPath}");
                    
                    var modInfo = modInfoService.LoadModInfo(testModPath);
                    
                    if (modInfo != null)
                    {
                        Console.WriteLine($"   ✅ 模組名稱: {modInfo.Name}");
                        Console.WriteLine($"   ✅ 作者: {modInfo.Author}");
                        Console.WriteLine($"   ✅ PackageId: {modInfo.PackageId}");
                        Console.WriteLine($"   ✅ 模組版本: {modInfo.ModVersion}");
                        Console.WriteLine($"   ✅ 支援版本: {modInfo.SupportedVersions}");
                        Console.WriteLine($"   ✅ 官方網址: {modInfo.Url}");
                        Console.WriteLine($"   ✅ 模組來源: {modInfo.Source}");
                        
                        // 測試描述（最重要）
                        Console.WriteLine($"   ✅ 描述長度: {modInfo.Description.Length} 字符");
                        if (modInfo.Description.Length > 0)
                        {
                            Console.WriteLine($"   ✅ 描述預覽: {modInfo.Description.Substring(0, Math.Min(100, modInfo.Description.Length))}...");
                        }
                        
                        // 測試依賴關係
                        Console.WriteLine($"   ✅ 模組依賴數量: {modInfo.ModDependencies.Count}");
                        foreach (var dep in modInfo.ModDependencies.Take(3))
                        {
                            Console.WriteLine($"      - {dep.DisplayName} ({dep.PackageId})");
                        }
                        
                        // 測試版本特定依賴
                        Console.WriteLine($"   ✅ 版本特定依賴數量: {modInfo.ModDependenciesByVersion.Count}");
                        var versionGroups = modInfo.ModDependenciesByVersion
                            .GroupBy(d => d.TargetVersion)
                            .ToList();
                        foreach (var group in versionGroups.Take(3))
                        {
                            Console.WriteLine($"      - 版本 {group.Key}: {group.Count()} 個依賴");
                        }
                        
                        // 測試載入順序
                        Console.WriteLine($"   ✅ 需要在之後載入的模組: {modInfo.LoadAfter.Count} 個");
                        foreach (var loadAfter in modInfo.LoadAfter)
                        {
                            Console.WriteLine($"      - {loadAfter}");
                        }
                        
                        // 測試不相容模組
                        Console.WriteLine($"   ✅ 不相容模組: {modInfo.IncompatibleWith.Count} 個");
                        foreach (var incompatible in modInfo.IncompatibleWith.Take(5))
                        {
                            Console.WriteLine($"      - {incompatible}");
                        }
                        
                        Console.WriteLine("   ✅ 完整解析測試成功！");
                    }
                    else
                    {
                        Console.WriteLine("   ❌ 模組載入失敗");
                    }
                }
                else
                {
                    Console.WriteLine("   ⚠️ 測試模組路徑不存在，跳過實際檔案測試");
                    
                    // 測試 XML 解析方法的直接調用
                    TestXmlParsingMethods(xmlParser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 解析測試失敗: {ex.Message}");
            }
        }
        
        private static void TestXmlParsingMethods(IXmlParserService xmlParser)
        {
            Console.WriteLine("\n2. 測試 XML 解析方法:");
            
            // 創建測試 XML
            var testXml = @"<?xml version='1.0' encoding='utf-8'?>
<ModMetaData>
    <name>測試模組</name>
    <author>測試作者</author>
    <packageId>test.mod</packageId>
    <modVersion>1.0.0</modVersion>
    <url>https://example.com</url>
    <supportedVersions>
        <li>1.4</li>
        <li>1.5</li>
    </supportedVersions>
    <description>這是一個測試模組的描述。
包含多行文字和詳細說明。</description>
    <modDependencies>
        <li>
            <packageId>core</packageId>
            <displayName>Core</displayName>
        </li>
    </modDependencies>
    <loadAfter>
        <li>core</li>
    </loadAfter>
    <incompatibleWith>
        <li>incompatible.mod</li>
    </incompatibleWith>
</ModMetaData>";

            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(testXml);
                var root = doc.Root;
                
                if (root != null)
                {
                    Console.WriteLine($"   ✅ 名稱: {xmlParser.GetElementValue(root, "name")}");
                    Console.WriteLine($"   ✅ 作者: {xmlParser.GetElementValue(root, "author")}");
                    Console.WriteLine($"   ✅ PackageId: {xmlParser.GetElementValue(root, "packageId")}");
                    Console.WriteLine($"   ✅ 版本: {xmlParser.GetModVersion(root)}");
                    Console.WriteLine($"   ✅ URL: {xmlParser.GetUrl(root)}");
                    Console.WriteLine($"   ✅ 支援版本: {xmlParser.GetVersionsString(root.Element("supportedVersions"))}");
                    Console.WriteLine($"   ✅ 描述: {xmlParser.GetDescription(root).Substring(0, 20)}...");
                    
                    var deps = xmlParser.GetModDependencies(root);
                    Console.WriteLine($"   ✅ 依賴數量: {deps.Count}");
                    
                    var loadAfter = xmlParser.GetLoadAfterList(root);
                    Console.WriteLine($"   ✅ LoadAfter 數量: {loadAfter.Count}");
                    
                    var incompatible = xmlParser.GetIncompatibleWithList(root);
                    Console.WriteLine($"   ✅ 不相容數量: {incompatible.Count}");
                    
                    Console.WriteLine("   ✅ XML 解析方法測試成功！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ XML 解析測試失敗: {ex.Message}");
            }
        }
    }
}
