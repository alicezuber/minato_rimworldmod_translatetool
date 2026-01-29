using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorldTranslationTool.Models;
using RimWorldTranslationTool.Services.Logging;

namespace RimWorldTranslationTool.Services.Paths
{
    /// <summary>
    /// 路徑服務實現 - 統一管理 RimWorld 相關路徑計算
    /// </summary>
    public class PathService : IPathService
    {
        private readonly ILoggerService _loggerService;
        
        public PathService(ILoggerService loggerService)
        {
            _loggerService = loggerService;
        }
        /// <summary>
        /// 根據遊戲本體路徑推導工作坊路徑
        /// </summary>
        public string GetWorkshopPath(string gamePath)
        {
            if (string.IsNullOrEmpty(gamePath))
                return "";
                
            try
            {
                // 從遊戲路徑往上兩層到 steamapps，然後到 workshop/content/294100
                var gameDir = new DirectoryInfo(gamePath);
                var steamAppsDir = gameDir.Parent?.Parent;
                
                if (steamAppsDir?.Name == "steamapps")
                {
                    return Path.Combine(steamAppsDir.FullName, PathConstants.WorkshopRelativePath);
                }
                
                // 嘗試其他可能的 Steam 安裝結構
                var parentDir = gameDir.Parent;
                if (parentDir?.Name == "common")
                {
                    var steamAppsDir2 = parentDir.Parent;
                    if (steamAppsDir2?.Name == "steamapps")
                    {
                        return Path.Combine(steamAppsDir2.FullName, PathConstants.WorkshopRelativePath);
                    }
                }
                
                return "";
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync($"推導工作坊路徑失敗: {gamePath}", ex);
                return "";
            }
        }

        public bool TryGetWorkshopPath(string gamePath, out string path)
        {
            path = "";
            if (string.IsNullOrEmpty(gamePath)) return false;
            var result = GetWorkshopPath(gamePath);
            if (!string.IsNullOrEmpty(result))
            {
                path = result;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 獲取 RimWorld 設定資料夾路徑
        /// </summary>
        public string GetConfigPath()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configPath = Path.Combine(localAppData, "..", "LocalLow", PathConstants.ConfigFolderName);
                
                // 標準化路徑
                return Path.GetFullPath(configPath);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("獲取設定路徑失敗", ex);
                return "";
            }
        }

        public bool TryGetConfigPath(out string path)
        {
            path = "";
            var result = GetConfigPath();
            if (!string.IsNullOrEmpty(result))
            {
                path = result;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 獲取 ModsConfig.xml 完整路徑
        /// </summary>
        public string GetModsConfigPath()
        {
            try
            {
                var configPath = GetConfigPath();
                if (string.IsNullOrEmpty(configPath))
                    return "";
                    
                return Path.Combine(configPath, PathConstants.ConfigFolder, PathConstants.ModsConfigFileName);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("獲取 ModsConfig.xml 路徑失敗", ex);
                return "";
            }
        }

        public bool TryGetModsConfigPath(out string path)
        {
            path = "";
            var result = GetModsConfigPath();
            if (!string.IsNullOrEmpty(result))
            {
                path = result;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 獲取存檔資料夾路徑
        /// </summary>
        public string GetSavesPath()
        {
            try
            {
                var configPath = GetConfigPath();
                if (string.IsNullOrEmpty(configPath))
                    return "";
                    
                return Path.Combine(configPath, PathConstants.SavesFolder);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("獲取存檔路徑失敗", ex);
                return "";
            }
        }

        public bool TryGetSavesPath(out string path)
        {
            path = "";
            var result = GetSavesPath();
            if (!string.IsNullOrEmpty(result))
            {
                path = result;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 根據遊戲本體路徑獲取本地模組資料夾路徑
        /// </summary>
        public string GetLocalModsPath(string gamePath)
        {
            if (string.IsNullOrEmpty(gamePath))
                return "";
                
            try
            {
                return Path.Combine(gamePath, PathConstants.LocalModsFolder);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync($"獲取本地模組路徑失敗: {gamePath}", ex);
                return "";
            }
        }

        public bool TryGetLocalModsPath(string gamePath, out string path)
        {
            path = "";
            if (string.IsNullOrEmpty(gamePath)) return false;
            var result = GetLocalModsPath(gamePath);
            if (!string.IsNullOrEmpty(result))
            {
                path = result;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 根據遊戲本體路徑獲取 Data 資料夾路徑
        /// </summary>
        public string GetDataPath(string gamePath)
        {
            if (string.IsNullOrEmpty(gamePath))
                return "";
                
            try
            {
                return Path.Combine(gamePath, PathConstants.DataFolder);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync($"獲取 Data 路徑失敗: {gamePath}", ex);
                return "";
            }
        }

        public bool TryGetDataPath(string gamePath, out string path)
        {
            path = "";
            if (string.IsNullOrEmpty(gamePath)) return false;
            var result = GetDataPath(gamePath);
            if (!string.IsNullOrEmpty(result))
            {
                path = result;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 驗證是否為有效的 RimWorld 遊戲路徑
        /// </summary>
        public PathValidationResult IsValidGamePath(string path)
        {
            var result = new PathValidationResult();
            
            if (string.IsNullOrEmpty(path))
            {
                result.Status = PathValidationStatus.Error;
                result.Message = "路徑不能為空";
                return result;
            }
            
            if (!Directory.Exists(path))
            {
                result.Status = PathValidationStatus.Error;
                result.Message = "目錄不存在";
                return result;
            }
            
            try
            {
                // 檢查主程式檔案
                var executablePath = Path.Combine(path, PathConstants.GameExecutableName);
                if (!File.Exists(executablePath))
                {
                    // 嘗試其他平台的執行檔
                    executablePath = Path.Combine(path, PathConstants.GameExecutableNameLinux);
                    if (!File.Exists(executablePath))
                    {
                        executablePath = Path.Combine(path, PathConstants.GameExecutableNameMac);
                    }
                }
                
                result.ExecutablePath = executablePath;
                
                // 檢查 Data 資料夾
                var dataPath = GetDataPath(path);
                result.DataPath = dataPath;
                
                if (!Directory.Exists(dataPath))
                {
                    result.Status = PathValidationStatus.Warning;
                    result.Message = "目錄存在但缺少 Data 資料夾，可能不是完整的 RimWorld 安裝";
                    return result;
                }
                
                // 檢查是否有官方擴展資料夾（至少要有 Core）
                var corePath = Path.Combine(dataPath, "Core");
                if (!Directory.Exists(corePath))
                {
                    result.Status = PathValidationStatus.Warning;
                    result.Message = "Data 資料夾存在但缺少 Core 資料夾，可能不是有效的 RimWorld 安裝";
                    return result;
                }
                
                if (File.Exists(executablePath))
                {
                    result.IsValid = true;
                    result.Status = PathValidationStatus.Valid;
                    result.Message = "有效的 RimWorld 遊戲目錄";
                }
                else
                {
                    result.Status = PathValidationStatus.Warning;
                    result.Message = "Data 資料夾結構正確但找不到執行檔，可能是非標準安裝";
                }
            }
            catch (Exception ex)
            {
                result.Status = PathValidationStatus.Error;
                result.Message = $"驗證路徑時發生錯誤: {ex.Message}";
                _ = _loggerService.LogErrorAsync($"驗證遊戲路徑失敗: {path}", ex);
            }
            
            return result;
        }
        
        /// <summary>
        /// 獲取所有可能的 RimWorld 安裝路徑
        /// </summary>
        public List<string> GetPossibleGamePaths()
        {
            var paths = new List<string>();
            
            try
            {
                // 常見的 Steam 安裝路徑
                var steamPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                    @"C:\Steam",
                    @"D:\Steam",
                    @"E:\Steam",
                    @"F:\Steam"
                };
                
                foreach (var steamPath in steamPaths)
                {
                    if (Directory.Exists(steamPath))
                    {
                        var rimworldPath = Path.Combine(steamPath, "steamapps", "common", "RimWorld");
                        if (Directory.Exists(rimworldPath))
                        {
                            paths.Add(rimworldPath);
                        }
                    }
                }
                
                // 常見的遊戲安裝路徑
                var gamePaths = new[]
                {
                    @"C:\Games\RimWorld",
                    @"D:\Games\RimWorld",
                    @"E:\Games\RimWorld",
                    @"F:\Games\RimWorld"
                };
                
                foreach (var gamePath in gamePaths)
                {
                    if (Directory.Exists(gamePath))
                    {
                        paths.Add(gamePath);
                    }
                }
                
                // GOG Galaxy 路徑
                var gogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GOG Galaxy", "Games", "RimWorld");
                if (Directory.Exists(gogPath))
                {
                    paths.Add(gogPath);
                }
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("獲取可能的遊戲路徑失敗", ex);
            }
            
            return paths.Distinct().ToList();
        }
        
        /// <summary>
        /// 根據模組資料夾路徑獲取模組的 About.xml 路徑
        /// </summary>
        public string GetModAboutXmlPath(string modFolderPath)
        {
            if (string.IsNullOrEmpty(modFolderPath))
                return "";
                
            try
            {
                return Path.Combine(modFolderPath, PathConstants.ModAboutXml);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync($"獲取模組 About.xml 路徑失敗: {modFolderPath}", ex);
                return "";
            }
        }

        public bool TryGetModAboutXmlPath(string modFolderPath, out string path)
        {
            path = "";
            if (string.IsNullOrEmpty(modFolderPath)) return false;
            var result = GetModAboutXmlPath(modFolderPath);
            if (!string.IsNullOrEmpty(result))
            {
                path = result;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 根據模組資料夾路徑獲取模組的 Languages 資料夾路徑
        /// </summary>
        public string GetModLanguagesPath(string modFolderPath)
        {
            if (string.IsNullOrEmpty(modFolderPath))
                return "";
                
            try
            {
                return Path.Combine(modFolderPath, PathConstants.ModLanguagesFolder);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync($"獲取模組 Languages 路徑失敗: {modFolderPath}", ex);
                return "";
            }
        }

        public bool TryGetModLanguagesPath(string modFolderPath, out string path)
        {
            path = "";
            if (string.IsNullOrEmpty(modFolderPath)) return false;
            var result = GetModLanguagesPath(modFolderPath);
            if (!string.IsNullOrEmpty(result))
            {
                path = result;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 檢查路徑是否存在且可存取
        /// </summary>
        public bool PathExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
                
            try
            {
                return Directory.Exists(path) || File.Exists(path);
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync($"檢查路徑存在性失敗: {path}", ex);
                return false;
            }
        }

        /// <summary>
        /// 確保目錄存在，如果不存在則建立
        /// </summary>
        public bool EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return false;

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _ = _loggerService.LogInfoAsync($"已建立目錄: {directoryPath}");
                }
                return true;
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync($"建立目錄失敗: {directoryPath}", ex);
                return false;
            }
        }
        
        public string GetAppDataPath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "RimWorldTranslationTool");
            }
            catch (Exception ex)
            {
                _ = _loggerService.LogErrorAsync("獲取 AppData 路徑失敗", ex);
                return "";
            }
        }

        public string GetBackupDirectory()
        {
            return Path.Combine(GetAppDataPath(), "Backups");
        }

        public string GetSettingsFilePath()
        {
            return Path.Combine(GetAppDataPath(), "RimWorldTranslationTool_Settings.json");
        }
        
        public string GetLogsDirectory()
        {
            return Path.Combine(GetAppDataPath(), "Logs");
        }
    }
}
