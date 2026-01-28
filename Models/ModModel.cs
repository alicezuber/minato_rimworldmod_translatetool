using System;
using System.Collections.Generic;

namespace RimWorldTranslationTool.Models
{
    /// <summary>
    /// 模組純數據模型
    /// </summary>
    public class ModModel
    {
        public string FolderName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string PackageId { get; set; } = "";
        public string SupportedVersions { get; set; } = "";
        public string SupportedLanguages { get; set; } = "unknown";
        public bool IsVersionCompatible { get; set; } = true;
        
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
        public string ModVersion { get; set; } = "";
        public List<ModDependency> ModDependencies { get; set; } = new();
        public List<ModDependency> ModDependenciesByVersion { get; set; } = new();
        public List<string> LoadAfter { get; set; } = new();
        public List<string> IncompatibleWith { get; set; } = new();
        
        public ModSource Source { get; set; } = ModSource.Unknown;
        
        public bool HasTranslationMod { get; set; } = false;
        public string TranslationPatchLanguages { get; set; } = "none";
        
        public List<string> TargetModPackageIds { get; set; } = new();
        public List<string> TranslationPatchPackageIds { get; set; } = new();
        
        // 狀態標記（純數據）
        public string HasChineseTraditional { get; set; } = "無";
        public string HasChineseSimplified { get; set; } = "無";
        public string HasTranslationPatch { get; set; } = "無";
        public string CanTranslate { get; set; } = "否";
        public bool IsEnabled { get; set; } = false;
        public bool IsTranslationPatch { get; set; } = false;
        
        // 圖片路徑（ViewModel 會根據此路徑載入 BitmapImage）
        public string? PreviewImagePath { get; set; }
    }
}
