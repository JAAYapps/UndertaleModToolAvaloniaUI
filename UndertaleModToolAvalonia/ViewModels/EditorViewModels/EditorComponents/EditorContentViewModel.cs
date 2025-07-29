using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    /// <summary>A base class for the information of a tab content state.</summary>
    public partial class EditorContentViewModel : ViewModelBase
    {
        /// <summary>The scroll position of the object editor.</summary>
        [ObservableProperty]
        private double mainScrollPosition;

        [ObservableProperty]
        private string mainText;

        public EditorContentViewModel(string title)
        {
            MainText = title;
        }
    }

    /// <summary>Stores the information about the tab with a code.</summary>
    public partial class CodeViewModel(string title) : EditorContentViewModel(title)
    {
        /// <summary>The decompiled code position.</summary>
        [ObservableProperty]
        public (int Line, int Column, double ScrollPos) decompiledCodePosition;

        /// <summary>The disassembly code position.</summary>
        [ObservableProperty]
        public (int Line, int Column, double ScrollPos) disassemblyCodePosition;

        /// <summary>The scroll position of decompiled code.</summary>
        [ObservableProperty]
        public double decompiledScrollPos;

        /// <summary>The scroll position of disassembly code.</summary>
        [ObservableProperty]
        public double disassemblyScrollPos;

        /// <summary>Whether the "Decompiled" tab is open.</summary>
        [ObservableProperty]
        public bool isDecompiledOpen;

        /// <summary>Whether this state was already restored (applied to the code editor).</summary>
        [ObservableProperty]
        public bool isStateRestored;
    }

    /// <summary>Stores the information about the tab with a room.</summary>
    public partial class RoomViewModel(string title) : EditorContentViewModel(title)
    {
        /// <summary>The scroll position of the room editor preview.</summary>
        [ObservableProperty]
        public (double Left, double Top) roomPreviewScrollPosition;

        /// <summary>The scale of the room editor preview.</summary>
        [ObservableProperty]
        public Transform roomPreviewTransform;

        /// <summary>The scroll position of the room objects tree.</summary>
        [ObservableProperty]
        public (double Left, double Top) objectsTreeScrollPosition;

        /// <summary>The states of the room objects tree items.</summary>
        /// <remarks>
        /// An order of the states is following:
        /// Backgrounds, views, game objects, tiles, layers.
        /// </remarks>
        [ObservableProperty]
        public bool[] objectTreeItemsStates;

        /// <summary>The selected room object.</summary>
        [ObservableProperty]
        public object selectedObject;
    }

    /// <summary>Stores the information about the tab with a font.</summary>
    public partial class FontViewModel(string title) : EditorContentViewModel(title)
    {
        /// <summary>The selected font glyph.</summary>
        [ObservableProperty]
        public UndertaleFont.Glyph selectedGlyph;

        /// <summary>The scroll position of the glyphs grid.</summary>
        [ObservableProperty]
        public double glyphsScrollPosition;
    }

    /// <summary>Stores the information about the tab with a sprite.</summary>
    public partial class SpriteViewModel(string title) : EditorContentViewModel(title)
    {
        /// <summary>The selected sprite texture.</summary>
        [ObservableProperty]
        public object selectedTexture;

        /// <summary>The scroll position of the textures grid.</summary>
        [ObservableProperty]
        public double textureListScrollPosition;

        /// <summary>The selected sprite mask.</summary>
        [ObservableProperty]
        public object selectedMask;

        /// <summary>The scroll position of the masks grid.</summary>
        [ObservableProperty]
        public double maskListScrollPosition;
    }

    /// <summary>Stores the information about the tab with a tile set (or a background).</summary>
    public partial class TileSetViewModel(string title) : EditorContentViewModel(title)
    {
        /// <summary>The selected tile.</summary>
        [ObservableProperty]
        public object selectedTile;

        /// <summary>The scroll position of the tile list grid.</summary>
        [ObservableProperty]
        public double tileListScrollPosition;
    }

    /// <summary>Stores the information about the tab with a texture group.</summary>
    public partial class TextureGroupViewModel(string title) : EditorContentViewModel(title)
    {
        /// <summary>The states of the texture group lists.</summary>
        /// <remarks>
        /// An order of the states is following:
        /// Texture pages, sprites, spine sprites, fonts, tilesets.
        /// </remarks>
        [ObservableProperty]
        public (bool IsExpanded, double ScrollPos, object SelectedItem)[] groupListsStates;
    }

    /// <summary>Stores the information about the tab with a texture page.</summary>
    public partial class TexturePageViewModel(string title) : EditorContentViewModel(title)
    {
        /// <summary>The scroll position of the embedded texture editor preview.</summary>
        [ObservableProperty]
        public (double Left, double Top) texturePreviewScrollPosition;

        /// <summary>The scale of the embedded texture editor preview.</summary>
        [ObservableProperty]
        public Transform texturePreviewTransform;
    }
}
