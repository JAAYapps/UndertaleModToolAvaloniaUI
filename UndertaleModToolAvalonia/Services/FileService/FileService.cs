#nullable enable
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UndertaleModToolAvalonia.Services.FileService
{
    public class FileService : IFileService
    {
        public async Task<IStorageFile?> SaveFileAsync(IStorageProvider storageProvider, string path = "")
        {
            string directory = !string.IsNullOrEmpty(path) ? Path.GetFullPath(path) : "";
            string name = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "";
            return await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Save Undertale File",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("GameMaker data files (.win, .unx, .ios, .droid)") { Patterns = new List<string> { "*.win;*.unx;*.ios;*.droid" } }
                },
                ShowOverwritePrompt = true,
                SuggestedFileName = !string.IsNullOrEmpty(name) ? name : "data.win",
                DefaultExtension = "*.win",
                SuggestedStartLocation = !string.IsNullOrEmpty(directory) ? await storageProvider.TryGetFolderFromPathAsync(directory) : await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
            });
        }

        public async Task<IStorageFile?> SaveTextFileAsync(IStorageProvider storageProvider, string path = "")
        {
            string directory = !string.IsNullOrEmpty(path) ? Path.GetFullPath(path) : "";
            string name = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "";
            return await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Save Text File",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Text files (.txt)") { Patterns = new List<string> { "*.txt" } },
                    new FilePickerFileType("All files") { Patterns = new List<string> { "*" } }
                },
                ShowOverwritePrompt = true,
                SuggestedFileName = !string.IsNullOrEmpty(name) ? name : "data.txt",
                DefaultExtension = "*.txt",
                SuggestedStartLocation = !string.IsNullOrEmpty(directory) ? await storageProvider.TryGetFolderFromPathAsync(directory) : await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
            });
        }

        public async Task<IReadOnlyList<IStorageFile>> LoadFileAsync(IStorageProvider storageProvider, string path = "")
        {
            string directory = !string.IsNullOrEmpty(path) ? Path.GetFullPath(path) : "";
            string name = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "";
            return await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                Title = "Load Undertale File",
                FileTypeFilter = [
                    new FilePickerFileType("GameMaker data files (.win, .unx, .ios, .droid)") { Patterns = new List<string> { "*.win;*.unx;*.ios;*.droid" } },
                    new FilePickerFileType("All files") { Patterns = new List<string> { "*" } }
                ],
                SuggestedFileName = !string.IsNullOrEmpty(name) ? name : "data.win",
                SuggestedStartLocation = !string.IsNullOrEmpty(directory) ? await storageProvider.TryGetFolderFromPathAsync(directory) : await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
            });
        }

        public async Task<IReadOnlyList<IStorageFile>> LoadTextFileAsync(IStorageProvider storageProvider, string path = "")
        {
            string directory = !string.IsNullOrEmpty(path) ? Path.GetFullPath(path) : "";
            string name = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "";
            return await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                Title = "Load Text File",
                FileTypeFilter = [
                    new FilePickerFileType("Text files (.txt)") { Patterns = new List<string> { "*.txt" } },
                    new FilePickerFileType("All files") { Patterns = new List<string> { "*" } }
                ],
                SuggestedFileName = !string.IsNullOrEmpty(name) ? name : "data.txt",
                SuggestedStartLocation = !string.IsNullOrEmpty(directory) ? await storageProvider.TryGetFolderFromPathAsync(directory) : await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
            });
        }
    }
}
