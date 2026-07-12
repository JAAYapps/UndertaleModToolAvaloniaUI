using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel
{
    public partial class UndertaleRoomEditorViewModel(string title, UndertaleRoom room, EditorViewModel editorViewModel, IFileService fileService, ITextureCacheService textureCacheService) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        UndertaleRoom room = room;
        
    }
}
