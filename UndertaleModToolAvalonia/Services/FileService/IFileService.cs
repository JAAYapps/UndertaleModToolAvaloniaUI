#nullable enable
using System.Collections.Generic;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.FileService
{
    public interface IFileService
    {
        Task<IStorageFile?> SaveFileAsync(IStorageProvider storageProvider);
        
        Task<IReadOnlyList<IStorageFile>> LoadFileAsync(IStorageProvider storageProvider);
    }
}
