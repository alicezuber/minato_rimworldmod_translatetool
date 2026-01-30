using System;
using System.Threading.Tasks;

namespace RimWorldTranslationTool.Services.Dialogs
{
    /// <summary>
    /// 彈窗服務介面
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// 顯示成功訊息
        /// </summary>
        Task ShowSuccessAsync(string message, string title = "成功");
        
        /// <summary>
        /// 顯示資訊訊息
        /// </summary>
        Task ShowInfoAsync(string message, string title = "資訊");
        
        /// <summary>
        /// 顯示警告訊息
        /// </summary>
        Task ShowWarningAsync(string message, string title = "警告");
        
        /// <summary>
        /// 顯示錯誤訊息
        /// </summary>
        Task ShowErrorAsync(string message, Exception? exception = null, string title = "錯誤");
        
        /// <summary>
        /// 顯示嚴重錯誤訊息
        /// </summary>
        Task ShowCriticalErrorAsync(string message, Exception? exception = null, string title = "嚴重錯誤");
        
        /// <summary>
        /// 顯示確認對話框
        /// </summary>
        Task<bool> ShowConfirmationAsync(string message, string title = "確認");
        
        /// <summary>
        /// 顯示是/否/取消對話框
        /// </summary>
        Task<DialogResult> ShowYesNoCancelAsync(string message, string title = "選擇");
        
        /// <summary>
        /// 顯示輸入對話框
        /// </summary>
        Task<string?> ShowInputDialogAsync(string message, string title = "輸入", string defaultValue = "");
        
        /// <summary>
        /// 顯示選擇對話框
        /// </summary>
        Task<T?> ShowSelectionDialogAsync<T>(string message, string title, T[] options, T? defaultOption = default)
            where T : class;
        
        /// <summary>
        /// 顯示進度對話框
        /// </summary>
        Task ShowProgressDialogAsync(string title, string message, IProgress<ProgressReport> progress);
        
        /// <summary>
        /// 顯示關於對話框
        /// </summary>
        Task ShowAboutAsync();
        
        /// <summary>
        /// 顯示日誌檢視器
        /// </summary>
        Task ShowLogViewerAsync();
        
        /// <summary>
        /// 顯示設定對話框
        /// </summary>
        Task ShowSettingsAsync();
    }
    
    /// <summary>
    /// 對話框結果
    /// </summary>
    public enum DialogResult
    {
        None,
        Yes,
        No,
        Cancel,
        OK,
        Abort,
        Retry,
        Ignore
    }
    
    /// <summary>
    /// 進度報告
    /// </summary>
    public class ProgressReport
    {
        public int Percentage { get; set; }
        public string Message { get; set; } = "";
        public bool IsIndeterminate { get; set; }
        public bool CanCancel { get; set; }
        public bool IsCancelled { get; set; }
        
        public static ProgressReport Create(int percentage, string message = "", bool canCancel = false)
        {
            return new ProgressReport
            {
                Percentage = percentage,
                Message = message,
                CanCancel = canCancel
            };
        }
        
        public static ProgressReport CreateIndeterminate(string message = "", bool canCancel = false)
        {
            return new ProgressReport
            {
                IsIndeterminate = true,
                Message = message,
                CanCancel = canCancel
            };
        }
    }
}
