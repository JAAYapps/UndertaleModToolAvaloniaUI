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
    public partial class UndertaleExtensionFileEditorViewModel(string title, UndertaleExtensionFile undertaleExtensionFile) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleExtensionFile undertaleExtensionFile = undertaleExtensionFile;

        public UndertaleExtensionKind[] ExtensionKinds { get; } = 
            Enum.GetValues<UndertaleExtensionKind>();

        [RelayCommand]
        private void AddFunction()
        {
            int lastItem = UndertaleExtensionFile.Functions.Count;

            UndertaleExtensionFunction obj = new()
            {
                Name = AppConstants.Data!.Strings.MakeString($"new_extension_function_{lastItem}"),
                ExtName = AppConstants.Data.Strings.MakeString($"new_extension_function_{lastItem}_ext"),
                RetType = UndertaleExtensionVarType.Double,
                Arguments = new UndertaleSimpleList<UndertaleExtensionFunctionArg>(),
                Kind = 11,
                ID = AppConstants.Data.ExtensionFindLastId()
            };
            UndertaleExtensionFile.Functions.Add(obj);
        }
    }
}
