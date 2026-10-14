#nullable enable
using System.Collections.Generic;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.FileService
{
    public interface IFileService
    {
        Task<IStorageFile?> SaveFileAsync(IStorageProvider storageProvider, string path = "", string defaultExtension = "*.win");

        Task<IStorageFile?> SaveAudioFileAsync(IStorageProvider storageProvider, string path = "", string defaultExtension = "*.ogg");

        Task<IStorageFile?> SaveTextFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IStorageFile?> SaveImageFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IReadOnlyList<IStorageFile>> LoadFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IReadOnlyList<IStorageFile>> LoadAudioFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IReadOnlyList<IStorageFile>> LoadTextFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IReadOnlyList<IStorageFile>> LoadImageFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IReadOnlyList<IStorageFile>> LoadScriptFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IReadOnlyList<IStorageFile>> LoadFilteredFileAsync(IStorageProvider storageProvider, string filter, string path = "", string title = "Load File");

        Task<IStorageFile?> SaveFilteredFileAsync(IStorageProvider storageProvider, string defaultExtension, string filter, string path = "", string title = "Save File");

        Task<IReadOnlyList<IStorageFolder>> LoadFolderAsync(IStorageProvider storageProvider, string path = "", string title = "Choose Folder");
    }
}
