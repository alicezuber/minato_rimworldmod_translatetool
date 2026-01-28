namespace RimWorldTranslationTool.Models
{
    /// <summary>
    /// 模組來源枚舉
    /// </summary>
    public enum ModSource
    {
        Unknown,
        Local,      // 本地模組
        Steam,      // Steam Workshop
        Official    // 官方核心模組
    }

    /// <summary>
    /// 模組依賴信息
    /// </summary>
    public class ModDependency
    {
        public string PackageId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string SteamWorkshopUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string TargetVersion { get; set; } = "";  // 用於 modDependenciesByVersion
    }
}
