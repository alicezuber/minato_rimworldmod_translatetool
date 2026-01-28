using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using RimWorldTranslationTool.Models;
using RimWorldTranslationTool.Services.Scanning;
using RimWorldTranslationTool.Services.Paths;
using RimWorldTranslationTool.Services.Logging;

namespace RimWorldTranslationTool.ViewModels
{
    /// <summary>
    /// 主視窗的 ViewModel
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly IModScannerService _modScannerService;
        private readonly IPathService _pathService;
        private readonly ILoggerService _logger;

        private string _gamePath = "";
        private bool _isScanning = false;
        private ModViewModel? _selectedMod;
        private string _statusText = "準備就緒";
        private double _scanProgress = 0;
        
        public MainViewModel(
            IModScannerService modScannerService,
            IPathService pathService,
            ILoggerService logger)
        {
            _modScannerService = modScannerService;
            _pathService = pathService;
            _logger = logger;

            Mods = new ObservableCollection<ModViewModel>();
            LocalMods = new ObservableCollection<ModViewModel>();
            
            ScanCommand = new RelayCommand(async _ => await ScanModsAsync(), _ => !IsScanning);
        }

        // 屬性
        public ObservableCollection<ModViewModel> Mods { get; }
        public ObservableCollection<ModViewModel> LocalMods { get; }

        public string GamePath
        {
            get => _gamePath;
            set
            {
                if (SetProperty(ref _gamePath, value))
                {
                    // 當路徑改變時，自動更新狀態
                }
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            private set => SetProperty(ref _isScanning, value);
        }

        public ModViewModel? SelectedMod
        {
            get => _selectedMod;
            set => SetProperty(ref _selectedMod, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public double ScanProgress
        {
            get => _scanProgress;
            set => SetProperty(ref _scanProgress, value);
        }

        // 命令
        public ICommand ScanCommand { get; }

        // 業務邏輯
        public async Task ScanModsAsync()
        {
            if (string.IsNullOrEmpty(GamePath))
            {
                StatusText = "錯誤：請先設定遊戲路徑";
                return;
            }

            IsScanning = true;
            StatusText = "正在掃描模組...";
            ScanProgress = 0;

            try
            {
                // 1. 清理舊數據
                foreach (var mod in Mods) mod.Dispose();
                Mods.Clear();
                LocalMods.Clear();

                // 2. 建立進度報告
                var progress = new Progress<ScanProgress>(p =>
                {
                    StatusText = $"正在處理: {p.CurrentMod} ({p.Processed}/{p.Total})";
                    ScanProgress = p.PercentComplete;
                });

                // 3. 執行掃描
                var results = await _modScannerService.ScanModsAsync(GamePath, progress);

                // 4. 轉換為 ViewModel 並加入集合
                foreach (var model in results)
                {
                    var vm = new ModViewModel(model);
                    Mods.Add(vm);
                    if (model.Source == ModSource.Local)
                    {
                        LocalMods.Add(vm);
                    }
                }

                StatusText = $"掃描完成，共找到 {Mods.Count} 個模組";
            }
            catch (Exception ex)
            {
                StatusText = $"掃描失敗: {ex.Message}";
                await _logger.LogErrorAsync("掃描模組失敗", ex);
            }
            finally
            {
                IsScanning = false;
                ScanProgress = 100;
            }
        }
    }
}
