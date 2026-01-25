using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorldTranslationTool.Services.Logging;
using RimWorldTranslationTool.Services.Paths;

namespace RimWorldTranslationTool.Services.Scanning
{
    /// <summary>
    /// 模組掃描服務實現
    /// </summary>
    public class ModScannerService : IModScannerService
    {
        private readonly IModInfoService _modInfoService;
        private readonly IPathService _pathService;
        private readonly ILoggerService _logger;

        public ModScannerService(IModInfoService modInfoService, IPathService pathService, ILoggerService logger)
        {
            _modInfoService = modInfoService ?? throw new ArgumentNullException(nameof(modInfoService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ModInfo>> ScanModsAsync(string gamePath, IProgress<ScanProgress> progress = null)
        {
            var allModInfos = new List<ModInfo>();
            
            // 1. 掃描本地模組
            var localDirectories = CollectLocalModDirectories(gamePath);
            if (localDirectories.Any())
            {
                var localMods = await ScanDirectoriesAsync(localDirectories, progress, ModSource.Local);
                allModInfos.AddRange(localMods);
            }
            
            // 2. 掃描核心模組
            var dataDirectories = CollectDataDirectories(gamePath);
            if (dataDirectories.Any())
            {
                var coreMods = await ScanDirectoriesAsync(dataDirectories, progress, ModSource.Official);
                allModInfos.AddRange(coreMods);
            }
            
            // 3. 掃描工作坊模組
            var workshopDirectories = CollectWorkshopDirectories(gamePath);
            if (workshopDirectories.Any())
            {
                var workshopMods = await ScanDirectoriesAsync(workshopDirectories, progress, ModSource.Steam);
                allModInfos.AddRange(workshopMods);
            }

            return allModInfos;
        }

        public async Task<List<ModInfo>> ScanLocalModsAsync(string gamePath, IProgress<ScanProgress> progress = null)
        {
            var localDirectories = CollectLocalModDirectories(gamePath);
            return await ScanDirectoriesAsync(localDirectories, progress, ModSource.Local);
        }

        private List<string> CollectAllModDirectories(string gamePath)
        {
            var allDirectories = new List<string>();
            
            // 1. 本體模組 (Mods 資料夾)
            AddLocalModsDirectories(allDirectories, gamePath);
            
            // 2. 核心模組 (Data 資料夾)
            AddDataDirectories(allDirectories, gamePath);
            
            // 3. 工作坊模組
            AddWorkshopDirectories(allDirectories, gamePath);
            
            return allDirectories;
        }

        private List<string> CollectLocalModDirectories(string gamePath)
        {
            var directories = new List<string>();
            AddLocalModsDirectories(directories, gamePath);
            return directories;
        }

        private List<string> CollectDataDirectories(string gamePath)
        {
            var directories = new List<string>();
            AddDataDirectories(directories, gamePath);
            return directories;
        }

        private List<string> CollectWorkshopDirectories(string gamePath)
        {
            var directories = new List<string>();
            AddWorkshopDirectories(directories, gamePath);
            return directories;
        }

        private void AddLocalModsDirectories(List<string> directories, string gamePath)
        {
            var modsPath = _pathService.GetLocalModsPath(gamePath);
            if (_pathService.PathExists(modsPath))
            {
                directories.AddRange(Directory.GetDirectories(modsPath));
                _logger.LogInfoAsync($"掃描本地模組: {modsPath}").Wait();
            }
            else
            {
                _logger.LogWarningAsync($"本地模組目錄不存在: {modsPath}").Wait();
            }
        }

        private void AddDataDirectories(List<string> directories, string gamePath)
        {
            var dataPath = _pathService.GetDataPath(gamePath);
            if (!_pathService.PathExists(dataPath))
            {
                _logger.LogWarningAsync($"Data 目錄不存在: {dataPath}").Wait();
                return;
            }

            var dataDirs = Directory.GetDirectories(dataPath);
            _logger.LogInfoAsync($"檢查 Data 目錄，找到 {dataDirs.Length} 個子目錄").Wait();

            int coreModsAdded = 0;
            foreach (var dir in dataDirs)
            {
                if (_modInfoService.IsValidModDirectory(dir))
                {
                    directories.Add(dir);
                    _logger.LogInfoAsync($"✅ 加入核心模組: {Path.GetFileName(dir)}").Wait();
                    coreModsAdded++;
                }
            }

            _logger.LogInfoAsync($"Data 目錄掃描完成，新增 {coreModsAdded} 個核心模組").Wait();
        }

        private void AddWorkshopDirectories(List<string> directories, string gamePath)
        {
            var workshopPath = _pathService.GetWorkshopPath(gamePath);
            if (_pathService.PathExists(workshopPath))
            {
                directories.AddRange(Directory.GetDirectories(workshopPath));
                _logger.LogInfoAsync($"掃描工作坊模組: {workshopPath}").Wait();
            }
            else
            {
                _logger.LogWarningAsync($"工作坊目錄不存在: {workshopPath}").Wait();
            }
        }

        private async Task<List<ModInfo>> ScanDirectoriesAsync(List<string> directories, IProgress<ScanProgress> progress = null, ModSource source = ModSource.Unknown)
        {
            var modInfos = new List<ModInfo>();
            int total = directories.Count;
            int processed = 0;

            _logger.LogInfoAsync($"開始掃描 {total} 個模組目錄 (來源: {source})").Wait();

            await Task.Run(() =>
            {
                foreach (var dir in directories)
                {
                    var modInfo = _modInfoService.LoadModInfo(dir);
                    if (modInfo != null)
                    {
                        modInfo.Source = source; // 設置模組來源
                        modInfos.Add(modInfo);
                    }

                    processed++;
                    
                    // 報告進度
                    if (progress != null)
                    {
                        var scanProgress = new ScanProgress
                        {
                            Processed = processed,
                            Total = total,
                            CurrentMod = Path.GetFileName(dir),
                            Status = $"已處理 {processed}/{total}"
                        };
                        progress.Report(scanProgress);
                    }
                }
            });

            _logger.LogInfoAsync($"掃描完成，找到 {modInfos.Count} 個有效模組").Wait();
            return modInfos;
        }
    }
}
