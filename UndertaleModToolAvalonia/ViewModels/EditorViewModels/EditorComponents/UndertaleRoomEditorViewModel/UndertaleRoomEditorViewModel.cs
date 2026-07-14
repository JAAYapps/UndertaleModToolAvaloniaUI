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
    // Step 1: state skeleton only. No rendering, no commands yet.
    // Goal for this step: the object tree binds, and selecting a node
    // updates SelectedObject (and SelectedLayer when the node is a Layer).
    //
    // Ctor signature is unchanged from the stub, so the call site in
    // EditorViewModel.cs (UndertaleRoom => new UndertaleRoomEditorViewModel(...))
    // keeps compiling. Only the param name editorViewModel -> editor changed,
    // which is safe because that call is positional.
    public partial class UndertaleRoomEditorViewModel(string title, UndertaleRoom room, EditorViewModel editor, IFileService fileService, ITextureCacheService textureCacheService) : EditorContentViewModel(title)
    {
        // Injected services, exposed the same way UndertaleSpriteEditorViewModel does.
        public EditorViewModel Editor { get; } = editor;
        public IFileService FileService { get; } = fileService;
        public ITextureCacheService TextureCacheService { get; } = textureCacheService;
 
        // The room being edited. Generated property name is Room, so the existing
        // Room.* bindings in the axaml (Backgrounds, Views, Flags, Name) keep working.
        [ObservableProperty]
        private UndertaleRoom room = room;
 
        // Selection state. WPF tracked this through ObjElemDict (object -> control);
        // we keep it as pure VM state instead so the VM never touches the visual tree.
        // The tree's SelectedItem binds two-way to this.
        [ObservableProperty]
        private object selectedObject;
 
        // The active layer (GMS2). For now this tracks SelectedObject only when the
        // selected node is itself a Layer. Resolving the parent layer of a selected
        // instance/tile is deferred to step 3, once we scan layer contents.
        [ObservableProperty]
        private UndertaleRoom.Layer selectedLayer;
 
        // View transform state. Not used until step 2 (rendering), but cheap to hold
        // now so the axaml preview area has something to bind against.
        [ObservableProperty]
        private double roomScale = 1.0;
 
        [ObservableProperty]
        private double scrollX;
 
        [ObservableProperty]
        private double scrollY;
 
        // Fired by the CommunityToolkit generator whenever SelectedObject changes.
        partial void OnSelectedObjectChanged(object value)
        {
            if (value is UndertaleRoom.Layer layer)
                SelectedLayer = layer;
        }
    }
}
