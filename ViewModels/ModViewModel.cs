using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RimWorldTranslationTool.Models;

namespace RimWorldTranslationTool.ViewModels
{
    /// <summary>
    /// 模組的 ViewModel，負責 UI 顯示邏輯
    /// </summary>
    public class ModViewModel : BaseViewModel, IDisposable
    {
        private readonly ModModel _model;
        private BitmapImage? _previewImage;

        public ModViewModel(ModModel model)
        {
            _model = model;
            LoadPreviewImage();
        }

        public ModModel Model => _model;

        // 代理屬性
        public string Name => _model.Name;
        public string Author => _model.Author;
        public string PackageId => _model.PackageId;
        public string FolderName => _model.FolderName;
        public string SupportedVersions => _model.SupportedVersions;
        public string HasChineseTraditional => _model.HasChineseTraditional;
        public string HasChineseSimplified => _model.HasChineseSimplified;
        public string HasTranslationPatch => _model.HasTranslationPatch;
        public string CanTranslate => _model.CanTranslate;
        public bool IsVersionCompatible => _model.IsVersionCompatible;
        
        public bool IsEnabled
        {
            get => _model.IsEnabled;
            set
            {
                if (_model.IsEnabled != value)
                {
                    _model.IsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        // UI 專用屬性
        public BitmapImage? PreviewImage
        {
            get => _previewImage;
            set => SetProperty(ref _previewImage, value);
        }

        public Brush HasChineseTraditionalColor => GetStatusColor(_model.HasChineseTraditional);
        public Brush HasChineseSimplifiedColor => GetStatusColor(_model.HasChineseSimplified);
        public Brush HasTranslationPatchColor => GetStatusColor(_model.HasTranslationPatch);
        public Brush CanTranslateColor => GetStatusColor(_model.CanTranslate);
        
        public Brush VersionCompatibilityColor => _model.IsVersionCompatible ? 
            new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)) : new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));

        public Brush VersionCompatibilityBackground => _model.IsVersionCompatible ? 
            new SolidColorBrush(Colors.Transparent) : new SolidColorBrush(Color.FromArgb(50, 255, 255, 0));

        public Brush HasChineseTraditionalBackground => GetStatusBackground(_model.HasChineseTraditional);
        public Brush HasChineseSimplifiedBackground => GetStatusBackground(_model.HasChineseSimplified);
        public Brush HasTranslationPatchBackground => GetStatusBackground(_model.HasTranslationPatch);
        public Brush CanTranslateBackground => GetStatusBackground(_model.CanTranslate);

        private void LoadPreviewImage()
        {
            if (string.IsNullOrEmpty(_model.PreviewImagePath) || !System.IO.File.Exists(_model.PreviewImagePath))
                return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_model.PreviewImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewImage = bitmap;
            }
            catch
            {
                // 忽略圖片載入錯誤
            }
        }

        private Brush GetStatusColor(string status)
        {
            return status switch
            {
                "有" or "是" => new SolidColorBrush(Color.FromArgb(128, 0, 128, 0)),
                "無" or "否" => new SolidColorBrush(Color.FromArgb(128, 128, 0, 0)),
                _ => new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
            };
        }

        private Brush GetStatusBackground(string status)
        {
            return status switch
            {
                "有" or "是" => new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)),
                "無" or "否" => new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }

        public void Dispose()
        {
            _previewImage = null;
        }
    }
}
