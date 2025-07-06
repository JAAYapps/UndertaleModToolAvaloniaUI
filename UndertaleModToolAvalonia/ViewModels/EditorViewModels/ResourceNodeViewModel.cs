using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModToolAvalonia.Services.ReferenceFinderService;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class ResourceNodeViewModel : ViewModelBase
    {
        public object? Content { get; }

        public string Header { get; }

        public ObservableCollection<ResourceNodeViewModel> Children { get; } = new();

        public ICommand OpenInNewTabCommand { get; }
        public ICommand FindAllReferencesCommand { get; }
        public ICommand DeleteCommand { get; }
        public bool CanFindReferences { get; }

        public ResourceNodeViewModel(object? content, EditorViewModel editor, IReferenceFinderService referenceFinderService)
        {
            Content = content;
            Header = content?.ToString() ?? "(null)";

            if (content is UndertaleResource resource)
            {
                CanFindReferences = referenceFinderService.IsTypeReferenceable(resource);
            }

            OpenInNewTabCommand = editor.OpenAssetInNewTabCommand;
            FindAllReferencesCommand = editor.FindAllReferencesCommand;
            DeleteCommand = editor.DeleteAssetCommand;
        }
    }
}
