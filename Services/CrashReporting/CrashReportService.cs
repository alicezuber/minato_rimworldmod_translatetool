using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Infrastructure;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool.Services.CrashReporting
{
    /// <summary>
    /// 崩潰報告服務實現
    /// </summary>
    public class CrashReportService : ICrashReportService
    {
        private static readonly Assembly _executingAssembly = Assembly.GetExecutingAssembly();
        private readonly IPathService _pathService;

        public CrashReportService(IPathService pathService)
        {
            _pathService = pathService;
            
            // 確保目錄存在
            _pathService.EnsureDirectoryExists(CrashReportDirectory);
        }

        public bool IsAutoSendEnabled { get; set; } = false;

        public string CrashReportDirectory => Path.Combine(_pathService.GetAppDataPath(), "CrashReports");

        public async Task<CrashReport> GenerateCrashReportAsync(Exception exception, string context = "")
        {
            await Task.CompletedTask;
            var report = new CrashReport
            {
                Timestamp = DateTime.Now,
                Exception = exception,
                Context = context,
                StackTrace = exception.StackTrace ?? "",
                InnerExceptions = GetInnerExceptions(exception),
                SystemInfo = GetSystemInfo(),
                ApplicationInfo = GetApplicationInfo()
            };

            return report;
        }

        public async Task SaveCrashReportAsync(CrashReport report)
        {
            string fileName = $"CrashReport_{report.Timestamp:yyyyMMdd_HHmmss}_{report.Id.Substring(0, 8)}.json";
            string filePath = Path.Combine(CrashReportDirectory, fileName);
            report.ReportPath = filePath;

            // 由於 Exception 對象序列化比較複雜，我們將其簡化為字串
            var reportData = new
            {
                report.Id,
                report.Timestamp,
                ExceptionType = report.Exception.GetType().FullName,
                ExceptionMessage = report.Exception.Message,
                report.Context,
                report.StackTrace,
                report.InnerExceptions,
                report.SystemInfo,
                report.ApplicationInfo,
                report.UserActionInfo
            };

            string json = JsonSerializer.Serialize(reportData, JsonConfiguration.DefaultOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<bool> SendCrashReportAsync(CrashReport report)
        {
            // 目前僅模擬發送，未來可以整合遠端 API
            await Task.Delay(500);
            return true;
        }

        public async Task<CrashReport[]> GetAllCrashReportsAsync()
        {
            if (!Directory.Exists(CrashReportDirectory)) return Array.Empty<CrashReport>();

            var files = Directory.GetFiles(CrashReportDirectory, "*.json");
            var reports = new List<CrashReport>();

            foreach (var file in files)
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    // 這裡簡化處理，實際可能需要反序列化回來的邏輯
                }
                catch { /* 忽略損壞的報告 */ }
            }

            return reports.ToArray();
        }

        public async Task CleanupOldReportsAsync(int keepDays = 30)
        {
            if (!Directory.Exists(CrashReportDirectory)) return;

            var threshold = DateTime.Now.AddDays(-keepDays);
            var files = Directory.GetFiles(CrashReportDirectory, "*.json");

            foreach (var file in files)
            {
                if (File.GetCreationTime(file) < threshold)
                {
                    File.Delete(file);
                }
            }
            await Task.CompletedTask;
        }

        private string GetInnerExceptions(Exception ex)
        {
            var inner = ex.InnerException;
            if (inner == null) return "";

            var messages = new List<string>();
            while (inner != null)
            {
                messages.Add($"{inner.GetType().Name}: {inner.Message}");
                inner = inner.InnerException;
            }
            return string.Join(" | ", messages);
        }

        private SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                OSVersion = Environment.OSVersion.ToString(),
                OSArchitecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                DotNetVersion = RuntimeInformation.FrameworkDescription,
                CultureName = System.Globalization.CultureInfo.CurrentCulture.Name,
                TotalMemory = GC.GetTotalMemory(false) // 簡化獲取記憶體資訊
            };
        }

        private ApplicationInfo GetApplicationInfo()
        {
            var assemblyName = _executingAssembly.GetName();
            return new ApplicationInfo
            {
                ApplicationName = assemblyName.Name ?? "RimWorldTranslationTool",
                Version = assemblyName.Version?.ToString() ?? "1.0.0",
                IsDebugMode = false, // 這裡可以根據編譯符號調整
                LoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name ?? "").ToArray()
            };
        }
    }
}
