using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    /// <summary>Stores the information about the tab with global init scripts.</summary>
    public partial class UndertaleGlobalInitEditorViewModel(string title, IList<UndertaleGlobalInit> globalInits) : EditorContentViewModel(title)
    {
        /// <summary>The selected script.</summary>
        [ObservableProperty]
        public object selectedScript;

        /// <summary>The scroll position of the script list grid.</summary>
        [ObservableProperty]
        public double scriptListScrollPosition;

        public IList<UndertaleGlobalInit> GlobalInits { get; private set; } = globalInits;
    }
}
