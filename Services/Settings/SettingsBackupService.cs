using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.Settings
{
    /// <summary>
    /// 設定備份服務
    /// </summary>
    public class SettingsBackupService
    {
        private readonly string _backupDirectory;
        
        public SettingsBackupService()
        {
            _backupDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RimWorldTranslationTool", "Backups");
            
            // 確保備份目錄存在
            Directory.CreateDirectory(_backupDirectory);
        }
        
        /// <summary>
        /// 建立設定備份
        /// </summary>
        public async Task<string> CreateBackupAsync(AppSettings settings)
        {
            try
            {
                string fileName = $"Settings_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(_backupDirectory, fileName);
                
                var backupData = new SettingsBackup
                {
                    CreatedAt = DateTime.Now,
                    Version = "1.0",
                    Settings = settings
                };
                
                var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(filePath, json);
                
                Logger.Log($"設定備份已建立: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Logger.LogError("建立設定備份失敗", ex);
                throw;
            }
        }
        
        /// <summary>
        /// 還原設定備份
        /// </summary>
        public async Task<AppSettings> RestoreBackupAsync(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    throw new FileNotFoundException("備份檔案不存在", backupFilePath);
                }
                
                string json = await File.ReadAllTextAsync(backupFilePath);
                var backupData = JsonSerializer.Deserialize<SettingsBackup>(json);
                
                if (backupData?.Settings == null)
                {
                    throw new InvalidDataException("備份檔案格式無效");
                }
                
                Logger.Log($"設定備份已還原: {backupFilePath}");
                return backupData.Settings;
            }
            catch (Exception ex)
            {
                Logger.LogError("還原設定備份失敗", ex);
                throw;
            }
        }
        
        /// <summary>
        /// 獲取所有備份檔案
        /// </summary>
        public async Task<SettingsBackupInfo[]> GetBackupListAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var backupFiles = Directory.GetFiles(_backupDirectory, "Settings_Backup_*.json");
                    var backups = new System.Collections.Generic.List<SettingsBackupInfo>();
                    
                    foreach (var file in backupFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        backups.Add(new SettingsBackupInfo
                        {
                            FilePath = file,
                            FileName = fileInfo.Name,
                            CreatedAt = fileInfo.CreationTime,
                            Size = fileInfo.Length
                        });
                    }
                    
                    return backups.OrderByDescending(b => b.CreatedAt).ToArray();
                }
                catch (Exception ex)
                {
                    Logger.LogError("獲取備份列表失敗", ex);
                    return Array.Empty<SettingsBackupInfo>();
                }
            });
        }
        
        /// <summary>
        /// 刪除備份檔案
        /// </summary>
        public async Task<bool> DeleteBackupAsync(string backupFilePath)
        {
            try
            {
                if (File.Exists(backupFilePath))
                {
                    await Task.Run(() => File.Delete(backupFilePath));
                    Logger.Log($"備份檔案已刪除: {backupFilePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"刪除備份檔案失敗: {backupFilePath}", ex);
                return false;
            }
        }
    }
    
    /// <summary>
    /// 設定備份數據
    /// </summary>
    public class SettingsBackup
    {
        public DateTime CreatedAt { get; set; }
        public string Version { get; set; } = "";
        public AppSettings Settings { get; set; } = new AppSettings();
    }
    
    /// <summary>
    /// 設定備份資訊
    /// </summary>
    public class SettingsBackupInfo
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
    }
}
