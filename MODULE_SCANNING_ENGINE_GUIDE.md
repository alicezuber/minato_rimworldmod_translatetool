# 模組掃描引擎使用指南

## 📋 概述

本文檔介紹如何使用重構後的 RimWorld 模組掃描引擎。該引擎採用分層架構設計，提供解耦、可復用的模組掃描和資訊提取功能。

## 🏗️ 架構概覽

```
┌─────────────────────────────────────┐
│           UI Layer (MainWindow)    │
├─────────────────────────────────────┤
│         Service Layer               │
│  ┌─────────────┐ ┌─────────────────┐│
│  │IModScanner  │ │IModInfo        ││
│  │Service      │ │Service         ││
│  └─────────────┘ └─────────────────┘│
│  ┌─────────────┐ ┌─────────────────┐│
│  │ITranslation │ │XmlParser       ││
│  │Mapping      │ │Service         ││
│  │Service      │ │                 ││
│  └─────────────┘ └─────────────────┘│
├─────────────────────────────────────┤
│         Infrastructure Layer        │
│  ┌─────────────┐ ┌─────────────────┐│
│  │IPathService │ │ILoggerService  ││
│  │(現有)       │ │                 ││
│  └─────────────┘ └─────────────────┘│
└─────────────────────────────────────┘
```

## 🔧 核心服務介面

### 1. IModScannerService - 模組掃描服務

```csharp
public interface IModScannerService
{
    /// <summary>
    /// 掃描指定遊戲路徑下的所有模組
    /// </summary>
    /// <param name="gamePath">RimWorld 遊戲路徑</param>
    /// <param name="progress">進度報告</param>
    /// <returns>找到的模組列表</returns>
    Task<List<ModInfo>> ScanModsAsync(string gamePath, IProgress<ScanProgress> progress = null);
    
    /// <summary>
    /// 只掃描本地模組資料夾
    /// </summary>
    /// <param name="gamePath">RimWorld 遊戲路徑</param>
    /// <param name="progress">進度報告</param>
    /// <returns>找到的本地模組列表</returns>
    Task<List<ModInfo>> ScanLocalModsAsync(string gamePath, IProgress<ScanProgress> progress = null);
}
```

### 2. IModInfoService - 模組資訊服務

```csharp
public interface IModInfoService
{
    /// <summary>
    /// 載入單一模組的資訊
    /// </summary>
    /// <param name="modPath">模組路徑</param>
    /// <returns>模組資訊，如果載入失敗返回 null</returns>
    ModInfo LoadModInfo(string modPath);
    
    /// <summary>
    /// 檢查是否為有效的模組目錄
    /// </summary>
    /// <param name="path">要檢查的路徑</param>
    /// <returns>是否為有效模組目錄</returns>
    bool IsValidModDirectory(string path);
}
```

### 3. IXmlParserService - XML 解析服務

```csharp
public interface IXmlParserService
{
    /// <summary>
    /// 載入並解析 XML 檔案
    /// </summary>
    /// <param name="filePath">XML 檔案路徑</param>
    /// <returns>解析的 XDocument，失敗返回 null</returns>
    XDocument LoadXml(string filePath);
    
    /// <summary>
    /// 安全獲取 XML 元素值
    /// </summary>
    /// <param name="parent">父元素</param>
    /// <param name="elementName">元素名稱</param>
    /// <returns>元素值，不存在返回空字串</returns>
    string GetElementValue(XElement parent, string elementName);
    
    /// <summary>
    /// 獲取版本列表字串
    /// </summary>
    /// <param name="versionsElement">版本元素</param>
    /// <returns>版本字串，逗號分隔</returns>
    string GetVersionsString(XElement versionsElement);
}
```

### 4. ITranslationMappingService - 翻譯映射服務

```csharp
public interface ITranslationMappingService
{
    /// <summary>
    /// 建立翻譯模組與目標模組的映射關係
    /// </summary>
    /// <param name="allMods">所有模組列表</param>
    /// <returns>映射字典：Key 為目標模組 PackageId，Value 為翻譯模組列表</returns>
    Task<Dictionary<string, List<ModInfo>>> BuildTranslationMappingsAsync(IEnumerable<ModInfo> allMods);
    
    /// <summary>
    /// 檢查模組是否為翻譯模組
    /// </summary>
    /// <param name="mod">要檢查的模組</param>
    /// <returns>是否為翻譯模組</returns>
    bool IsTranslationMod(ModInfo mod);
    
    /// <summary>
    /// 獲取翻譯模組的目標模組列表
    /// </summary>
    /// <param name="translationMod">翻譯模組</param>
    /// <param name="allMods">所有模組列表</param>
    /// <returns>目標模組列表</returns>
    Task<List<ModInfo>> GetTargetModsForTranslationAsync(ModInfo translationMod, IEnumerable<ModInfo> allMods);
    
    /// <summary>
    /// 獲取模組的翻譯補丁列表
    /// </summary>
    /// <param name="targetMod">目標模組</param>
    /// <param name="mappings">映射字典</param>
    /// <returns>翻譯補丁列表</returns>
    List<ModInfo> GetTranslationPatchesForMod(ModInfo targetMod, Dictionary<string, List<ModInfo>> mappings);
}
```

## 📦 依賴設置

### 必要的依賴

```csharp
using RimWorldTranslationTool.Services.Scanning;
using RimWorldTranslationTool.Services.Infrastructure;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;
```

### 服務初始化

```csharp
// 1. 初始化基礎設施服務
var loggerService = new LoggerService();
var pathService = new PathService();
var xmlParserService = new XmlParserService(loggerService);

// 2. 初始化核心服務
var modInfoService = new ModInfoService(xmlParserService, pathService, loggerService);
var modScannerService = new ModScannerService(modInfoService, pathService, loggerService);
var translationMappingService = new TranslationMappingService(pathService, loggerService);
```

## 🚀 基本使用方法

### 1. 掃描所有模組

```csharp
public async Task<List<ModInfo>> ScanAllMods(string gamePath)
{
    // 初始化服務
    var services = InitializeServices();
    
    // 設置進度報告
    var progress = new Progress<ScanProgress>(p =>
    {
        Console.WriteLine($"掃描進度: {p.Processed}/{p.Total} - {p.PercentComplete:F1}%");
        Console.WriteLine($"當前模組: {p.CurrentMod}");
    });
    
    // 執行掃描
    var mods = await services.modScannerService.ScanModsAsync(gamePath, progress);
    
    Console.WriteLine($"掃描完成，找到 {mods.Count} 個模組");
    return mods;
}
```

### 2. 只掃描本地模組

```csharp
public async Task<List<ModInfo>> ScanLocalModsOnly(string gamePath)
{
    var services = InitializeServices();
    
    var localMods = await services.modScannerService.ScanLocalModsAsync(gamePath);
    
    Console.WriteLine($"本地模組掃描完成，找到 {localMods.Count} 個模組");
    return localMods;
}
```

### 3. 載入單一模組資訊

```csharp
public ModInfo LoadSingleMod(string modPath)
{
    var services = InitializeServices();
    
    // 檢查是否為有效模組
    if (!services.modInfoService.IsValidModDirectory(modPath))
    {
        Console.WriteLine("無效的模組目錄");
        return null;
    }
    
    // 載入模組資訊
    var modInfo = services.modInfoService.LoadModInfo(modPath);
    
    if (modInfo != null)
    {
        Console.WriteLine($"模組名稱: {modInfo.Name}");
        Console.WriteLine($"作者: {modInfo.Author}");
        Console.WriteLine($"PackageId: {modInfo.PackageId}");
        Console.WriteLine($"支援版本: {modInfo.SupportedVersions}");
        Console.WriteLine($"支援語言: {modInfo.SupportedLanguages}");
        Console.WriteLine($"模組來源: {modInfo.Source}");
        Console.WriteLine($"有翻譯模組: {modInfo.HasTranslationMod}");
        Console.WriteLine($"翻譯補丁語言: {modInfo.TranslationPatchLanguages}");
    }
    
    return modInfo;
}
```

### 4. 建立翻譯映射關係

```csharp
public async Task BuildTranslationMappings(List<ModInfo> allMods)
{
    var services = InitializeServices();
    
    // 建立翻譯映射
    var mappings = await services.translationMappingService.BuildTranslationMappingsAsync(allMods);
    
    Console.WriteLine($"翻譯映射建立完成，共 {mappings.Count} 個目標模組有翻譯");
    
    // 顯示映射關係
    foreach (var mapping in mappings)
    {
        var targetMod = allMods.FirstOrDefault(m => m.PackageId == mapping.Key);
        if (targetMod != null)
        {
            Console.WriteLine($"\n目標模組: {targetMod.Name}");
            foreach (var translationMod in mapping.Value)
            {
                Console.WriteLine($"  ← 翻譯補丁: {translationMod.Name} (語言: {translationMod.SupportedLanguages})");
            }
        }
    }
}
```

## 📊 ModInfo 資料模型

### 模組來源枚舉

```csharp
public enum ModSource
{
    Unknown,
    Local,      // 本地模組
    Steam,      // Steam Workshop
    Official    // 官方核心模組
}
```

### 完整的 ModInfo 模型

```csharp
public class ModInfo
{
    // 基本資訊
    public string FolderName { get; set; } = "";           // 模組資料夾名稱
    public string Name { get; set; } = "";                 // 模組顯示名稱
    public string Author { get; set; } = "";               // 作者
    public string PackageId { get; set; } = "";            // 唯一識別符
    public string SupportedVersions { get; set; } = "";    // 支援的遊戲版本
    public string SupportedLanguages { get; set; } = "unknown"; // 支援的語言
    public bool IsVersionCompatible { get; set; } = true;   // 版本相容性
    public BitmapImage? PreviewImage { get; set; }         // 預覽圖片
    
    // 完整 About.xml 支援
    public string Description { get; set; } = "";           // 模組描述（最重要）
    public string Url { get; set; } = "";                  // 模組官方網址
    public string ModVersion { get; set; } = "";            // 模組版本
    public List<ModDependency> ModDependencies { get; set; } = new List<ModDependency>();  // 模組依賴
    public List<ModDependency> ModDependenciesByVersion { get; set; } = new List<ModDependency>();  // 版本特定依賴
    public List<string> LoadAfter { get; set; } = new List<string>();  // 需要在這些模組之後載入
    public List<string> IncompatibleWith { get; set; } = new List<string>();  // 不相容的模組
    
    // 新增：模組來源
    public ModSource Source { get; set; } = ModSource.Unknown;
    
    // 新增：翻譯相關信息
    public bool HasTranslationMod { get; set; } = false;  // 是否有翻譯模組
    public string TranslationPatchLanguages { get; set; } = "none";  // 翻譯補丁支持的語言
    
    // 新增：翻譯關聯信息
    public List<string> TargetModPackageIds { get; set; } = new List<string>();  // 此翻譯模組的目標模組
    public List<string> TranslationPatchPackageIds { get; set; } = new List<string>();  // 翻譯此模組的補丁
    
    // 舊有屬性（保持相容性）
    public string HasChineseTraditional { get; set; } = "無";
    public string HasChineseSimplified { get; set; } = "無";
    public string HasTranslationPatch { get; set; } = "無";
    public string CanTranslate { get; set; } = "否";
    public bool IsEnabled { get; set; } = false;
    public bool IsTranslationPatch { get; set; } = false;
}
```

### ModDependency 模型

```csharp
public class ModDependency
{
    public string PackageId { get; set; } = "";        // 依賴模組的 PackageId
    public string DisplayName { get; set; } = "";      // 依賴模組的顯示名稱
    public string SteamWorkshopUrl { get; set; } = ""; // Steam Workshop 連結
    public string DownloadUrl { get; set; } = "";      // 下載連結
    public string TargetVersion { get; set; } = "";    // 目標版本（用於版本特定依賴)
}
```

## 🔍 進階使用

### 1. 自定義進度報告

```csharp
public class CustomProgressReporter : IProgress<ScanProgress>
{
    public void Report(ScanProgress value)
    {
        // 自定義進度顯示邏輯
        UpdateProgressBar(value.PercentComplete);
        UpdateStatusText(value.Status);
        LogCurrentMod(value.CurrentMod);
        
        // 可以添加更多自定義邏輯
        if (value.Processed == value.Total)
        {
            ShowCompletionMessage();
        }
    }
}
```

### 2. 錯誤處理

```csharp
public async Task<List<ModInfo>> ScanWithErrorHandling(string gamePath)
{
    try
    {
        var services = InitializeServices();
        return await services.modScannerService.ScanModsAsync(gamePath);
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine($"找不到目錄: {ex.Message}");
        return new List<ModInfo>();
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"權限不足: {ex.Message}");
        return new List<ModInfo>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"掃描失敗: {ex.Message}");
        return new List<ModInfo>();
    }
}
```

### 3. 模組篩選

```csharp
public List<ModInfo> FilterMods(List<ModInfo> mods, string gameVersion)
{
    return mods.Where(mod => 
        mod.IsVersionCompatible && 
        mod.SupportedVersions.Contains(gameVersion)
    ).ToList();
}

public List<ModInfo> GetTranslationMods(List<ModInfo> mods)
{
    return mods.Where(mod => 
        mod.SupportedLanguages != "unknown" ||
        mod.IsTranslationPatch
    ).ToList();
}

public List<ModInfo> GetModsBySource(List<ModInfo> mods, ModSource source)
{
    return mods.Where(mod => mod.Source == source).ToList();
}

public List<ModInfo> GetModsWithTranslationSupport(List<ModInfo> mods)
{
    return mods.Where(mod => mod.HasTranslationMod).ToList();
}
```

### 4. 翻譯映射分析

```csharp
public void AnalyzeTranslationMappings(Dictionary<string, List<ModInfo>> mappings, List<ModInfo> allMods)
{
    Console.WriteLine("=== 翻譯映射分析 ===");
    
    // 統計翻譯覆蓋率
    int totalMods = allMods.Count;
    int modsWithTranslation = mappings.Count;
    double coverageRate = (double)modsWithTranslation / totalMods * 100;
    
    Console.WriteLine($"翻譯覆蓋率: {coverageRate:F1}% ({modsWithTranslation}/{totalMods})");
    
    // 統計語言分佈
    var languageStats = new Dictionary<string, int>();
    foreach (var mapping in mappings.Values)
    {
        foreach (var translationMod in mapping)
        {
            var languages = translationMod.SupportedLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var lang in languages)
            {
                var trimmedLang = lang.Trim();
                if (trimmedLang != "unknown")
                {
                    languageStats[trimmedLang] = languageStats.GetValueOrDefault(trimmedLang, 0) + 1;
                }
            }
        }
    }
    
    Console.WriteLine("\n語言分佈:");
    foreach (var (language, count) in languageStats.OrderByDescending(x => x.Value))
    {
        Console.WriteLine($"  {language}: {count} 個翻譯補丁");
    }
    
    // 找出最受歡迎的目標模組
    var popularTargets = mappings.OrderByDescending(x => x.Value.Count).Take(5);
    Console.WriteLine("\n最受歡迎的翻譯目標:");
    foreach (var (targetPackageId, translations) in popularTargets)
    {
        var targetMod = allMods.FirstOrDefault(m => m.PackageId == targetPackageId);
        if (targetMod != null)
        {
            Console.WriteLine($"  {targetMod.Name}: {translations.Count} 個翻譯補丁");
        }
    }
}
```

### 5. 完整 About.xml 解析

```csharp
public void AnalyzeModInfo(ModInfo modInfo)
{
    Console.WriteLine($"=== 模組分析: {modInfo.Name} ===");
    
    // 基本資訊
    Console.WriteLine($"作者: {modInfo.Author}");
    Console.WriteLine($"PackageId: {modInfo.PackageId}");
    Console.WriteLine($"模組版本: {modInfo.ModVersion}");
    Console.WriteLine($"支援版本: {modInfo.SupportedVersions}");
    Console.WriteLine($"模組來源: {modInfo.Source}");
    
    // 描述（最重要）
    if (!string.IsNullOrEmpty(modInfo.Description))
    {
        Console.WriteLine($"\n模組描述:");
        Console.WriteLine(modInfo.Description.Substring(0, Math.Min(200, modInfo.Description.Length)));
        if (modInfo.Description.Length > 200)
        {
            Console.WriteLine("...(描述已截斷)");
        }
    }
    
    // 官方連結
    if (!string.IsNullOrEmpty(modInfo.Url))
    {
        Console.WriteLine($"\n官方網址: {modInfo.Url}");
    }
    
    // 依賴分析
    if (modInfo.ModDependencies.Any())
    {
        Console.WriteLine($"\n模組依賴 ({modInfo.ModDependencies.Count} 個):");
        foreach (var dep in modInfo.ModDependencies)
        {
            Console.WriteLine($"  - {dep.DisplayName} ({dep.PackageId})");
            if (!string.IsNullOrEmpty(dep.SteamWorkshopUrl))
            {
                Console.WriteLine($"    Steam: {dep.SteamWorkshopUrl}");
            }
            if (!string.IsNullOrEmpty(dep.DownloadUrl))
            {
                Console.WriteLine($"    下載: {dep.DownloadUrl}");
            }
        }
    }
    
    // 版本特定依賴
    if (modInfo.ModDependenciesByVersion.Any())
    {
        Console.WriteLine($"\n版本特定依賴:");
        var versionGroups = modInfo.ModDependenciesByVersion.GroupBy(d => d.TargetVersion);
        foreach (var group in versionGroups)
        {
            Console.WriteLine($"  版本 {group.Key}: {group.Count()} 個依賴");
            foreach (var dep in group.Take(3))
            {
                Console.WriteLine($"    - {dep.DisplayName}");
            }
        }
    }
    
    // 載入順序
    if (modInfo.LoadAfter.Any())
    {
        Console.WriteLine($"\n需要在以下模組之後載入:");
        foreach (var loadAfter in modInfo.LoadAfter)
        {
            Console.WriteLine($"  - {loadAfter}");
        }
    }
    
    // 不相容模組
    if (modInfo.IncompatibleWith.Any())
    {
        Console.WriteLine($"\n不相容模組 ({modInfo.IncompatibleWith.Count} 個):");
        foreach (var incompatible in modInfo.IncompatibleWith)
        {
            Console.WriteLine($"  - {incompatible}");
        }
    }
    
    // 翻譯狀態
    Console.WriteLine($"\n翻譯狀態:");
    Console.WriteLine($"  有翻譯模組: {modInfo.HasTranslationMod}");
    Console.WriteLine($"  翻譯補丁語言: {modInfo.TranslationPatchLanguages}");
    Console.WriteLine($"  支援語言: {modInfo.SupportedLanguages}");
}
```

## 🎯 最佳實踐

### 1. 服務生命週期管理

```csharp
public class ModScannerManager : IDisposable
{
    private readonly IModScannerService _scannerService;
    private readonly ITranslationMappingService _translationMappingService;
    private readonly ILoggerService _loggerService;
    
    public ModScannerManager()
    {
        _loggerService = new LoggerService();
        var pathService = new PathService();
        var xmlParserService = new XmlParserService(_loggerService);
        var modInfoService = new ModInfoService(xmlParserService, pathService, _loggerService);
        _scannerService = new ModScannerService(modInfoService, pathService, _loggerService);
        _translationMappingService = new TranslationMappingService(pathService, _loggerService);
    }
    
    public async Task<List<ModInfo>> ScanMods(string gamePath)
    {
        return await _scannerService.ScanModsAsync(gamePath);
    }
    
    public async Task<Dictionary<string, List<ModInfo>>> BuildTranslationMappings(List<ModInfo> mods)
    {
        return await _translationMappingService.BuildTranslationMappingsAsync(mods);
    }
    
    public void Dispose()
    {
        _loggerService?.Dispose();
    }
}
```

### 2. 非同步模式

```csharp
// ✅ 推薦：使用 async/await
public async Task<List<ModInfo>> ScanModsAsync(string gamePath)
{
    var services = InitializeServices();
    return await services.modScannerService.ScanModsAsync(gamePath);
}

// ❌ 避免：同步阻塞
public List<ModInfo> ScanModsBlocking(string gamePath)
{
    var services = InitializeServices();
    return services.modScannerService.ScanModsAsync(gamePath).Result; // 可能導致死鎖
}
```

### 3. 記憶體管理

```csharp
public void ProcessLargeModList(List<ModInfo> mods)
{
    foreach (var mod in mods)
    {
        // 處理模組資訊
        ProcessModInfo(mod);
        
        // 釋放預覽圖片資源
        mod.PreviewImage?.Dispose();
        mod.PreviewImage = null;
    }
}
```

## 🔧 擴展性

### 1. 自定義模組資訊提取器

```csharp
public interface IModInfoExtractor
{
    ModInfo ExtractFromDirectory(string modPath);
    bool CanExtract(string modPath);
}

public class CustomModInfoExtractor : IModInfoExtractor
{
    public bool CanExtract(string modPath)
    {
        // 自定義檢測邏輯
        return Directory.Exists(Path.Combine(modPath, "CustomFolder"));
    }
    
    public ModInfo ExtractFromDirectory(string modPath)
    {
        // 自定義提取邏輯
        var modInfo = new ModInfo();
        // ... 提取邏輯
        return modInfo;
    }
}
```

### 2. 插件化掃描來源

```csharp
public interface IModSource
{
    string Name { get; }
    List<string> GetModDirectories(string basePath);
    bool IsAvailable(string basePath);
}

public class SteamWorkshopSource : IModSource
{
    public string Name => "Steam Workshop";
    
    public List<string> GetModDirectories(string basePath)
    {
        var workshopPath = GetWorkshopPath(basePath);
        return Directory.Exists(workshopPath) 
            ? Directory.GetDirectories(workshopPath).ToList()
            : new List<string>();
    }
    
    public bool IsAvailable(string basePath)
    {
        return Directory.Exists(GetWorkshopPath(basePath));
    }
}
```

## 📈 效能優化建議

1. **批次處理**：避免頻繁的小批次掃描
2. **快取機制**：對已掃描的模組進行快取
3. **並行處理**：在安全的情況下使用並行掃描
4. **資源釋放**：及時釋放圖片等大型資源
5. **進度節流**：避免過於頻繁的進度更新

## 🐛 常見問題

### Q: 如何處理損壞的 About.xml 檔案？
A: 服務會自動捕獲異常並返回 null，建議檢查日誌獲取詳細錯誤資訊。

### Q: 掃描速度很慢怎麼辦？
A: 考慮使用 ScanLocalModsAsync 只掃描本地模組，或者實現快取機制。

### Q: 如何自定義語言檢測？
A: 目前基於 Languages/ 目錄檢測，可以擴展 DetectSupportedLanguages 方法。

### Q: 翻譯映射是如何建立的？
A: 基於官方標準，從翻譯模組的 DefInjected XML 檔案中提取 `<DefName.field>` 標籤來精確識別目標模組。

### Q: 模組來源是如何判斷的？
A: 根據掃描目錄自動設置：
- `Mods/` 目錄 → Local
- `Workshop/` 目錄 → Steam  
- `Data/` 目錄 → Official

### Q: 如何獲取某個模組的所有翻譯補丁？
A: 使用 `GetTranslationPatchesForMod` 方法，或檢查 `TranslationPatchPackageIds` 屬性。

### Q: 翻譯補丁語言是如何檢測的？
A: 自動從翻譯補丁的 `SupportedLanguages` 屬性提取，並合併所有補丁的語言列表。

### Q: 如何判斷一個模組是否為翻譯模組？
A: 使用 `IsTranslationMod` 方法，基於目錄結構檢測（Languages/DefInjected/ 或 Languages/Keyed/ 目錄存在）。

### Q: 如何獲取模組的詳細描述？
A: 使用 `Description` 屬性，它會自動解析 About.xml 中的 `<description>` 標籤內容。

### Q: 如何分析模組的依賴關係？
A: 檢查 `ModDependencies` 和 `ModDependenciesByVersion` 屬性，它們包含完整的依賴信息和連結。

### Q: 如何處理模組的載入順序？
A: 檢查 `LoadAfter` 屬性，它列出了需要在哪些模組之後載入的模組列表。

### Q: 如何識別不相容的模組？
A: 檢查 `IncompatibleWith` 屬性，它列出了所有不相容的模組 PackageId。

### Q: 如何獲取模組的官方連結？
A: 使用 `Url` 屬性獲取官方網址，或從 `ModDependencies` 中獲取 Steam Workshop 和下載連結。

### Q: 如何處理版本特定的依賴？
A: `ModDependenciesByVersion` 屬性包含 `TargetVersion` 字段，可以區分不同版本的依賴需求。

### Q: 描述內容太長怎麼辦？
A: Description 屬性包含完整內容，可以根據需要截斷或分頁顯示。建議顯示前 200 字符作為預覽。

## 📞 支援

如有問題或建議，請查看：
- 程式碼註釋
- 日誌輸出
- 單元測試範例

---

**版本**: 2.0.0  
**最後更新**: 2025年1月  
**適用範圍**: RimWorld 模組掃描引擎 (包含翻譯映射功能)

## 🆕 v2.0.0 新功能

### ✨ 翻譯映射系統
- **精確目標檢測**: 基於官方標準的 `<DefName.field>` 標籤解析
- **雙向關聯**: 翻譯模組與目標模組互相關聯
- **語言統計**: 自動統計翻譯補丁支持的語言
- **來源識別**: 自動識別模組來源 (Local/Steam/Official)

### 🔧 ModInfo 模型增強
- **模組來源**: `Source` 屬性標識模組來源
- **翻譯狀態**: `HasTranslationMod` 和 `TranslationPatchLanguages` 屬性
- **關聯信息**: `TargetModPackageIds` 和 `TranslationPatchPackageIds` 列表
- **向後相容**: 保留所有舊有屬性

### 📋 完整 About.xml 支援
- **詳細描述**: `Description` 屬性解析完整的模組描述
- **模組版本**: `ModVersion` 屬性獲取模組版本號
- **官方連結**: `Url` 屬性獲取官方網址
- **依賴關係**: `ModDependencies` 和 `ModDependenciesByVersion` 完整解析
- **載入順序**: `LoadAfter` 屬性獲取載入順序要求
- **不相容模組**: `IncompatibleWith` 屬性識別不相容模組
- **ModDependency 模型**: 結構化的依賴信息管理

### 📊 分析功能
- **翻譯覆蓋率**: 統計模組的翻譯覆蓋情況
- **語言分佈**: 分析各語言的翻譯補丁數量
- **熱門目標**: 找出最受歡迎的翻譯目標模組
- **依賴分析**: 完整的模組依賴關係分析
- **相容性檢查**: 自動識別潛在的衝突模組
