using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleGameEndEditorViewModel(string title, IList<UndertaleGlobalInit> GameEnds) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        public ObservableCollection<UndertaleGlobalInit> gameEnds = (ObservableCollection<UndertaleGlobalInit>)GameEnds;
    }
}
