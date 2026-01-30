using System;
using System.ComponentModel;

namespace RimWorldTranslationTool.Models
{
    /// <summary>
    /// 設定狀態模型 - 用於 UI 狀態管理
    /// </summary>
    public class SettingsState : INotifyPropertyChanged
    {
        private AppSettings _currentSettings = new AppSettings();
        private bool _isLoading = false;
        private string _lastSaveTime = "";
        private bool _hasUnsavedChanges = false;
        
        public AppSettings CurrentSettings
        {
            get => _currentSettings;
            set
            {
                if (_currentSettings != value)
                {
                    _currentSettings = value;
                    OnPropertyChanged(nameof(CurrentSettings));
                    OnPropertyChanged(nameof(IsGamePathSet));
                    OnPropertyChanged(nameof(IsModsConfigSet));
                }
            }
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }
        
        public string LastSaveTime
        {
            get => _lastSaveTime;
            set
            {
                if (_lastSaveTime != value)
                {
                    _lastSaveTime = value;
                    OnPropertyChanged(nameof(LastSaveTime));
                }
            }
        }
        
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged(nameof(HasUnsavedChanges));
                }
            }
        }
        
        /// <summary>
        /// 遊戲路徑是否已設定
        /// </summary>
        public bool IsGamePathSet => !string.IsNullOrEmpty(_currentSettings.GamePath);
        
        /// <summary>
        /// ModsConfig 路徑是否已設定
        /// </summary>
        public bool IsModsConfigSet => !string.IsNullOrEmpty(_currentSettings.ModsConfigPath);
        
        /// <summary>
        /// 設定是否完整
        /// </summary>
        public bool IsConfigurationComplete => IsGamePathSet && IsModsConfigSet;
        
        /// <summary>
        /// 標記為已修改
        /// </summary>
        public void MarkAsModified()
        {
            HasUnsavedChanges = true;
        }
        
        /// <summary>
        /// 標記為已保存
        /// </summary>
        public void MarkAsSaved()
        {
            HasUnsavedChanges = false;
            LastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        /// <summary>
        /// 重設狀態
        /// </summary>
        public void Reset()
        {
            CurrentSettings = new AppSettings();
            HasUnsavedChanges = false;
            LastSaveTime = "";
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
