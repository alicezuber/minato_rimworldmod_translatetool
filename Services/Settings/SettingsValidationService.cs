using System;
using System.IO;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.Settings
{
    /// <summary>
    /// 設定驗證服務
    /// </summary>
    public class SettingsValidationService
    {
        /// <summary>
        /// 驗證遊戲路徑
        /// </summary>
        public async Task<ValidationResult> ValidateGamePathAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "路徑不能為空",
                        Status = ValidationStatus.Error
                    };
                }
                
                if (!Directory.Exists(path))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "目錄不存在",
                        Status = ValidationStatus.Error
                    };
                }
                
                // 檢查是否為有效的 RimWorld 目錄
                string exePath = Path.Combine(path, "RimWorldWin64.exe");
                string dataPath = Path.Combine(path, "Data");
                string aboutPath = Path.Combine(path, "About", "About.xml");
                
                if (File.Exists(exePath) && Directory.Exists(dataPath))
                {
                    return new ValidationResult
                    {
                        IsValid = true,
                        Message = "有效的 RimWorld 目錄",
                        Status = ValidationStatus.Valid
                    };
                }
                else if (Directory.Exists(path))
                {
                    // 檢查是否有 About.xml（可能是模組目錄）
                    if (File.Exists(aboutPath))
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            Message = "這是模組目錄，不是遊戲目錄",
                            Status = ValidationStatus.Error
                        };
                    }
                    
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "目錄存在但可能不是 RimWorld 遊戲目錄",
                        Status = ValidationStatus.Warning
                    };
                }
                
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "無效的路徑",
                    Status = ValidationStatus.Error
                };
            });
        }
        
        /// <summary>
        /// 驗證 ModsConfig.xml 路徑
        /// </summary>
        public async Task<ValidationResult> ValidateModsConfigPathAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "未選擇檔案",
                        Status = ValidationStatus.Warning
                    };
                }
                
                if (!File.Exists(path))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "檔案不存在",
                        Status = ValidationStatus.Error
                    };
                }
                
                if (!Path.GetFileName(path).Equals("ModsConfig.xml", StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "必須是 ModsConfig.xml 檔案",
                        Status = ValidationStatus.Error
                    };
                }
                
                // 嘗試解析檔案內容
                try
                {
                    string content = File.ReadAllText(path);
                    if (content.Contains("<ModsConfig>") && content.Contains("<activeMods>"))
                    {
                        return new ValidationResult
                        {
                            IsValid = true,
                            Message = "有效的 ModsConfig.xml 檔案",
                            Status = ValidationStatus.Valid
                        };
                    }
                    else
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            Message = "檔案格式不正確",
                            Status = ValidationStatus.Error
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = $"讀取檔案失敗: {ex.Message}",
                        Status = ValidationStatus.Error
                    };
                }
            });
        }
    }
}
