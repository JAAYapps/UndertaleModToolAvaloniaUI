#nullable enable
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.FileService
{
    public class FileService : IFileService
    {
        public async Task<IStorageFile?> SaveFileAsync(IStorageProvider storageProvider)
        {
            return await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Save Text File",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Wim (*.wim)") { Patterns = new List<string> { "*.win" } },
                    new FilePickerFileType("Wav (*.wav)") { Patterns = new List<string> { "*.wav" } },
                    new FilePickerFileType("Ogg (*.ogg)") { Patterns = new List<string> { "*.ogg" } },
                    new FilePickerFileType("Mp3 (*.mp3)") { Patterns = new List<string> { "*.mp3" } }
                },
                ShowOverwritePrompt = true,
                SuggestedFileName = "Audio",
                DefaultExtension = "*.wav"
            });
        }
        
        public async Task<IReadOnlyList<IStorageFile>> LoadFileAsync(IStorageProvider storageProvider)
        {
            return await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                Title = "Load reference audio File",
                FileTypeFilter = [
                    new FilePickerFileType("Wim (*.wim)") { Patterns = new List<string> { "*.win" } },
                    new FilePickerFileType("Wav (*.wav)") { Patterns = new List<string> { "*.wav" } },
                    new FilePickerFileType("Ogg (*.ogg)") { Patterns = new List<string> { "*.ogg" } },
                    new FilePickerFileType("Mp3 (*.mp3)") { Patterns = new List<string> { "*.mp3" } }
                ],
                SuggestedFileName = "",
                SuggestedStartLocation = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Music)//Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            });
        }
    }
}
