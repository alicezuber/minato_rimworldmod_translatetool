using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using RimWorldTranslationTool.Services.Dialogs;
using RimWorldTranslationTool.Services.Scanning;
using RimWorldTranslationTool.Services.Settings;

namespace RimWorldTranslationTool.ViewModels
{
    public class ModBrowserViewModel : BaseViewModel
    {
        private readonly IModScannerService _modScannerService;
        private readonly ISettingsService _settingsService;
        private readonly IDialogService _dialogService;
        private string _searchText = string.Empty;
        private string _statusText = "準備就緒";
        private ModViewModel? _selectedMod;
        private bool _isScanning;
        private int _scanProgress;
        private string _scanProgressText = string.Empty;

        public ModBrowserViewModel(IModScannerService modScannerService, ISettingsService settingsService, IDialogService dialogService)
        {
            _modScannerService = modScannerService;
            _settingsService = settingsService;
            _dialogService = dialogService;

            Mods = new ObservableCollection<ModViewModel>();
            FilteredModsView = CollectionViewSource.GetDefaultView(Mods);
            FilteredModsView.Filter = FilterMods;

            ScanCommand = new RelayCommandEmpty(async () => await ScanModsAsync());
        }

        public ObservableCollection<ModViewModel> Mods { get; }

        public ICollectionView FilteredModsView { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilteredModsView.Refresh();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ModViewModel? SelectedMod
        {
            get => _selectedMod;
            set => SetProperty(ref _selectedMod, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public int ScanProgress
        {
            get => _scanProgress;
            set => SetProperty(ref _scanProgress, value);
        }

        public string ScanProgressText
        {
            get => _scanProgressText;
            set => SetProperty(ref _scanProgressText, value);
        }

        public string ModCountText => $"共 {Mods.Count} 個模組";

        public ICommand ScanCommand { get; }

        private bool FilterMods(object obj)
        {
            if (obj is not ModViewModel mod)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            var search = SearchText.ToLowerInvariant();
            return (mod.Name?.ToLowerInvariant().Contains(search) ?? false) ||
                   (mod.Author?.ToLowerInvariant().Contains(search) ?? false) ||
                   (mod.PackageId?.ToLowerInvariant().Contains(search) ?? false);
        }

        private async System.Threading.Tasks.Task ScanModsAsync()
        {
            if (IsScanning)
            {
                return;
            }

            var gamePath = _settingsService.GetCurrentSettings().GamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                StatusText = "請先設定遊戲路徑";
                await _dialogService.ShowWarningAsync("請先在設定頁面配置 RimWorld 遊戲路徑", "未設定遊戲路徑");
                return;
            }

            IsScanning = true;
            StatusText = "正在掃描...";
            Mods.Clear();

            try
            {
                var progress = new Progress<Services.Scanning.ScanProgress>(p =>
                {
                    ScanProgress = (int)p.PercentComplete;
                    ScanProgressText = p.CurrentMod ?? string.Empty;
                });

                var mods = await _modScannerService.ScanModsAsync(gamePath, progress);

                foreach (var mod in mods)
                {
                    Mods.Add(new ModViewModel(mod));
                }

                StatusText = $"掃描完成，找到 {Mods.Count} 個模組";
                OnPropertyChanged(nameof(ModCountText));
            }
            catch (Exception ex)
            {
                StatusText = $"掃描失敗: {ex.Message}";
                await _dialogService.ShowErrorAsync($"掃描模組時發生錯誤\n\n{ex.Message}", ex, "掃描失敗");
            }
            finally
            {
                IsScanning = false;
                ScanProgress = 0;
            }
        }
    }
}
