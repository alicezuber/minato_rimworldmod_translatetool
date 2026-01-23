using System;
using System.IO;
using System.Text;

namespace RimWorldTranslationTool
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n_test.log");
        
        public static void Log(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}";
                
                // 同時寫入 Debug 和檔案
                System.Diagnostics.Debug.WriteLine(logMessage);
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logger 錯誤: {ex.Message}");
            }
        }
        
        public static void LogError(string message, Exception ex)
        {
            Log($"❌ 錯誤: {message}");
            Log($"   詳情: {ex.Message}");
            Log($"   堆疊: {ex.StackTrace}");
        }
        
        public static void LogSuccess(string message)
        {
            Log($"✅ 成功: {message}");
        }
        
        public static void LogInfo(string message)
        {
            Log($"ℹ️ 信息: {message}");
        }
        
        public static void LogWarning(string message)
        {
            Log($"⚠️ 警告: {message}");
        }
    }
}
