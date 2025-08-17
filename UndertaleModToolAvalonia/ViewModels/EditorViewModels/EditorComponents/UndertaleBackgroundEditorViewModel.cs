using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleBackgroundEditorViewModel(string title, UndertaleBackground undertaleBackground, EditorViewModel editor, ITextureCacheService textureCacheService, IFileService fileService) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleBackground undertaleBackground = undertaleBackground;

        [ObservableProperty]
        private EditorViewModel editor = editor;

        [ObservableProperty]
        private ITextureCacheService textureCacheService = textureCacheService;

        [ObservableProperty]
        private IFileService fileService = fileService;
    }
}
