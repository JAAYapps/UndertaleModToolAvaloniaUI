using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Differencing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;
using static UndertaleModLib.Models.UndertaleSprite;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleSpriteEditorViewModel(string title, UndertaleSprite undertaleSprite, EditorViewModel editor, IFileService fileService, ITextureCacheService textureCacheService) : EditorContentViewModel(title)
    {
        public EditorViewModel Editor { get; } = editor;
        public ITextureCacheService TextureCacheService { get; } = textureCacheService;
        public IFileService FileService { get; } = fileService;

        [ObservableProperty]
        private UndertaleSprite undertaleSprite = undertaleSprite;

        [ObservableProperty]
        private TextureEntry selectedItem;

        [ObservableProperty]
        private MaskEntry selectedMaskItem;

        [ObservableProperty]
        private UndertaleTexturePageItem selectedTexture;

        [ObservableProperty]
        private UndertaleTexturePageItem selectedMask;

        partial void OnSelectedItemChanged(TextureEntry value)
        {
            if (value != null)
            {
                SelectedTexture = value.Texture;
                OnPropertyChanged(nameof(SelectedTexture));
            }
            else
            {
                SelectedTexture = null;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        partial void OnSelectedMaskItemChanged(MaskEntry value)
        {
            
        }

        [RelayCommand]
        private void RemoveSprite(TextureEntry entry)
        {
            if (entry is null) return;

            UndertaleSprite.Textures.Remove(entry);

            if (SelectedItem == entry || !UndertaleSprite.Textures.Any())
            {
                SelectedTexture = null;
            }
        }

        [RelayCommand]
        private void RemoveMask(MaskEntry entry)
        {
            if (entry is null) return;
            
            UndertaleSprite.CollisionMasks.Remove(entry);
        }

        [RelayCommand]
        private void AddEntry()
        {
            UndertaleSprite.Textures.Add(new TextureEntry());
        }

        [RelayCommand]
        private void AddMaskEntry()
        {
            UndertaleSprite.NewMaskEntry(AppConstants.Data);
        }

        [RelayCommand]
        private void Import()
        {
            
        }

        [RelayCommand]
        private async Task Export(IStorageProvider storageProvider)
        {
            if (UndertaleSprite.IsSpineSprite)
            {
                await ExportAllSpine(storageProvider, UndertaleSprite);
                if (UndertaleSprite.SpineHasTextureData)
                    return;
            }

            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }

            var storage = await fileService.SaveImageFileAsync(storageProvider);

            if (storage == null || string.IsNullOrEmpty(storage.Name))
                return; // User cancelled

            try
            {
                bool includePadding = (await App.Current!.ShowQuestion("Include padding?") == MsBox.Avalonia.Enums.ButtonResult.Yes);

                using TextureWorker worker = new();
                if (UndertaleSprite.Textures.Count > 1)
                {
                    string dir = Path.GetDirectoryName(storage.Path.AbsolutePath) ?? throw new Exception("No path given by system.");
                    string name = Path.GetFileNameWithoutExtension(storage.Path.AbsolutePath);
                    string path = Path.Combine(dir, name);
                    string ext = Path.GetExtension(storage.Path.AbsolutePath);

                    Directory.CreateDirectory(path);
                    foreach (var tex in UndertaleSprite.Textures.Select((tex, id) => new { id, tex }))
                    {
                        try
                        {
                            worker.ExportAsPNG(tex.tex.Texture, Path.Combine(path, UndertaleSprite.Name.Content + "_" + tex.id + ext), null, includePadding);
                        }
                        catch (Exception ex)
                        {
                            await App.Current!.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                        }
                    }
                }
                else if (UndertaleSprite.Textures.Count == 1)
                {
                    try
                    {
                        worker.ExportAsPNG(UndertaleSprite.Textures[0].Texture, storage.Path.AbsolutePath, null, includePadding);
                    }
                    catch (Exception ex)
                    {
                        await App.Current!.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                    }
                }
                else
                {
                    await App.Current!.ShowError("No frames to export", "Failed to export sprite");
                }
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Failed to export: " + ex.Message, "Failed to export sprite");
            }
        }

        private async Task ExportAllSpine(IStorageProvider storageProvider, UndertaleSprite sprite)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }

            var storage = await fileService.SaveImageFileAsync(storageProvider);

            if (storage == null || string.IsNullOrEmpty(storage.Name))
                return; // User cancelled

            await App.Current!.ShowWarning("This seems to be a Spine sprite, .json and .atlas files will be exported together with the frames. " +
                                 "PLEASE EDIT THEM CAREFULLY! SOME MANUAL EDITING OF THE JSON MAY BE REQUIRED! THE DATA IS EXPORTED AS-IS.", "Spine warning");

            try
            {
                string dir = Path.GetDirectoryName(storage.Path.AbsolutePath) ?? throw new Exception("No path was given by system.");
                string name = Path.GetFileNameWithoutExtension(storage.Path.AbsolutePath);
                string path = Path.Combine(dir, name);
                string ext = Path.GetExtension(storage.Path.AbsolutePath);

                if (sprite.SpineTextures.Count > 0)
                {
                    Directory.CreateDirectory(path);

                    // textures
                    if (sprite.SpineHasTextureData)
                    {
                        foreach (var tex in sprite.SpineTextures.Select((tex, id) => new { id, tex }))
                        {
                            try
                            {
                                File.WriteAllBytes(Path.Combine(path, tex.id + ext), tex.tex.TexBlob);
                            }
                            catch (Exception ex)
                            {
                                await App.Current!.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                            }
                        }
                    }

                    // json and atlas
                    File.WriteAllText(Path.Combine(path, "spine.json"), sprite.SpineJSON);
                    File.WriteAllText(Path.Combine(path, "spine.atlas"), sprite.SpineAtlas);
                }
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Failed to export: " + ex.Message, "Failed to export sprite");
            }
        }

        [RelayCommand]
        private async Task MaskImportAsync(IStorageProvider storageProvider)
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
                (int maskWidth, int maskHeight) = UndertaleSprite.CalculateMaskDimensions(AppConstants.Data);
                SelectedMaskItem.Data = TextureWorker.ReadMaskData(filePath, maskWidth, maskHeight);
                SelectedMaskItem.Width = maskWidth;
                SelectedMaskItem.Height = maskHeight;
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
            }
        }

        [RelayCommand]
        private async Task MaskExportAsync(IStorageProvider storageProvider)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }

            var storage = await fileService.SaveImageFileAsync(storageProvider);

            if (storage == null || string.IsNullOrEmpty(storage.Name))
                return; // User cancelled

            try
            {
                (int maskWidth, int maskHeight) = UndertaleSprite.CalculateMaskDimensions(AppConstants.Data);
                TextureWorker.ExportCollisionMaskPNG(SelectedMaskItem, storage.Path.AbsolutePath, maskWidth, maskHeight);
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
            }
        }
    }
}
