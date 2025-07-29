using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleBackgroundEditorViewModel : EditorContentViewModel
    {
        [ObservableProperty]
        UndertaleBackground undertaleBackground;

        public UndertaleBackgroundEditorViewModel(string title, UndertaleBackground undertaleBackground) : base(title)
        {
            this.undertaleBackground = undertaleBackground;
        }
    }
}
