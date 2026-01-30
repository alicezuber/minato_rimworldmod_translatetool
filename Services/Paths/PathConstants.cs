using System.IO;

namespace RimWorldTranslationTool.Services.Paths
{
    /// <summary>
    /// RimWorld 路徑相關常數定義
    /// </summary>
    public static class PathConstants
    {
        /// <summary>
        /// Steam 工作坊相對路徑
        /// </summary>
        public const string WorkshopRelativePath = @"steamapps\workshop\content\294100";
        
        /// <summary>
        /// RimWorld 設定資料夾名稱
        /// </summary>
        public const string ConfigFolderName = @"Ludeon Studios\RimWorld by Ludeon Studios";
        
        /// <summary>
        /// ModsConfig.xml 檔案名稱
        /// </summary>
        public const string ModsConfigFileName = "ModsConfig.xml";
        
        /// <summary>
        /// Config 資料夾名稱
        /// </summary>
        public const string ConfigFolder = "Config";
        
        /// <summary>
        /// Saves 資料夾名稱
        /// </summary>
        public const string SavesFolder = "Saves";
        
        /// <summary>
        /// 本地模組資料夾名稱
        /// </summary>
        public const string LocalModsFolder = "Mods";
        
        /// <summary>
        /// Data 資料夾名稱
        /// </summary>
        public const string DataFolder = "Data";
        
        /// <summary>
        /// RimWorld 主程式檔案名稱
        /// </summary>
        public const string GameExecutableName = "RimWorldWin64.exe";
        
        /// <summary>
        /// RimWorld Linux 主程式檔案名稱
        /// </summary>
        public const string GameExecutableNameLinux = "RimWorldLinux";
        
        /// <summary>
        /// RimWorld macOS 主程式檔案名稱
        /// </summary>
        public const string GameExecutableNameMac = "RimWorldMac.app";
        
        /// <summary>
        /// 模組 About.xml 檔案路徑
        /// </summary>
        public const string ModAboutXml = @"About\About.xml";
        
        /// <summary>
        /// 模組 Languages 資料夾
        /// </summary>
        public const string ModLanguagesFolder = "Languages";
        
        /// <summary>
        /// 模組 Defs 資料夾
        /// </summary>
        public const string ModDefsFolder = "Defs";
        
        /// <summary>
        /// 官方擴展資料夾列表
        /// </summary>
        public static readonly string[] OfficialExpansions = 
        {
            "Core",
            "Royalty", 
            "Ideology",
            "Biotech",
            "Anomaly",
            "Odyssey"
        };
    }
}
