using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageMagick;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleTexturePageItemEditorViewModel(string title, UndertaleTexturePageItem texturePageItem, EditorViewModel editor, ITextureCacheService textureCacheService, IFileService fileService) : EditorContentViewModel(title) 
    {
        [ObservableProperty]
        private UndertaleTexturePageItem texturePageItem = texturePageItem;

        public int TexturePageWidth => TexturePageItem?.TexturePage?.TextureData?.Width ?? 100000;
        public int TexturePageHeight => TexturePageItem?.TexturePage?.TextureData?.Height ?? 100000;

        partial void OnTexturePageItemChanged(UndertaleTexturePageItem value)
        {
            OnPropertyChanged(nameof(TexturePageWidth));
            OnPropertyChanged(nameof(TexturePageHeight));
        }

        public RectangleGeometry SourceClipGeometry => new RectangleGeometry
        {
            Rect = new Rect(SourceX, SourceY, SourceWidth, SourceHeight)
        };

        public ushort SourceX
        {
            get => TexturePageItem.SourceX;
            set
            {
                TexturePageItem.SourceX = value;
                OnPropertyChanged(nameof(TexturePageItem));
                OnPropertyChanged(nameof(SourceX));
                OnPropertyChanged(nameof(SourceClipGeometry));
            }
        }

        public ushort SourceY
        {
            get => TexturePageItem.SourceY;
            set
            {
                TexturePageItem.SourceY = value;
                OnPropertyChanged(nameof(TexturePageItem));
                OnPropertyChanged(nameof(SourceY));
                OnPropertyChanged(nameof(SourceClipGeometry));
            }
        }

        public ushort SourceWidth
        {
            get => TexturePageItem.SourceWidth;
            set
            {
                TexturePageItem.SourceWidth = value;
                OnPropertyChanged(nameof(TexturePageItem));
                OnPropertyChanged(nameof(SourceWidth));
                OnPropertyChanged(nameof(SourceClipGeometry));
            }
        }

        public ushort SourceHeight
        {
            get => TexturePageItem.SourceHeight;
            set
            {
                TexturePageItem.SourceHeight = value;
                OnPropertyChanged(nameof(TexturePageItem));
                OnPropertyChanged(nameof(SourceHeight));
                OnPropertyChanged(nameof(SourceClipGeometry));
            }
        }

        public ushort TargetX
        {
            get => TexturePageItem.TargetX;
            set
            {
                TexturePageItem.TargetX = value;
                OnPropertyChanged(nameof(TargetX));
            }
        }

        public ushort TargetY
        {
            get => TexturePageItem.TargetY;
            set
            {
                TexturePageItem.TargetY = value;
                OnPropertyChanged(nameof(TargetY));
            }
        }

        public ushort TargetWidth
        {
            get => TexturePageItem.TargetWidth;
            set
            {
                TexturePageItem.TargetWidth = value;
                OnPropertyChanged(nameof(TargetWidth));
            }
        }

        public ushort TargetHeight
        {
            get => TexturePageItem.TargetHeight;
            set
            {
                TexturePageItem.TargetHeight = value;
                OnPropertyChanged(nameof(TargetHeight));
            }
        }

        public ushort BoundingWidth
        {
            get => TexturePageItem.BoundingWidth;
            set
            {
                TexturePageItem.BoundingWidth = value;
                OnPropertyChanged(nameof(BoundingWidth));
            }
        }

        public ushort BoundingHeight
        {
            get => TexturePageItem.BoundingHeight;
            set
            {
                TexturePageItem.BoundingHeight = value;
                OnPropertyChanged(nameof(BoundingHeight));
            }
        }

        [ObservableProperty]
        private EditorViewModel editor = editor;

        [ObservableProperty]
        private ITextureCacheService textureCacheService = textureCacheService;

        /// <summary>
        /// Handle on the texture page item we're listening for updates from.
        /// </summary>
        [ObservableProperty]
        private UndertaleTexturePageItem textureItemContext = null;

        /// <summary>
        /// Handle on the texture data where we're listening for updates from.
        /// </summary>
        [ObservableProperty]
        private UndertaleEmbeddedTexture.TexData textureDataContext = null;

        [RelayCommand]
        private async Task ImportAsync(IStorageProvider storageProvider)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }

            // Use the FileService to get a file path
            var files = await fileService.LoadImageFileAsync(storageProvider);
            var filePath = files?.FirstOrDefault()?.Path.LocalPath;

            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled

            try
            {
                using MagickImage image = TextureWorker.ReadBGRAImageFromFile(filePath);
                TexturePageItem.ReplaceTexture(image);
                OnPropertyChanged(nameof(TexturePageItem));
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError(ex.Message, "Failed to import image");
            }
        }

        [RelayCommand]
        private async Task ExportAsync(IStorageProvider storageProvider)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }

            var storage = await fileService.SaveImageFileAsync(storageProvider);

            if (storage == null || string.IsNullOrEmpty(storage.Name))
                return; // User cancelled

            using TextureWorker worker = new();
            try
            {
                worker.ExportAsPNG(TexturePageItem, storage.Path.AbsolutePath);
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
            }
        }
    }
}
