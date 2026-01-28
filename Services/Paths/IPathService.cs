using System;
using System.Collections.Generic;

namespace RimWorldTranslationTool.Services.Paths
{
    /// <summary>
    /// 路徑服務介面 - 統一管理 RimWorld 相關路徑計算
    /// </summary>
    public interface IPathService
    {
        /// <summary>
        /// 根據遊戲本體路徑推導工作坊路徑
        /// </summary>
        /// <param name="gamePath">遊戲本體路徑</param>
        /// <returns>工作坊路徑，如果無法推導則返回空字串</returns>
        string GetWorkshopPath(string gamePath);
        
        /// <summary>
        /// 獲取 RimWorld 設定資料夾路徑
        /// </summary>
        /// <returns>設定資料夾路徑</returns>
        string GetConfigPath();
        
        /// <summary>
        /// 獲取 ModsConfig.xml 完整路徑
        /// </summary>
        /// <returns>ModsConfig.xml 路徑</returns>
        string GetModsConfigPath();
        
        /// <summary>
        /// 獲取存檔資料夾路徑
        /// </summary>
        /// <returns>存檔資料夾路徑</returns>
        string GetSavesPath();
        
        /// <summary>
        /// 根據遊戲本體路徑獲取本地模組資料夾路徑
        /// </summary>
        /// <param name="gamePath">遊戲本體路徑</param>
        /// <returns>本地模組資料夾路徑</returns>
        string GetLocalModsPath(string gamePath);
        
        /// <summary>
        /// 根據遊戲本體路徑獲取 Data 資料夾路徑
        /// </summary>
        /// <param name="gamePath">遊戲本體路徑</param>
        /// <returns>Data 資料夾路徑</returns>
        string GetDataPath(string gamePath);
        
        /// <summary>
        /// 驗證是否為有效的 RimWorld 遊戲路徑
        /// </summary>
        /// <param name="path">要驗證的路徑</param>
        /// <returns>驗證結果</returns>
        PathValidationResult IsValidGamePath(string path);
        
        /// <summary>
        /// 獲取所有可能的 RimWorld 安裝路徑
        /// </summary>
        /// <returns>可能的路徑列表</returns>
        List<string> GetPossibleGamePaths();
        
        /// <summary>
        /// 根據模組資料夾路徑獲取模組的 About.xml 路徑
        /// </summary>
        /// <param name="modFolderPath">模組資料夾路徑</param>
        /// <returns>About.xml 路徑</returns>
        string GetModAboutXmlPath(string modFolderPath);
        
        /// <summary>
        /// 根據模組資料夾路徑獲取模組的 Languages 資料夾路徑
        /// </summary>
        /// <param name="modFolderPath">模組資料夾路徑</param>
        /// <returns>Languages 資料夾路徑</returns>
        string GetModLanguagesPath(string modFolderPath);
        
        /// <summary>
        /// 檢查路徑是否存在且可存取
        /// </summary>
        /// <param name="path">要檢查的路徑</param>
        /// <returns>是否存在且可存取</returns>
        bool PathExists(string path);
        
        /// <summary>
        /// 確保目錄存在，如果不存在則建立
        /// </summary>
        /// <param name="directoryPath">目錄路徑</param>
        /// <returns>是否成功建立或已存在</returns>
        bool EnsureDirectoryExists(string directoryPath);
        
        /// <summary>
        /// 獲取應用程式資料路徑
        /// </summary>
        /// <returns>應用程式資料路徑</returns>
        string GetAppDataPath();

        /// <summary>
        /// 獲取備份目錄路徑
        /// </summary>
        /// <returns>備份目錄路徑</returns>
        string GetBackupDirectory();

        /// <summary>
        /// 獲取設定檔案路徑
        /// </summary>
        /// <returns>設定檔案路徑</returns>
        string GetSettingsFilePath();
        
        /// <summary>
        /// 獲取日誌目錄路徑
        /// </summary>
        /// <returns>日誌目錄路徑</returns>
        string GetLogsDirectory();
    }
    
    /// <summary>
    /// 路徑驗證結果
    /// </summary>
    public class PathValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";
        public PathValidationStatus Status { get; set; } = PathValidationStatus.None;
        public string ExecutablePath { get; set; } = "";
        public string DataPath { get; set; } = "";
    }
    
    /// <summary>
    /// 路徑驗證狀態
    /// </summary>
    public enum PathValidationStatus
    {
        None,
        Valid,
        Warning,
        Error
    }
}
