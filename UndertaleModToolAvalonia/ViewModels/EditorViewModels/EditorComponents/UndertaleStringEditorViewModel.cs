using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleStringEditorViewModel(string title, UndertaleString undertaleString) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleString undertaleString = undertaleString;
    }
}
