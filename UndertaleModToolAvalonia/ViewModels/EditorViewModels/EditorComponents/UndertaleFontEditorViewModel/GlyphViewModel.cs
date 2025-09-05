using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel
{
    public partial class GlyphViewModel(UndertaleFont.Glyph model) : ObservableObject
    {
        public UndertaleFont.Glyph Model { get; } = model;

        [ObservableProperty]
        private bool isSelected;

        public ushort SourceX 
        { 
            get => Model.SourceX;
            set
            {
                Model.SourceX = value;
                OnPropertyChanged(nameof(Model.SourceX));
            }
        }

        public ushort SourceY
        { 
            get => Model.SourceY;
            set
            {
                Model.SourceY = value;
                OnPropertyChanged(nameof(Model.SourceY));
            }
        }

        public ushort SourceWidth
        {
            get => Model.SourceWidth;
            set
            {
                Model.SourceWidth = value;
                OnPropertyChanged(nameof(Model.SourceWidth));
            }
        }
        
        public ushort SourceHeight
        {
            get => Model.SourceHeight;
            set
            {
                Model.SourceHeight = value;
                OnPropertyChanged(nameof(Model.SourceHeight));
            }
        }

        public ushort Character => Model.Character;
        public short Shift
        {
            get => Model.Shift;
            set
            {
                Model.Shift = value;
                OnPropertyChanged(nameof(Model.Shift));
            }
        }

        public short Offset => Model.Offset;
    }
}
