using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog
{
    public partial class ReferenceResultNode : ObservableObject
    {
        private const int PagingThreshold = 200;
        private const int PageSize = 100;

        private readonly FindReferencesResultsViewModel resultsVM;
        private readonly List<ReferenceResultNode> allChildren = new();
        private List<ReferenceResultNode> visibleResults = new();

        [ObservableProperty]
        private bool isExpanded = true;

        public string Header { get; }

        public object? Model { get; }

        public bool IsCategory => Model is null;

        public bool IsResultItem => !IsCategory;

        public bool CanFindReferences { get; }

        public ObservableRangeCollection<ReferenceResultNode> Children { get; } = new();

        public IReadOnlyList<ReferenceResultNode> VisibleResults => visibleResults;

        public ICommand OpenInNewTabCommand { get; }

        public ICommand FindAllReferencesCommand { get; }

        public ReferenceResultNode(string header, object? model, FindReferencesResultsViewModel resultsVM)
        {
            Header = header;
            Model = model;
            this.resultsVM = resultsVM;
            CanFindReferences = model is UndertaleObject && UndertaleResourceReferenceMap.IsTypeReferenceable(model.GetType());

            OpenInNewTabCommand = resultsVM.OpenInNewTabCommand;
            FindAllReferencesCommand = resultsVM.FindAllReferencesCommand;
        }

        public void AddChild(ReferenceResultNode child)
        {
            allChildren.Add(child);
        }

        public void Filter(string? text)
        {
            if (IsResultItem)
                return;

            if (string.IsNullOrEmpty(text))
                visibleResults = allChildren;
            else
                visibleResults = allChildren.Where(x => x.Header.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();

            Children.Clear();
            if (visibleResults.Count <= PagingThreshold)
            {
                Children.AddRange(visibleResults);
                return;
            }

            var pages = new List<ReferenceResultNode>();
            for (int i = 0; i < visibleResults.Count; i += PageSize)
            {
                var chunk = visibleResults.Skip(i).Take(PageSize).ToList();
                var page = new ReferenceResultNode($"{Header} ({i + 1} - {i + chunk.Count})", null, resultsVM);
                page.IsExpanded = false;
                page.Children.AddRange(chunk);
                pages.Add(page);
            }
            Children.AddRange(pages);
        }
    }

    public partial class FindReferencesResultsViewModel : ViewModelBase, IInitializable<FindReferencesResultsParameters>
    {
        private UndertaleData? data;
        private string? sourceObjName;

        public object? SourceObject { get; private set; }

        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private string heading = "";

        [ObservableProperty]
        private string emptyText = "";

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private ReferenceResultNode? selectedResult;

        public ObservableCollection<ReferenceResultNode> Results { get; } = new();

        public bool HasResults => Results.Count > 0;

        public Task<bool> InitializeAsync(FindReferencesResultsParameters parameters)
        {
            data = parameters.data;
            SourceObject = parameters.undertaleResource;

            if (SourceObject is null)
            {
                Title = "Unreferenced game assets";
                Heading = "Results for unreferenced game assets.";
                EmptyText = "No unreferenced assets found.";
            }
            else
            {
                if (SourceObject is UndertaleNamedResource namedObj)
                    sourceObjName = namedObj.Name?.Content;
                else if (SourceObject is UndertaleString str)
                    sourceObjName = str.Content;
                else if (SourceObject is ValueTuple<UndertaleBackground, UndertaleBackground.TileID> tileTuple)
                    sourceObjName = $"Tile {tileTuple.Item2.ID} of {tileTuple.Item1.Name?.Content}";
                sourceObjName ??= SourceObject.GetType().Name;

                Title = $"References of game asset \"{sourceObjName}\"";
                Heading = $"Results for game asset\n\"{sourceObjName}\".";
                EmptyText = "No references found.";
            }

            if (parameters.results is not null)
                ProcessResults(parameters.results);

            return Task.FromResult(true);
        }

        private void ProcessResults(Dictionary<string, List<object>> results)
        {
            foreach (var result in results)
            {
                if (result.Value is null || result.Value.Count == 0)
                    continue;

                if (result.Value[0] is EditorContentViewModel)
                {
                    Results.Add(new ReferenceResultNode(result.Key, result.Value[0], this));
                    continue;
                }

                ReferenceResultNode category = new(result.Key, null, this);
                foreach (object obj in result.Value)
                {
                    if (obj is object[] inst)
                    {
                        if (inst.Length > 0)
                            category.AddChild(new ReferenceResultNode(GetItemName(obj), inst[^1], this));
                    }
                    else if (obj is not null)
                        category.AddChild(new ReferenceResultNode(GetItemName(obj), obj, this));
                }
                category.Filter(SearchText);
                Results.Add(category);
            }
        }

        private static string GetItemName(object obj)
        {
            if (obj is object[] inst)
                return string.Join(" — ", inst.Select(GetItemName));
            if (obj is UndertaleNamedResource namedRes)
                return namedRes.Name?.Content ?? obj.ToString() ?? "";
            if (obj is UndertaleString str)
                return StringTitleConverter.Instance.Convert(str.Content, null, null, null) as string ?? "";
            return obj?.ToString() ?? "";
        }

        partial void OnSearchTextChanged(string value)
        {
            foreach (ReferenceResultNode node in Results)
                node.Filter(value);
        }

        private bool IsStale() => !ReferenceEquals(data, AppConstants.Data);

        [RelayCommand]
        private async Task Open(ReferenceResultNode? node)
        {
            await OpenNode(node, false);
        }

        [RelayCommand]
        private async Task OpenInNewTab(ReferenceResultNode? node)
        {
            await OpenNode(node, true);
        }

        private async Task OpenNode(ReferenceResultNode? node, bool inNewTab)
        {
            if (node?.Model is null)
                return;

            if (IsStale())
            {
                await App.Current!.ShowError("The object reference is stale - a different game data was loaded.");
                return;
            }

            WeakReferenceMessenger.Default.Send(new OpenAssetMessage(node.Model, inNewTab));
        }

        [RelayCommand]
        private async Task FindAllReferences(ReferenceResultNode? node)
        {
            if (node?.Model is null)
                return;

            if (IsStale())
            {
                await App.Current!.ShowError("The object reference is stale - a different game data was loaded.");
                return;
            }

            WeakReferenceMessenger.Default.Send(new FindReferencesMessage(node.Model));
        }

        [RelayCommand(CanExecute = nameof(HasResults))]
        private async Task ExportResults()
        {
            if (IsStale())
            {
                await App.Current!.ShowError("The object references are stale - a different game data was loaded.");
                return;
            }

            StringBuilder sb = new();
            sb.AppendLine(Title + ":");
            sb.AppendLine(new string('-', Title.Length + 1));
            sb.AppendLine();
            int initLength = sb.Length;

            foreach (ReferenceResultNode node in Results)
            {
                if (node.IsResultItem)
                {
                    sb.AppendLine(node.Header);
                    sb.AppendLine();
                    continue;
                }

                if (node.VisibleResults.Count == 0)
                    continue;

                sb.AppendLine(node.Header + ':');
                foreach (ReferenceResultNode child in node.VisibleResults)
                    sb.AppendLine($"    {child.Header}");
                sb.AppendLine();
            }

            if (sb.Length == initLength)
            {
                await App.Current!.ShowError("No results to export.");
                return;
            }

            string? folderPath = Path.GetDirectoryName(AppConstants.FilePath);
            if (string.IsNullOrEmpty(folderPath))
            {
                await App.Current!.ShowError("The results are saved next to the data file, but this data file was never saved.\nSave it first, then export again.");
                return;
            }

            string fileName = sourceObjName is null
                              ? "unreferenced_assets.txt"
                              : $"references_of_asset_{string.Join("_", sourceObjName.Split(Path.GetInvalidFileNameChars()))}.txt";
            string? filePath = Paths.TryJoinVerifyWithinDirectory(folderPath, fileName);
            if (filePath is null)
            {
                await App.Current!.ShowError("Failed to choose good output file name; directory escaped.");
                return;
            }

            if (File.Exists(filePath))
                if (await App.Current!.ShowQuestion($"File \"{filePath}\" exists.\nOverwrite?") != ButtonResult.Yes)
                    return;

            try
            {
                File.WriteAllText(filePath, sb.ToString().TrimEnd() + Environment.NewLine);
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError($"Failed to save the results at path\n\"{filePath}\".\n\n{ex.Message}");
                return;
            }

            await App.Current!.ShowMessage($"The results were successfully saved at path\n\"{filePath}\".");
        }
    }
}
