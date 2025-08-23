using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.PlayerService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleEmbeddedAudioEditorViewModel(string title, UndertaleEmbeddedAudio embeddedAudio, IPlayer player, IFileService fileService) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleEmbeddedAudio embeddedAudio = embeddedAudio;

        [RelayCommand]
        private async Task ImportAsync(IStorageProvider storageProvider)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }

            // Use the FileService to get a file path
            var files = await fileService.LoadAudioFileAsync(storageProvider);
            var filePath = files?.FirstOrDefault()?.Path.LocalPath;

            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                
                if (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F')
                {
                    EmbeddedAudio.Data = data;
                }
                else if (data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S')
                {
                    EmbeddedAudio.Data = data;
                }
                else
                    await App.Current!.ShowError("Failed to import audio!\r\nNot a WAV or OGG.", "Audio failure");
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

            var storage = await fileService.SaveAudioFileAsync(storageProvider, "", "*.wav");

            if (storage == null || string.IsNullOrEmpty(storage.Name))
                return; // User cancelled

            try
            {
                File.WriteAllBytes(storage.Path.AbsolutePath, EmbeddedAudio.Data);
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
            }
        }

        private void InitAudio()
        {
            if (player == null)
                return;
            else if (player.IsPlaying)
                player.Stop();
        }

        [RelayCommand]
        private async Task PlayAsync()
        {
            if (EmbeddedAudio.Data.Length > 4)
            {
                try
                {
                    if (EmbeddedAudio.Data[0] == 'R' && EmbeddedAudio.Data[1] == 'I' && EmbeddedAudio.Data[2] == 'F' && EmbeddedAudio.Data[3] == 'F')
                    {
                        InitAudio();
                        player.Play(new MemoryStream(EmbeddedAudio.Data));
                    }
                    else if (EmbeddedAudio.Data[0] == 'O' && EmbeddedAudio.Data[1] == 'g' && EmbeddedAudio.Data[2] == 'g' && EmbeddedAudio.Data[3] == 'S')
                    {
                        InitAudio();
                        player.Play(new MemoryStream(EmbeddedAudio.Data));
                    }
                    else
                        await App.Current!.ShowError("Failed to play audio!\r\nNot a WAV or OGG.", "Audio failure");
                }
                catch (Exception ex)
                {
                    await App.Current!.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                }
            }
        }


        [RelayCommand]
        private void Stop()
        {
            if (player != null)
                player.Stop();
        }
    }
}
