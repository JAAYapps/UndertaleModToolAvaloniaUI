#nullable enable
using System.Collections.Generic;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.FileService
{
    public interface IFileService
    {
        Task<IStorageFile?> SaveFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IStorageFile?> SaveTextFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IReadOnlyList<IStorageFile>> LoadFileAsync(IStorageProvider storageProvider, string path = "");

        Task<IReadOnlyList<IStorageFile>> LoadTextFileAsync(IStorageProvider storageProvider, string path = "");
    }
}
