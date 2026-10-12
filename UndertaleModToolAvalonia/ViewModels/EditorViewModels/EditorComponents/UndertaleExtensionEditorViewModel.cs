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
    public partial class UndertaleExtensionEditorViewModel(string title, UndertaleExtension undertaleExtension) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleExtension undertaleExtension = undertaleExtension;

        public UndertaleExtensionOption.OptionKind[] OptionKinds { get; } = 
            Enum.GetValues<UndertaleExtensionOption.OptionKind>();

        public int MyIndex => AppConstants.Data?.Extensions.IndexOf(UndertaleExtension) ?? -1;

        public bool IsProductIdVisible => UndertaleExtension.ProductDataEligible(AppConstants.Data)
                                          && MyIndex >= 0 && MyIndex < AppConstants.Data!.FORM.EXTN.productIdData.Count;

        public byte[]? ProductIdData
        {
            get
            {
                if (IsProductIdVisible)
                    return AppConstants.Data!.FORM.EXTN.productIdData[MyIndex];
                return null;
            }
            set
            {
                if (IsProductIdVisible && value is not null)
                    AppConstants.Data!.FORM.EXTN.productIdData[MyIndex] = value;
            }
        }

        [RelayCommand]
        private void AddFile()
        {
            int lastItem = UndertaleExtension.Files.Count;

            UndertaleExtensionFile obj = new()
            {
                Kind = UndertaleExtensionKind.Dll,
                Filename = AppConstants.Data!.Strings.MakeString($"NewExtensionFile{lastItem}.dll"),
                Functions = new UndertalePointerList<UndertaleExtensionFunction>()
            };
            UndertaleExtension.Files.Add(obj);
        }

        [RelayCommand]
        private void AddOption()
        {
            int lastItem = UndertaleExtension.Options.Count;

            UndertaleExtensionOption obj = new()
            {
                Name = AppConstants.Data!.Strings.MakeString($"extensionOption{lastItem}"),
                Value = AppConstants.Data.Strings.MakeString("", true)
            };
            UndertaleExtension.Options.Add(obj);
        }
    }
}
