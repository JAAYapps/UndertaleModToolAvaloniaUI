using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.PlayerService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleSoundEditorViewModel(string title, UndertaleSound sound, IPlayer player) : EditorContentViewModel(title)
    {
        private UndertaleData audioGroupData;
        private string loadedPath;

        [ObservableProperty]
        private UndertaleSound sound = sound;

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
            if ((Sound.Flags & UndertaleSound.AudioEntryFlags.IsEmbedded) != UndertaleSound.AudioEntryFlags.IsEmbedded &&
                (Sound.Flags & UndertaleSound.AudioEntryFlags.IsCompressed) != UndertaleSound.AudioEntryFlags.IsCompressed)
            {
                try
                {
                    string filename;
                    if (!Sound.File.Content.Contains("."))
                        filename = Sound.File.Content + ".ogg";
                    else
                        filename = Sound.File.Content;
                    string audioPath = Path.Combine(Path.GetDirectoryName(AppConstants.FilePath)!, filename);
                    if (File.Exists(audioPath))
                    {
                        InitAudio();
                        player?.Play(File.OpenRead(audioPath));
                    }
                    else
                        throw new Exception("Failed to find audio file.");
                }
                catch (Exception ex)
                {
                    await App.Current!.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                }
                return;
            }

            UndertaleEmbeddedAudio target;

            if (Sound.GroupID != 0 && Sound.AudioID != -1)
            {
                try
                {
                    string relativePath;
                    if (Sound.AudioGroup is UndertaleAudioGroup { Path.Content: string customRelativePath })
                    {
                        relativePath = customRelativePath;
                    }
                    else
                    {
                        relativePath = $"audiogroup{Sound.GroupID}.dat";
                    }
                    string path = Path.Combine(Path.GetDirectoryName(AppConstants.FilePath)!, relativePath);
                    if (File.Exists(path))
                    {
                        if (loadedPath != path)
                        {
                            loadedPath = path;

                            using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
                            audioGroupData = UndertaleIO.Read(stream, (warning, _) =>
                            {
                                throw new Exception(warning);
                            });
                        }

                        target = audioGroupData.EmbeddedAudio[Sound.AudioID];
                    }
                    else
                        throw new Exception("Failed to find audio group file.");
                }
                catch (Exception ex)
                {
                    await App.Current!.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                    return;
                }
            }
            else
                target = Sound.AudioFile;

            if (target != null)
            {
                if (target.Data.Length > 4)
                {
                    try
                    {
                        InitAudio();
                        player?.Play(new MemoryStream(target.Data));
                    }
                    catch (Exception ex)
                    {
                        await App.Current!.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                    }
                }
            }
            else
                await App.Current!.ShowError("Failed to play audio!\r\nNo options for playback worked.", "Audio failure");
        }

        [RelayCommand]
        private void Stop()
        {
            if (player != null)
                player.Stop();
        }
    }
}
