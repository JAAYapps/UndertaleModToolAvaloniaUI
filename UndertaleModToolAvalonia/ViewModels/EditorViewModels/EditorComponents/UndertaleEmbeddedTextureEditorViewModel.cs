using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleEmbeddedTextureEditorViewModel(string title, UndertaleEmbeddedTexture embeddedTexture, EditorViewModel editor, IFileService fileService, ITextureCacheService textureCacheService) : EditorContentViewModel(title)
    {
        public ITextureCacheService textureCacheService = textureCacheService;

        public EditorViewModel editorViewModel = editor;

        [ObservableProperty]
        UndertaleEmbeddedTexture embeddedTexture = embeddedTexture;

        [ObservableProperty]
        private UndertaleTexturePageItem[] items;

        [ObservableProperty]
        private UndertaleTexturePageItem hoveredItem;

        /// <summary>
        /// Handle on the texture data where we're listening for updates from.
        /// </summary>
        [ObservableProperty]
        private UndertaleEmbeddedTexture.TexData textureDataContext = null;

        [ObservableProperty]
        private (Transform Transform, double Left, double Top) overriddenPreviewState;

        [RelayCommand]
        private async Task ImportAsync(IStorageProvider storageProvider)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }

            // Use the FileService to get a file path
            var files = await fileService.LoadFileAsync(storageProvider);
            var filePath = files?.FirstOrDefault()?.Path.LocalPath;

            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled

            try
            {
                GMImage image;
                if (System.IO.Path.GetExtension(filePath).Equals(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Import PNG data verbatim, without attempting to modify it
                    image = GMImage.FromPng(File.ReadAllBytes(filePath), true)
                                   .ConvertToFormat(EmbeddedTexture.TextureData.Image?.Format ?? GMImage.ImageFormat.Png);
                }
                else
                {
                    // Import any file type
                    using var magickImage = new MagickImage(filePath);
                    magickImage.Format = MagickFormat.Bgra;
                    magickImage.Alpha(AlphaOption.Set);
                    magickImage.SetCompression(CompressionMethod.NoCompression);

                    // Import image
                    image = GMImage.FromMagickImage(magickImage)
                                   .ConvertToFormat(EmbeddedTexture.TextureData.Image?.Format ?? GMImage.ImageFormat.Png);
                }

                // Check dimensions
                uint width = (uint)image.Width, height = (uint)image.Height;
                if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
                {
                    await App.Current!.ShowWarning("WARNING: Texture page dimensions are not powers of 2. Sprite blurring is very likely in-game.", "Unexpected texture dimensions");
                }

                // Import image
                EmbeddedTexture.TextureData.Image = image;
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
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
            
            var storage = await fileService.SaveFileAsync(storageProvider);

            if (storage == null || string.IsNullOrEmpty(storage.Name))
                return; // User cancelled

            try
            {
                using FileStream fs = new(storage.Path.AbsolutePath, FileMode.Create);
                EmbeddedTexture.TextureData.Image.SavePng(fs);
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
            }
        }
    }
}
