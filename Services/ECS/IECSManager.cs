using System;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.ErrorHandling;

namespace RimWorldTranslationTool.Services.ECS
{
    /// <summary>
    /// ECS (Error, Crash, Save) 協調管理器介面
    /// </summary>
    public interface IECSManager
    {
        /// <summary>
        /// 處理全域未處理異常
        /// </summary>
        Task HandleGlobalExceptionAsync(Exception exception, string source);

        /// <summary>
        /// 執行緊急關閉流程（C -> S -> Shutdown）
        /// </summary>
        Task PerformEmergencyShutdownAsync(Exception exception, string context);
        
        /// <summary>
        /// 獲取當前 ECS 狀態
        /// </summary>
        ECSStatus GetStatus();
    }

    public class ECSStatus
    {
        public bool IsEmergencyMode { get; set; }
        public DateTime LastIncidentTime { get; set; }
        public string LastError { get; set; } = "";
    }
}
