using System;
using System.IO;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool.Services.Settings
{
    /// <summary>
    /// 設定驗證服務
    /// </summary>
    public class SettingsValidationService
    {
        private readonly IPathService _pathService;
        
        public SettingsValidationService(IPathService pathService)
        {
            _pathService = pathService;
        }
        
        /// <summary>
        /// 驗證遊戲路徑
        /// </summary>
        public async Task<ValidationResult> ValidateGamePathAsync(string path)
        {
            return await Task.Run(() =>
            {
                var pathResult = _pathService.IsValidGamePath(path);
                
                return new ValidationResult
                {
                    IsValid = pathResult.IsValid,
                    Message = pathResult.Message,
                    Status = (ValidationStatus)pathResult.Status
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
                
                if (!Path.GetFileName(path).Equals(PathConstants.ModsConfigFileName, StringComparison.OrdinalIgnoreCase))
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
