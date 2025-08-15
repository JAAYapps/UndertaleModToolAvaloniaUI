using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleVariableEditorViewModel(string title, UndertaleVariable undertaleVariable) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleVariable undertaleVariable = undertaleVariable;
    }
}
