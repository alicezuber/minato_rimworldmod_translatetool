using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool.Services.EmergencySave
{
    /// <summary>
    /// 緊急儲存服務實現 (基於組件註冊)
    /// </summary>
    public class EmergencySaveService : IEmergencySaveService
    {
        private readonly ConcurrentDictionary<string, ISavableComponent> _components = new();
        private readonly IPathService _pathService;
        private readonly ILoggerService _loggerService;
        private DateTime _lastSaveTime;
        private int _saveCount;

        public EmergencySaveService(IPathService pathService, ILoggerService loggerService)
        {
            _pathService = pathService;
            _loggerService = loggerService;
            
            _pathService.EnsureDirectoryExists(EmergencySaveDirectory);
        }

        public string EmergencySaveDirectory => Path.Combine(_pathService.GetAppDataPath(), "EmergencySaves");

        public bool HasUnsavedData => _components.Any(); // 簡化邏輯

        public DateTime LastSaveTime => _lastSaveTime;

        /// <summary>
        /// 註冊可儲存組件
        /// </summary>
        public void RegisterComponent(ISavableComponent component)
        {
            _components[component.ComponentName] = component;
        }

        /// <summary>
        /// 移除註冊組件
        /// </summary>
        public void UnregisterComponent(string componentName)
        {
            _components.TryRemove(componentName, out _);
        }

        public async Task<bool> EmergencySaveAllAsync()
        {
            var startTime = DateTime.Now;
            var results = new List<bool>();
            
            await _loggerService.LogWarningAsync("[EmergencySave] 開始執行緊急儲存流程...", "EmergencySave");

            var tasks = _components.Values.Select(async component =>
            {
                try
                {
                    await _loggerService.LogInfoAsync($"[EmergencySave] 正在儲存組件: {component.ComponentName}", "EmergencySave");
                    await component.SaveAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync($"[EmergencySave] 組件儲存失敗: {component.ComponentName}", ex, "EmergencySave");
                    return false;
                }
            });

            var taskResults = await Task.WhenAll(tasks);
            
            _lastSaveTime = DateTime.Now;
            _saveCount++;
            
            bool allSuccess = taskResults.All(r => r);
            var duration = DateTime.Now - startTime;
            
            await _loggerService.LogInfoAsync($"[EmergencySave] 緊急儲存完成。耗時: {duration.TotalMilliseconds}ms, 成功: {allSuccess}", "EmergencySave");
            
            return allSuccess;
        }

        public async Task<bool> SaveSettingsAsync() => await SaveComponentAsync("Settings");
        public async Task<bool> SaveWorkInProgressAsync() => await SaveComponentAsync("WorkInProgress");
        public async Task<bool> SaveApplicationStateAsync() => await SaveComponentAsync("ApplicationState");
        public async Task<bool> SaveModDataAsync() => await SaveComponentAsync("ModData");

        public async Task<EmergencySaveStatus> GetStatusAsync()
        {
            return await Task.FromResult(new EmergencySaveStatus
            {
                IsInProgress = false,
                LastSaveTime = _lastSaveTime,
                SaveCount = _saveCount,
                EmergencySaveDirectory = EmergencySaveDirectory
            });
        }

        public async Task CleanupOldSavesAsync(int keepCount = 5)
        {
            // 實作清理邏輯...
            await Task.CompletedTask;
        }

        private async Task<bool> SaveComponentAsync(string name)
        {
            if (_components.TryGetValue(name, out var component))
            {
                try
                {
                    await component.SaveAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
