using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool.Services.Scanning
{
    /// <summary>
    /// 翻譯映射服務實現 - 基於官方標準的精確翻譯模組識別
    /// </summary>
    public class TranslationMappingService : ITranslationMappingService
    {
        private readonly IPathService _pathService;
        private readonly ILoggerService _logger;
        private readonly Dictionary<string, string> _modPaths; // PackageId -> Path 映射

        public TranslationMappingService(IPathService pathService, ILoggerService logger)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modPaths = new Dictionary<string, string>();
        }

        /// <summary>
        /// 設置模組路徑映射（需要在掃描後調用）
        /// </summary>
        public void SetModPaths(IEnumerable<ModInfo> mods)
        {
            _modPaths.Clear();
            foreach (var mod in mods)
            {
                // 這裡需要根據實際情況設置路徑
                // 暫時使用簡單的邏輯，實際使用時需要從掃描結果中獲取完整路徑
                _modPaths[mod.PackageId] = mod.FolderName;
            }
        }

        public async Task<Dictionary<string, List<ModInfo>>> BuildTranslationMappingsAsync(IEnumerable<ModInfo> allMods)
        {
            var mappings = new Dictionary<string, List<ModInfo>>();
            var modList = allMods.ToList();

            // 設置模組路徑映射
            SetModPaths(modList);

            _logger.LogInfoAsync("開始建立翻譯映射關係").Wait();

            // 1. 識別翻譯模組
            var translationMods = modList.Where(m => IsTranslationMod(m)).ToList();
            _logger.LogInfoAsync($"找到 {translationMods.Count} 個翻譯模組").Wait();

            // 2. 為每個翻譯模組建立目標映射
            foreach (var transMod in translationMods)
            {
                try
                {
                    var targetMods = await GetTargetModsForTranslationAsync(transMod, modList);
                    
                    foreach (var targetMod in targetMods)
                    {
                        if (!mappings.ContainsKey(targetMod.PackageId))
                        {
                            mappings[targetMod.PackageId] = new List<ModInfo>();
                        }
                        
                        if (!mappings[targetMod.PackageId].Any(m => m.PackageId == transMod.PackageId))
                        {
                            mappings[targetMod.PackageId].Add(transMod);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogErrorAsync($"處理翻譯模組失敗: {transMod.Name}", ex).Wait();
                }
            }

            // 3. 更新每個模組的關聯信息
            foreach (var mod in modList)
            {
                // 設置翻譯補丁信息
                if (mappings.ContainsKey(mod.PackageId))
                {
                    var patches = mappings[mod.PackageId];
                    mod.HasTranslationMod = true;
                    mod.TranslationPatchPackageIds = patches.Select(p => p.PackageId).ToList();
                    
                    // 設置翻譯補丁支持的語言
                    var languages = patches
                        .SelectMany(p => p.SupportedLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        .Select(lang => lang.Trim())
                        .Where(lang => lang != "unknown" && !string.IsNullOrEmpty(lang))
                        .Distinct()
                        .ToList();
                    
                    mod.TranslationPatchLanguages = languages.Any() ? string.Join(", ", languages) : "none";
                }
                else
                {
                    mod.HasTranslationMod = false;
                    mod.TranslationPatchLanguages = "none";
                }
                
                // 設置目標模組信息（如果是翻譯模組）
                if (IsTranslationMod(mod))
                {
                    var targets = await GetTargetModsForTranslationAsync(mod, modList);
                    mod.TargetModPackageIds = targets.Select(t => t.PackageId).ToList();
                }
            }

            _logger.LogInfoAsync($"翻譯映射建立完成，共 {mappings.Count} 個目標模組有翻譯").Wait();
            return mappings;
        }

        public bool IsTranslationMod(ModInfo mod)
        {
            try
            {
                // 基於目錄結構檢測，不依賴名稱猜測
                var languagesPath = _pathService.GetModLanguagesPath(GetModPath(mod));
                
                if (!Directory.Exists(languagesPath))
                {
                    return false;
                }

                var languageDirs = Directory.GetDirectories(languagesPath);
                
                foreach (var langDir in languageDirs)
                {
                    var defInjectedPath = Path.Combine(langDir, "DefInjected");
                    var keyedPath = Path.Combine(langDir, "Keyed");
                    
                    if (Directory.Exists(defInjectedPath) || Directory.Exists(keyedPath))
                    {
                        return true; // 有翻譯檔案就是翻譯模組
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync($"檢測翻譯模組失敗: {mod.Name}", ex).Wait();
                return false;
            }
        }

        public async Task<List<ModInfo>> GetTargetModsForTranslationAsync(ModInfo translationMod, IEnumerable<ModInfo> allMods)
        {
            var targetMods = new List<ModInfo>();
            var modList = allMods.ToList();

            try
            {
                var modPath = GetModPath(translationMod);
                var targetDefNames = await ExtractTargetDefNamesAsync(modPath);
                
                _logger.LogInfoAsync($"翻譯模組 {translationMod.Name} 的目標 Def: {string.Join(", ", targetDefNames)}").Wait();

                foreach (var defName in targetDefNames)
                {
                    // 使用多種匹配策略
                    var targetMod = FindTargetMod(defName, modList);
                    
                    if (targetMod != null && targetMod != translationMod && !targetMods.Contains(targetMod))
                    {
                        targetMods.Add(targetMod);
                        _logger.LogInfoAsync($"找到目標模組: {targetMod.Name} (PackageId: {targetMod.PackageId})").Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync($"獲取翻譯目標失敗: {translationMod.Name}", ex).Wait();
            }

            return targetMods;
        }

        public List<ModInfo> GetTranslationPatchesForMod(ModInfo targetMod, Dictionary<string, List<ModInfo>> mappings)
        {
            if (mappings.ContainsKey(targetMod.PackageId))
            {
                return mappings[targetMod.PackageId];
            }
            
            return new List<ModInfo>();
        }

        private async Task<HashSet<string>> ExtractTargetDefNamesAsync(string translationModPath)
        {
            var targetDefs = new HashSet<string>();
            var languagesPath = _pathService.GetModLanguagesPath(translationModPath);

            if (!Directory.Exists(languagesPath))
            {
                return targetDefs;
            }

            foreach (var langDir in Directory.GetDirectories(languagesPath))
            {
                var defInjectedPath = Path.Combine(langDir, "DefInjected");
                if (!Directory.Exists(defInjectedPath)) continue;

                var xmlFiles = Directory.GetFiles(defInjectedPath, "*.xml", SearchOption.AllDirectories);
                
                foreach (var file in xmlFiles.Take(50)) // 限制檢查數量以提高效能
                {
                    try
                    {
                        var xml = XDocument.Load(file);
                        var defNames = ExtractDefNamesFromXml(xml);
                        
                        foreach (var defName in defNames)
                        {
                            targetDefs.Add(defName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogErrorAsync($"解析翻譯檔案失敗: {file}", ex).Wait();
                    }
                }
            }

            return targetDefs;
        }

        private HashSet<string> ExtractDefNamesFromXml(XDocument xml)
        {
            var defNames = new HashSet<string>();
            
            if (xml.Root == null) return defNames;

            // 精確提取：從 <DefName.field> 標籤中提取 DefName
            foreach (var element in xml.Root.Elements())
            {
                var fullName = element.Name.LocalName;
                var parts = fullName.Split('.');
                
                if (parts.Length >= 2)
                {
                    var defName = parts[0];
                    if (!string.IsNullOrEmpty(defName))
                    {
                        defNames.Add(defName);
                    }
                }
            }

            return defNames;
        }

        private ModInfo FindTargetMod(string defName, List<ModInfo> allMods)
        {
            // 按優先級匹配
            var candidates = allMods.Where(m => 
                m.PackageId.Equals(defName, StringComparison.OrdinalIgnoreCase) ||
                m.Name.Equals(defName, StringComparison.OrdinalIgnoreCase) ||
                m.FolderName.Equals(defName, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            // 優先返回 PackageId 精確匹配
            var exactMatch = candidates.FirstOrDefault(m => m.PackageId.Equals(defName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return exactMatch;

            // 其次返回名稱匹配
            var nameMatch = candidates.FirstOrDefault(m => m.Name.Equals(defName, StringComparison.OrdinalIgnoreCase));
            if (nameMatch != null) return nameMatch;

            // 最後返回資料夾名稱匹配
            return candidates.FirstOrDefault();
        }

        private string GetModPath(ModInfo mod)
        {
            // 從映射中獲取模組路徑
            if (_modPaths.TryGetValue(mod.PackageId, out string path))
            {
                return path;
            }
            
            // 備用方案：使用 FolderName
            return mod.FolderName;
        }
    }
}
