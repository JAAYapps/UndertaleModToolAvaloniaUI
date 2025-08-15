using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModLib;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleVariableChunkEditorViewModel(string title, UndertaleChunkVARI undertaleChunkVARI) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleChunkVARI undertaleChunkVARI = undertaleChunkVARI;
    }
}
