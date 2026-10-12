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
    public partial class UndertaleExtensionFunctionEditorViewModel(string title, UndertaleExtensionFunction undertaleExtensionFunction) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleExtensionFunction undertaleExtensionFunction = undertaleExtensionFunction;

        public UndertaleExtensionVarType[] VarTypes { get; } = 
            Enum.GetValues<UndertaleExtensionVarType>();

        [RelayCommand]
        private void AddArgument()
        {
            UndertaleExtensionFunction.Arguments.Add(new UndertaleExtensionFunctionArg());
        }
    }
}
