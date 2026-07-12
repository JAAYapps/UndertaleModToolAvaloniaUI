using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Services.ProfileService
{
    public interface IProfileService
    {
        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null, IDecompileSettings settings = null);

        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null, IDecompileSettings settings = null);

        public string GetDisassemblyText(UndertaleCode code);

        public string GetDisassemblyText(string codeName);

        public Task CrashCheckAsync();

        public Task ApplyCorrectionsAsync();

        public Task CreateUMTLastEditedAsync(IStorageFolder folder);

        public Task DestroyUMTLastEditedAsync();

        public Task RevertProfileAsync();

        public Task UpdateProfileAsync(UndertaleData data, IStorageFolder folder);

        public Task ProfileSaveEventAsync(UndertaleData data, string filename);

        public Task DirectoryCopyAsync(string sourceDirName, string destDirName, bool copySubDirs);
    }
}
