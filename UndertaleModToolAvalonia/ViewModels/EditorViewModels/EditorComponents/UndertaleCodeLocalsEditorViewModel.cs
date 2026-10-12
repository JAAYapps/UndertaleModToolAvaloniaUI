using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleCodeLocalsEditorViewModel(string title, UndertaleCodeLocals undertaleCodeLocals) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleCodeLocals undertaleCodeLocals = undertaleCodeLocals;

        [RelayCommand]
        private void AddLocal()
        {
            UndertaleCodeLocals.LocalVar obj = new();
            obj.Index = (uint)UndertaleCodeLocals.Locals.Count;
            UndertaleCodeLocals.Locals.Add(obj);
        }
    }
}
