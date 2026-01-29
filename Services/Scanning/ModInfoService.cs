using System;
using System.Collections.Generic;
using System.IO;
using RimWorldTranslationTool.Models;
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

            return _pathService.TryGetModAboutXmlPath(path, out var aboutXmlPath) && File.Exists(aboutXmlPath);
        }

        public ModModel? LoadModInfo(string modPath)
        {
            try
            {
                if (!IsValidModDirectory(modPath))
                {
                    _logger.LogWarningAsync($"無效的模組目錄: {modPath}").Wait();
                    return null;
                }

                if (!_pathService.TryGetModAboutXmlPath(modPath, out var aboutXmlPath))
                {
                    _logger.LogWarningAsync($"無法獲取 About.xml 路徑: {modPath}").Wait();
                    return null;
                }
                
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

                var modModel = new ModModel
                {
                    FolderName = folderName,
                    Name = name,
                    Author = _xmlParser.GetElementValue(meta, "author"),
                    PackageId = packageId,
                    SupportedVersions = _xmlParser.GetVersionsString(meta.Element("supportedVersions")),
                    SupportedLanguages = DetectSupportedLanguages(modPath),
                    
                    Description = _xmlParser.GetDescription(meta),
                    Url = _xmlParser.GetUrl(meta),
                    ModVersion = _xmlParser.GetModVersion(meta),
                    ModDependencies = _xmlParser.GetModDependencies(meta),
                    ModDependenciesByVersion = _xmlParser.GetModDependenciesByVersion(meta),
                    LoadAfter = _xmlParser.GetLoadAfterList(meta),
                    IncompatibleWith = _xmlParser.GetIncompatibleWithList(meta),
                    
                    IsVersionCompatible = CheckVersionCompatibility(_xmlParser.GetVersionsString(meta.Element("supportedVersions")))
                };

                // 獲取預覽圖路徑
                modModel.PreviewImagePath = GetPreviewImagePath(modPath);

                return modModel;
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync($"載入模組資訊失敗: {modPath}", ex).Wait();
                return null;
            }
        }

        private string? GetPreviewImagePath(string modPath)
        {
            // 嘗試標準路徑
            string previewPath = Path.Combine(modPath, "About", "Preview.png");
            if (File.Exists(previewPath)) return previewPath;

            // 如果不存在，嘗試核心模組路徑
            previewPath = Path.Combine(modPath, "Preview.png");
            if (File.Exists(previewPath)) return previewPath;

            return null;
        }

        private bool CheckVersionCompatibility(string supportedVersions)
        {
            return true;
        }

        private string DetectSupportedLanguages(string modPath)
        {
            try
            {
                if (!_pathService.TryGetModLanguagesPath(modPath, out var languagesPath) || !Directory.Exists(languagesPath))
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
