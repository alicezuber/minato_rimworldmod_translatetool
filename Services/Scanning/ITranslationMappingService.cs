using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorldTranslationTool.Models;

namespace RimWorldTranslationTool.Services.Scanning
{
    /// <summary>
    /// 翻譯映射服務介面 - 處理翻譯模組與目標模組的關聯
    /// </summary>
    public interface ITranslationMappingService
    {
        /// <summary>
        /// 建立翻譯模組與目標模組的映射關係
        /// </summary>
        /// <param name="allMods">所有模組列表</param>
        /// <returns>映射字典：Key 為目標模組 PackageId，Value 為翻譯模組列表</returns>
        Task<Dictionary<string, List<ModModel>>> BuildTranslationMappingsAsync(IEnumerable<ModModel> allMods);
        
        /// <summary>
        /// 檢查模組是否為翻譯模組
        /// </summary>
        /// <param name="mod">要檢查的模組</param>
        /// <returns>是否為翻譯模組</returns>
        bool IsTranslationMod(ModModel mod);
        
        /// <summary>
        /// 獲取翻譯模組的目標模組列表
        /// </summary>
        /// <param name="translationMod">翻譯模組</param>
        /// <param name="allMods">所有模組列表</param>
        /// <returns>目標模組列表</returns>
        Task<List<ModModel>> GetTargetModsForTranslationAsync(ModModel translationMod, IEnumerable<ModModel> allMods);
        
        /// <summary>
        /// 獲取模組的翻譯補丁列表
        /// </summary>
        /// <param name="targetMod">目標模組</param>
        /// <param name="mappings">映射字典</param>
        /// <returns>翻譯補丁列表</returns>
        List<ModModel> GetTranslationPatchesForMod(ModModel targetMod, Dictionary<string, List<ModModel>> mappings);
    }
}
