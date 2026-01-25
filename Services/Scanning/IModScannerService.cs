using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.Scanning
{
    /// <summary>
    /// 模組掃描服務介面 - 專注於核心掃描功能
    /// </summary>
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

    /// <summary>
    /// 模組資訊服務介面
    /// </summary>
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

    /// <summary>
    /// 掃描進度資訊
    /// </summary>
    public class ScanProgress
    {
        public int Processed { get; set; }
        public int Total { get; set; }
        public string CurrentMod { get; set; } = "";
        public string Status { get; set; } = "";
        
        public double PercentComplete => Total > 0 ? (double)Processed / Total * 100 : 0;
    }
}
