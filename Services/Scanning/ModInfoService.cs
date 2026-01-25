using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using RimWorldTranslationTool.Services.Infrastructure;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool.Services.Scanning
{
    /// <summary>
    /// 模組資訊服務實現
    /// </summary>
    public class ModInfoService : IModInfoService
    {
        private readonly IXmlParserService _xmlParser;
        private readonly IPathService _pathService;
        private readonly ILoggerService _logger;

        public ModInfoService(IXmlParserService xmlParser, IPathService pathService, ILoggerService logger)
        {
            _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsValidModDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var aboutXmlPath = _pathService.GetModAboutXmlPath(path);
            return File.Exists(aboutXmlPath);
        }

        public ModInfo LoadModInfo(string modPath)
        {
            try
            {
                if (!IsValidModDirectory(modPath))
                {
                    _logger.LogWarningAsync($"無效的模組目錄: {modPath}").Wait();
                    return null;
                }

                var aboutXmlPath = _pathService.GetModAboutXmlPath(modPath);
                var aboutXml = _xmlParser.LoadXml(aboutXmlPath);
                
                if (aboutXml?.Root == null)
                {
                    _logger.LogWarningAsync($"無法解析 About.xml: {aboutXmlPath}").Wait();
                    return null;
                }

                var meta = aboutXml.Root.Element("ModMetaData");
                if (meta == null)
                {
                    _logger.LogWarningAsync($"找不到 ModMetaData 元素: {aboutXmlPath}").Wait();
                    return null;
                }

                var folderName = Path.GetFileName(modPath);
                var packageId = _xmlParser.GetElementValue(meta, "packageId");
                var name = _xmlParser.GetElementValue(meta, "name");

                _logger.LogInfoAsync($"載入模組: {name} ({packageId})").Wait();

                var modInfo = new ModInfo
                {
                    FolderName = folderName,
                    Name = name,
                    Author = _xmlParser.GetElementValue(meta, "author"),
                    PackageId = packageId,
                    SupportedVersions = _xmlParser.GetVersionsString(meta.Element("supportedVersions")),
                    SupportedLanguages = DetectSupportedLanguages(modPath),  // 新增：檢測支援語言
                    
                    // 新增：完整 About.xml 支援
                    Description = _xmlParser.GetDescription(meta),
                    Url = _xmlParser.GetUrl(meta),
                    ModVersion = _xmlParser.GetModVersion(meta),
                    ModDependencies = _xmlParser.GetModDependencies(meta),
                    ModDependenciesByVersion = _xmlParser.GetModDependenciesByVersion(meta),
                    LoadAfter = _xmlParser.GetLoadAfterList(meta),
                    IncompatibleWith = _xmlParser.GetIncompatibleWithList(meta),
                    
                    IsVersionCompatible = CheckVersionCompatibility(_xmlParser.GetVersionsString(meta.Element("supportedVersions")))
                };

                // 載入預覽圖
                LoadPreviewImage(modInfo, modPath);

                return modInfo;
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync($"載入模組資訊失敗: {modPath}", ex).Wait();
                return null;
            }
        }

        private void LoadPreviewImage(ModInfo modInfo, string modPath)
        {
            // 嘗試標準路徑
            string previewPath = Path.Combine(modPath, "About", "Preview.png");
            
            // 如果不存在，嘗試核心模組路徑
            if (!File.Exists(previewPath))
            {
                previewPath = Path.Combine(modPath, "Preview.png");
            }

            if (File.Exists(previewPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(previewPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    modInfo.PreviewImage = bitmap;
                }
                catch (Exception ex)
                {
                    _logger.LogErrorAsync($"載入預覽圖片失敗: {previewPath}", ex).Wait();
                    // 預覽圖載入失敗不影響模組資訊
                }
            }
        }

        private bool CheckVersionCompatibility(string supportedVersions)
        {
            // 簡化版本相容性檢查 - 暫時返回 true
            // 未來可以根據需要實現更複雜的版本檢查邏輯
            return true;
        }

        private string DetectSupportedLanguages(string modPath)
        {
            try
            {
                var languagesPath = _pathService.GetModLanguagesPath(modPath);
                
                if (!Directory.Exists(languagesPath))
                {
                    return "unknown";
                }

                var languageDirs = Directory.GetDirectories(languagesPath);
                var languages = new List<string>();

                foreach (var dir in languageDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    if (!string.IsNullOrEmpty(dirName))
                    {
                        languages.Add(dirName);
                    }
                }

                return languages.Count > 0 ? string.Join(", ", languages) : "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync($"檢測支援語言失敗: {modPath}", ex).Wait();
                return "unknown";
            }
        }
    }
}
