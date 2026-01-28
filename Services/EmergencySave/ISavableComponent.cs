using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.EmergencySave
{
    /// <summary>
    /// 可緊急儲存的組件介面
    /// </summary>
    public interface ISavableComponent
    {
        /// <summary>
        /// 組件名稱
        /// </summary>
        string ComponentName { get; }

        /// <summary>
        /// 執行緊急儲存
        /// </summary>
        Task SaveAsync();
    }
}
