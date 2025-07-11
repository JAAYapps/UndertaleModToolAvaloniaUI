using System.Collections.ObjectModel;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModToolAvalonia.Services.ReferenceFinderService;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class ResourceNodeViewModel : ViewModelBase
    {
        public string Header { get; }

        public object? Model { get; }

        public ObservableCollection<ResourceNodeViewModel> Children { get; } = new();

        public bool IsCategory { get; }

        public bool IsResourceItem => !IsCategory;

        public ICommand AddItemCommand { get; }

        public ICommand CopyItemToClipboardCommand { get; }

        public ICommand DeleteItemCommand { get; }

        public ICommand OpenInNewTabCommand { get; }

        public ICommand FindAllReferencesCommand { get; }

        public bool CanFindReferences { get; }

        public bool ExpandedByDefault { get; }

        public ResourceNodeViewModel(string header, object? model, EditorViewModel editorVM, IReferenceFinderService referenceFinderService, bool expanded = false)
        {
            Header = header;
            Model = model;
            ExpandedByDefault = expanded;

            // A node is a category if it doesn't represent a specific data model
            IsCategory = model is null || model is UndertaleData;

            if (model is UndertaleObject resource)
            {
                CanFindReferences = referenceFinderService.IsTypeReferenceable(resource);
            }

            // Link to commands on the main EditorViewModel
            AddItemCommand = editorVM.AddNewItemCommand;
            DeleteItemCommand = editorVM.DeleteItemCommand;
            OpenInNewTabCommand = editorVM.OpenAssetInNewTabCommand;
            FindAllReferencesCommand = editorVM.FindAllReferencesCommand;
            CopyItemToClipboardCommand = editorVM.CopyItemToClipboardCommand;
        }
    }
}
