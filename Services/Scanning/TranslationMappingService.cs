using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using RimWorldTranslationTool.Models;
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

        public void SetModPaths(IEnumerable<ModModel> mods)
        {
            _modPaths.Clear();
            foreach (var mod in mods)
            {
                _modPaths[mod.PackageId] = mod.FolderName;
            }
        }

        public async Task<Dictionary<string, List<ModModel>>> BuildTranslationMappingsAsync(IEnumerable<ModModel> allMods)
        {
            var mappings = new Dictionary<string, List<ModModel>>();
            var modList = allMods.ToList();

            SetModPaths(modList);

            await _logger.LogInfoAsync("開始建立翻譯映射關係");

            // 1. 識別翻譯模組
            var translationMods = modList.Where(m => IsTranslationMod(m)).ToList();
            await _logger.LogInfoAsync($"找到 {translationMods.Count} 個翻譯模組");

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
                            mappings[targetMod.PackageId] = new List<ModModel>();
                        }
                        
                        if (!mappings[targetMod.PackageId].Any(m => m.PackageId == transMod.PackageId))
                        {
                            mappings[targetMod.PackageId].Add(transMod);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"處理翻譯模組失敗: {transMod.Name}", ex);
                }
            }

            // 3. 更新每個模組的關聯信息
            foreach (var mod in modList)
            {
                if (mappings.ContainsKey(mod.PackageId))
                {
                    var patches = mappings[mod.PackageId];
                    mod.HasTranslationMod = true;
                    mod.TranslationPatchPackageIds = patches.Select(p => p.PackageId).ToList();
                    
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
                
                if (IsTranslationMod(mod))
                {
                    var targets = await GetTargetModsForTranslationAsync(mod, modList);
                    mod.TargetModPackageIds = targets.Select(t => t.PackageId).ToList();
                }
            }

            await _logger.LogInfoAsync($"翻譯映射建立完成，共 {mappings.Count} 個目標模組有翻譯");
            return mappings;
        }

        public bool IsTranslationMod(ModModel mod)
        {
            try
            {
                var languagesPath = _pathService.GetModLanguagesPath(GetModPath(mod));
                if (!Directory.Exists(languagesPath)) return false;

                var languageDirs = Directory.GetDirectories(languagesPath);
                foreach (var langDir in languageDirs)
                {
                    var defInjectedPath = Path.Combine(langDir, "DefInjected");
                    var keyedPath = Path.Combine(langDir, "Keyed");
                    if (Directory.Exists(defInjectedPath) || Directory.Exists(keyedPath)) return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ModModel>> GetTargetModsForTranslationAsync(ModModel translationMod, IEnumerable<ModModel> allMods)
        {
            var targetMods = new List<ModModel>();
            var modList = allMods.ToList();

            try
            {
                var modPath = GetModPath(translationMod);
                var targetDefNames = await ExtractTargetDefNamesAsync(modPath);
                
                foreach (var defName in targetDefNames)
                {
                    var targetMod = FindTargetMod(defName, modList);
                    if (targetMod != null && targetMod != translationMod && !targetMods.Contains(targetMod))
                    {
                        targetMods.Add(targetMod);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"獲取翻譯目標失敗: {translationMod.Name}", ex);
            }

            return targetMods;
        }

        public List<ModModel> GetTranslationPatchesForMod(ModModel targetMod, Dictionary<string, List<ModModel>> mappings)
        {
            if (mappings.ContainsKey(targetMod.PackageId))
            {
                return mappings[targetMod.PackageId];
            }
            return new List<ModModel>();
        }

        private async Task<HashSet<string>> ExtractTargetDefNamesAsync(string translationModPath)
        {
            var targetDefs = new HashSet<string>();
            var languagesPath = _pathService.GetModLanguagesPath(translationModPath);

            if (!Directory.Exists(languagesPath)) return targetDefs;

            foreach (var langDir in Directory.GetDirectories(languagesPath))
            {
                var defInjectedPath = Path.Combine(langDir, "DefInjected");
                if (!Directory.Exists(defInjectedPath)) continue;

                var xmlFiles = Directory.GetFiles(defInjectedPath, "*.xml", SearchOption.AllDirectories);
                foreach (var file in xmlFiles.Take(50))
                {
                    try
                    {
                        var xml = XDocument.Load(file);
                        var defNames = ExtractDefNamesFromXml(xml);
                        foreach (var defName in defNames) targetDefs.Add(defName);
                    }
                    catch { }
                }
            }

            return await Task.FromResult(targetDefs);
        }

        private HashSet<string> ExtractDefNamesFromXml(XDocument xml)
        {
            var defNames = new HashSet<string>();
            if (xml.Root == null) return defNames;

            foreach (var element in xml.Root.Elements())
            {
                var fullName = element.Name.LocalName;
                var parts = fullName.Split('.');
                if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                {
                    defNames.Add(parts[0]);
                }
            }
            return defNames;
        }

        private ModModel? FindTargetMod(string defName, List<ModModel> allMods)
        {
            var candidates = allMods.Where(m => 
                m.PackageId.Equals(defName, StringComparison.OrdinalIgnoreCase) ||
                m.Name.Equals(defName, StringComparison.OrdinalIgnoreCase) ||
                m.FolderName.Equals(defName, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            var exactMatch = candidates.FirstOrDefault(m => m.PackageId.Equals(defName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return exactMatch;

            var nameMatch = candidates.FirstOrDefault(m => m.Name.Equals(defName, StringComparison.OrdinalIgnoreCase));
            if (nameMatch != null) return nameMatch;

            return candidates.FirstOrDefault();
        }

        private string GetModPath(ModModel mod)
        {
            if (_modPaths.TryGetValue(mod.PackageId, out string? path)) return path;
            return mod.FolderName;
        }
    }
}
