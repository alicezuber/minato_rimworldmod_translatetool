using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorldTranslationTool.Services.Logging;

namespace RimWorldTranslationTool.Services.Infrastructure
{
    /// <summary>
    /// XML 解析服務實現
    /// </summary>
    public class XmlParserService : IXmlParserService
    {
        private readonly ILoggerService _logger;

        public XmlParserService(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public XDocument LoadXml(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarningAsync($"XML 檔案不存在: {filePath}").Wait();
                    return null;
                }

                return XDocument.Load(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync($"載入 XML 檔案失敗: {filePath}", ex).Wait();
                return null;
            }
        }

        public string GetElementValue(XElement parent, string elementName)
        {
            try
            {
                var element = parent?.Element(elementName);
                return element?.Value ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync($"獲取 XML 元素值失敗: {elementName}", ex).Wait();
                return "";
            }
        }

        /// <summary>
        /// 獲取版本列表字串
        /// </summary>
        /// <param name="versionsElement">版本元素</param>
        /// <returns>版本字串，逗號分隔</returns>
        public string GetVersionsString(XElement versionsElement)
        {
            try
            {
                if (versionsElement == null)
                    return "";

                var versions = versionsElement.Elements("li")
                    .Select(v => v.Value)
                    .ToArray();

                return string.Join(", ", versions);
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("解析版本列表失敗", ex).Wait();
                return "";
            }
        }

        /// <summary>
        /// 獲取模組描述
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>模組描述</returns>
        public string GetDescription(XElement parent)
        {
            try
            {
                var descElement = parent?.Element("description");
                return descElement?.Value ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("獲取模組描述失敗", ex).Wait();
                return "";
            }
        }

        /// <summary>
        /// 獲取模組版本
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>模組版本</returns>
        public string GetModVersion(XElement parent)
        {
            try
            {
                var versionElement = parent?.Element("modVersion");
                return versionElement?.Value ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("獲取模組版本失敗", ex).Wait();
                return "";
            }
        }

        /// <summary>
        /// 獲取模組 URL
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>模組 URL</returns>
        public string GetUrl(XElement parent)
        {
            try
            {
                var urlElement = parent?.Element("url");
                return urlElement?.Value ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("獲取模組 URL 失敗", ex).Wait();
                return "";
            }
        }

        /// <summary>
        /// 獲取模組依賴列表
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>模組依賴列表</returns>
        public List<ModDependency> GetModDependencies(XElement parent)
        {
            var dependencies = new List<ModDependency>();
            
            try
            {
                var depsElement = parent?.Element("modDependencies");
                if (depsElement == null) return dependencies;

                foreach (var depElement in depsElement.Elements("li"))
                {
                    var dependency = new ModDependency
                    {
                        PackageId = GetElementValue(depElement, "packageId"),
                        DisplayName = GetElementValue(depElement, "displayName"),
                        SteamWorkshopUrl = GetElementValue(depElement, "steamWorkshopUrl"),
                        DownloadUrl = GetElementValue(depElement, "downloadUrl")
                    };
                    
                    dependencies.Add(dependency);
                }
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("解析模組依賴失敗", ex).Wait();
            }
            
            return dependencies;
        }

        /// <summary>
        /// 獲取版本特定依賴列表
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>版本特定依賴列表</returns>
        public List<ModDependency> GetModDependenciesByVersion(XElement parent)
        {
            var dependencies = new List<ModDependency>();
            
            try
            {
                var depsByVersionElement = parent?.Element("modDependenciesByVersion");
                if (depsByVersionElement == null) return dependencies;

                foreach (var versionElement in depsByVersionElement.Elements())
                {
                    var targetVersion = versionElement.Name.LocalName;
                    
                    foreach (var depElement in versionElement.Elements("li"))
                    {
                        var dependency = new ModDependency
                        {
                            PackageId = GetElementValue(depElement, "packageId"),
                            DisplayName = GetElementValue(depElement, "displayName"),
                            SteamWorkshopUrl = GetElementValue(depElement, "steamWorkshopUrl"),
                            DownloadUrl = GetElementValue(depElement, "downloadUrl"),
                            TargetVersion = targetVersion
                        };
                        
                        dependencies.Add(dependency);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("解析版本特定依賴失敗", ex).Wait();
            }
            
            return dependencies;
        }

        /// <summary>
        /// 獲取載入順序列表
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>載入順序列表</returns>
        public List<string> GetLoadAfterList(XElement parent)
        {
            var loadAfter = new List<string>();
            
            try
            {
                var loadAfterElement = parent?.Element("loadAfter");
                if (loadAfterElement == null) return loadAfter;

                loadAfter = loadAfterElement.Elements("li")
                    .Select(li => li.Value)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("解析載入順序失敗", ex).Wait();
            }
            
            return loadAfter;
        }

        /// <summary>
        /// 獲取不相容模組列表
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>不相容模組列表</returns>
        public List<string> GetIncompatibleWithList(XElement parent)
        {
            var incompatible = new List<string>();
            
            try
            {
                var incompatibleElement = parent?.Element("incompatibleWith");
                if (incompatibleElement == null) return incompatible;

                incompatible = incompatibleElement.Elements("li")
                    .Select(li => li.Value)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("解析不相容模組失敗", ex).Wait();
            }
            
            return incompatible;
        }
    }
}
