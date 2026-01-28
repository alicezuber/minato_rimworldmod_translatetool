using System.Text.Encodings.Web;
using System.Text.Json;

namespace RimWorldTranslationTool.Services.Infrastructure
{
    /// <summary>
    /// JSON 序列化配置
    /// </summary>
    public static class JsonConfiguration
    {
        /// <summary>
        /// 預設的 JSON 序列化選項
        /// </summary>
        public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null // 保持與現有設定檔案相容
        };
    }
}
