using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModToolAvalonia.Services.ReferenceFinderService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class ResourceNodeViewModel : ViewModelBase
    {
        private EditorViewModel editorVM;

        private IReferenceFinderService referenceFinderService;

        public string Header { get; }

        public object? Model { get; }

        private readonly IEnumerable<UndertaleObject>? sourceChildren;

        private bool isLoaded = false;
        private bool isLoading = false;

        public ObservableRangeCollection<ResourceNodeViewModel> Children { get; } = new();

        public bool IsCategory { get; }

        public bool IsResourceItem => !IsCategory;

        public ICommand AddItemCommand { get; }

        public ICommand CopyItemToClipboardCommand { get; }

        public ICommand DeleteItemCommand { get; }

        public ICommand OpenInNewTabCommand { get; }

        public ICommand FindAllReferencesCommand { get; }

        public bool CanFindReferences { get; }

        [ObservableProperty]
        public bool isExpanded;

        public ResourceNodeViewModel(string header, object? model, EditorViewModel editorVM, IReferenceFinderService referenceFinderService, bool expanded = false, IEnumerable<UndertaleObject>? sourceChildren = null)
        {
            Header = header;
            Model = model;
            isExpanded = expanded;
            this.sourceChildren = sourceChildren;
            this.editorVM = editorVM;
            this.referenceFinderService = referenceFinderService;

            if (sourceChildren?.Any() == true)
            {
                Children.Add(new ResourceNodeViewModel("Loading...", null, editorVM, referenceFinderService));
            }

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

        // This method is called automatically when IsExpanded changes.
        partial void OnIsExpandedChanged(bool value)
        {
            // If the node is being expanded for the first time and isn't already loaded...
            if (value && !isLoaded)
            {
                LoadChildren();
            }
        }

        private const int PagingThreshold = 200; // Any list larger than this will be paged.
        private const int PageSize = 100;

        private async void LoadChildren()
        {
            if (isLoading) return;
            isLoading = true;

            //var childViewModels = new List<ResourceNodeViewModel>();

            //await Task.Run(() =>
            //{
            //    if (sourceChildren == null) return;

            //    foreach (var childModel in sourceChildren)
            //    {
            //        var childViewModel = new ResourceNodeViewModel(childModel.ToString(), childModel, editorVM, referenceFinderService);
            //        childViewModels.Add(childViewModel);
            //    }
            //});

            var newChildren = new List<ResourceNodeViewModel>();

            await Task.Run(() =>
            {
                if (sourceChildren == null) return;

                // We need a concrete list to work with Skip/Take efficiently
                var sourceList = sourceChildren.ToList();

                if (sourceList.Count > PagingThreshold)
                {
                    // PAGING LOGIC for very large lists
                    for (int i = 0; i < sourceList.Count; i += PageSize)
                    {
                        var chunk = sourceList.Skip(i).Take(PageSize).ToList();
                        string header = $"{Header} ({i + 1} - {i + chunk.Count})";

                        // Create a new "group" node. This node is also lazy-loading.
                        var groupNode = new ResourceNodeViewModel(header, null, editorVM, referenceFinderService, false, chunk);
                        newChildren.Add(groupNode);
                    }
                }
                else
                {
                    // DIRECT LOADING for smaller lists
                    foreach (var childModel in sourceList)
                    {
                        var childViewModel = new ResourceNodeViewModel(childModel.ToString(), childModel, editorVM, referenceFinderService);
                        newChildren.Add(childViewModel);
                    }
                }
            });

            Children.Clear();
            Children.AddRange(newChildren);

            isLoaded = true;
            isLoading = false;
        }

        public void MarkAsLoaded()
        {
            isLoaded = true;
        }
    }
}
