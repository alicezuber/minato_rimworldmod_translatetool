using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using RimWorldTranslationTool.Services.Settings;

namespace RimWorldTranslationTool.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private string _gamePath = string.Empty;
        private string _modsConfigPath = string.Empty;
        private string _gamePathStatus = string.Empty;
        private bool _isGamePathValid;
        private string? _selectedLanguage;
        private string? _selectedVersion;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            AvailableLanguages = new ObservableCollection<string> { "繁體中文", "简体中文", "English" };
            AvailableVersions = new ObservableCollection<string> { "1.5", "1.4", "1.3", "1.2", "1.1", "1.0" };

            BrowseGamePathCommand = new RelayCommandEmpty(BrowseGamePath);
            AutoDetectCommand = new RelayCommandEmpty(AutoDetect);
            SelectModsConfigCommand = new RelayCommandEmpty(SelectModsConfig);
            SaveSettingsCommand = new RelayCommandEmpty(SaveSettings);
            ResetSettingsCommand = new RelayCommandEmpty(ResetSettings);

            LoadSettings();
        }

        public string GamePath
        {
            get => _gamePath;
            set
            {
                if (SetProperty(ref _gamePath, value))
                {
                    ValidateGamePath();
                }
            }
        }

        public string ModsConfigPath
        {
            get => _modsConfigPath;
            set => SetProperty(ref _modsConfigPath, value);
        }

        public string GamePathStatus
        {
            get => _gamePathStatus;
            set => SetProperty(ref _gamePathStatus, value);
        }

        public bool IsGamePathValid
        {
            get => _isGamePathValid;
            set => SetProperty(ref _isGamePathValid, value);
        }

        public string? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    ApplyLanguage();
                }
            }
        }

        public string? SelectedVersion
        {
            get => _selectedVersion;
            set => SetProperty(ref _selectedVersion, value);
        }

        public ObservableCollection<string> AvailableLanguages { get; }
        public ObservableCollection<string> AvailableVersions { get; }

        public ICommand BrowseGamePathCommand { get; }
        public ICommand AutoDetectCommand { get; }
        public ICommand SelectModsConfigCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetSettingsCommand { get; }

        private void LoadSettings()
        {
            var settings = _settingsService.GetCurrentSettings();
            GamePath = settings.GamePath ?? string.Empty;
            ModsConfigPath = settings.ModsConfigPath ?? string.Empty;
            SelectedLanguage = settings.Language ?? "繁體中文";
            SelectedVersion = settings.GameVersion ?? "1.5";
            ValidateGamePath();
        }

        private void ValidateGamePath()
        {
            if (string.IsNullOrWhiteSpace(GamePath))
            {
                IsGamePathValid = false;
                GamePathStatus = "請設定遊戲路徑";
                return;
            }

            var exePath = Path.Combine(GamePath, "RimWorldWin64.exe");
            if (Directory.Exists(GamePath) && File.Exists(exePath))
            {
                IsGamePathValid = true;
                GamePathStatus = "✓ 已找到有效的 RimWorld 安裝";
            }
            else if (Directory.Exists(GamePath))
            {
                IsGamePathValid = false;
                GamePathStatus = "⚠ 資料夾存在但找不到 RimWorld 執行檔";
            }
            else
            {
                IsGamePathValid = false;
                GamePathStatus = "✗ 路徑不存在";
            }
        }

        private void BrowseGamePath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "選擇 RimWorld 安裝目錄"
            };

            if (dialog.ShowDialog() == true)
            {
                GamePath = dialog.FolderName;
            }
        }

        private void AutoDetect()
        {
            // 嘗試常見的 Steam 路徑
            var steamPaths = new[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld",
                @"C:\Program Files\Steam\steamapps\common\RimWorld",
                @"D:\Steam\steamapps\common\RimWorld",
                @"D:\SteamLibrary\steamapps\common\RimWorld",
                @"E:\Steam\steamapps\common\RimWorld",
                @"E:\SteamLibrary\steamapps\common\RimWorld"
            };

            foreach (var path in steamPaths)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "RimWorldWin64.exe")))
                {
                    GamePath = path;
                    break;
                }
            }

            // 自動偵測 ModsConfig.xml
            var localLow = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = Path.Combine(localLow, "..", "LocalLow", "Ludeon Studios", "RimWorld by Ludeon Studios", "Config", "ModsConfig.xml");
            if (File.Exists(configPath))
            {
                ModsConfigPath = Path.GetFullPath(configPath);
            }
        }

        private void SelectModsConfig()
        {
            var dialog = new OpenFileDialog
            {
                Title = "選擇 ModsConfig.xml",
                Filter = "XML 檔案|*.xml|所有檔案|*.*",
                FileName = "ModsConfig.xml"
            };

            if (dialog.ShowDialog() == true)
            {
                ModsConfigPath = dialog.FileName;
            }
        }

        private void ApplyLanguage()
        {
            var langCode = SelectedLanguage switch
            {
                "繁體中文" => "zh-TW",
                "简体中文" => "zh-CN",
                "English" => "en-US",
                _ => "zh-TW"
            };
            Services.Localization.LocalizationService.Instance.SetLanguage(langCode);
        }

        private async void SaveSettings()
        {
            _settingsService.UpdateSetting(settings =>
            {
                settings.GamePath = GamePath;
                settings.ModsConfigPath = ModsConfigPath;
                settings.Language = SelectedLanguage;
                settings.GameVersion = SelectedVersion;
            });

            var settings = _settingsService.GetCurrentSettings();
            await _settingsService.SaveSettingsAsync(settings);
        }

        private void ResetSettings()
        {
            GamePath = string.Empty;
            ModsConfigPath = string.Empty;
            SelectedLanguage = "繁體中文";
            SelectedVersion = "1.5";
        }
    }
}
