using System;
using System.Collections.Generic;
using System.Xml.Linq;
using RimWorldTranslationTool.Models;

namespace RimWorldTranslationTool.Services.Infrastructure
{
    /// <summary>
    /// XML 解析服務介面
    /// </summary>
    public interface IXmlParserService
    {
        /// <summary>
        /// 載入並解析 XML 檔案
        /// </summary>
        /// <param name="filePath">XML 檔案路徑</param>
        /// <returns>解析的 XDocument，失敗返回 null</returns>
        XDocument LoadXml(string filePath);
        
        /// <summary>
        /// 安全獲取 XML 元素值
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="elementName">元素名稱</param>
        /// <returns>元素值，不存在返回空字串</returns>
        string GetElementValue(XElement parent, string elementName);
        
        /// <summary>
        /// 獲取版本列表字串
        /// </summary>
        /// <param name="versionsElement">版本元素</param>
        /// <returns>版本字串，逗號分隔</returns>
        string GetVersionsString(XElement versionsElement);
        
        /// <summary>
        /// 獲取模組描述
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>模組描述</returns>
        string GetDescription(XElement parent);
        
        /// <summary>
        /// 獲取模組版本
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>模組版本</returns>
        string GetModVersion(XElement parent);
        
        /// <summary>
        /// 獲取模組 URL
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>模組 URL</returns>
        string GetUrl(XElement parent);
        
        /// <summary>
        /// 獲取模組依賴列表
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>模組依賴列表</returns>
        List<ModDependency> GetModDependencies(XElement parent);
        
        /// <summary>
        /// 獲取版本特定依賴列表
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>版本特定依賴列表</returns>
        List<ModDependency> GetModDependenciesByVersion(XElement parent);
        
        /// <summary>
        /// 獲取載入順序列表
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>載入順序列表</returns>
        List<string> GetLoadAfterList(XElement parent);
        
        /// <summary>
        /// 獲取不相容模組列表
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>不相容模組列表</returns>
        List<string> GetIncompatibleWithList(XElement parent);
    }
}
