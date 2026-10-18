using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Project;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.ClipboardService;
using UndertaleModToolAvalonia.Services.ProjectService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public record SearchInCodeResult(string Code, int LineNumber, string LineText);

    public partial class SearchInCodeViewModel : ViewModelBase, IInitializable<SearchInCodeParameters>
    {
        private readonly IClipboardService? clipboardService;
        private readonly IProjectService? projectService;

        private UndertaleData? searchedData;
        private UndertaleCodeEditorViewModel.CodeEditorTab resultsTab = UndertaleCodeEditorViewModel.CodeEditorTab.Decompiled;

        public string Title { get; } = "Search in code";

        public ObservableCollection<SearchInCodeResult> Results { get; } = new();

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private bool isCaseSensitive;

        [ObservableProperty]
        private bool isRegexSearch;

        [ObservableProperty]
        private bool isMultilineRegex;

        [ObservableProperty]
        private bool isInAssembly;

        [ObservableProperty]
        private bool filterByName;

        [ObservableProperty]
        private string nameFilter = "";

        [ObservableProperty]
        private bool nameIsCaseSensitive;

        [ObservableProperty]
        private bool nameIsRegex;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        private bool isSearching;

        [ObservableProperty]
        private double progress;

        [ObservableProperty]
        private double progressMaximum = 1;

        [ObservableProperty]
        private string statusText = "";

        [ObservableProperty]
        private SearchInCodeResult? selectedResult;

        public SearchInCodeViewModel() { }

        public SearchInCodeViewModel(IClipboardService clipboardService, IProjectService projectService)
        {
            this.clipboardService = clipboardService;
            this.projectService = projectService;
        }

        public Task<bool> InitializeAsync(SearchInCodeParameters parameters)
        {
            if (parameters.Query is string query && query.Length <= 256 && query.Count(x => x == '\n') <= 16)
                SearchText = query;
            IsInAssembly = parameters.InAssembly;
            return Task.FromResult(true);
        }

        private bool CanSearch() => !IsSearching;

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task Search()
        {
            UndertaleData? data = AppConstants.Data;
            if (data is null)
            {
                await App.Current!.ShowError("No data.win loaded.");
                return;
            }

            if (data.IsYYC())
            {
                await App.Current!.ShowError("Can't search code in YYC game, there's no code to search.");
                return;
            }

            string text = SearchText.Replace("\r\n", "\n");
            if (string.IsNullOrEmpty(text))
                return;

            Regex? keywordRegex = null;
            if (IsRegexSearch)
            {
                RegexOptions options = RegexOptions.Compiled;
                if (!IsCaseSensitive)
                    options |= RegexOptions.IgnoreCase;
                if (IsMultilineRegex)
                    options |= RegexOptions.Multiline;
                try
                {
                    keywordRegex = new(text, options);
                }
                catch (ArgumentException e)
                {
                    await App.Current!.ShowError($"Invalid Regex: {e.Message}");
                    return;
                }
            }

            IList<UndertaleCode> codeEntriesToSearch = data.Code;
            if (FilterByName && !string.IsNullOrEmpty(NameFilter))
            {
                string name = NameFilter;
                if (NameIsRegex)
                {
                    try
                    {
                        Regex nameRegex = new(name, NameIsCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        codeEntriesToSearch = data.Code.Where(c => !string.IsNullOrEmpty(c?.Name?.Content) && nameRegex.IsMatch(c.Name.Content)).ToList();
                    }
                    catch (ArgumentException e)
                    {
                        await App.Current!.ShowError($"Invalid name Regex: {e.Message}");
                        return;
                    }
                }
                else
                {
                    var comparison = NameIsCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    codeEntriesToSearch = data.Code.Where(c => !string.IsNullOrEmpty(c?.Name?.Content) && c.Name.Content.Contains(name, comparison)).ToList();
                }
            }

            if (codeEntriesToSearch.Count == 0)
            {
                await App.Current!.ShowMessage("There are no code entries that match the name filter.");
                return;
            }

            bool inAssembly = IsInAssembly;
            bool caseSensitive = IsCaseSensitive;
            ProjectContext? project = projectService?.Project;
            GlobalDecompileContext? decompileContext = inAssembly ? null : new GlobalDecompileContext(data);

            IsSearching = true;
            Results.Clear();
            Progress = 0;
            ProgressMaximum = codeEntriesToSearch.Count;
            StatusText = "Searching...";

            var resultsDict = new ConcurrentDictionary<UndertaleCode, List<SearchInCodeResult>>();
            var failedList = new ConcurrentBag<UndertaleCode>();
            int progressCount = 0;

            void SearchInUndertaleCode(UndertaleCode code)
            {
                try
                {
                    if (code is not null && code.ParentEntry is null)
                    {
                        string codeText;
                        if (inAssembly)
                            codeText = code.Disassemble(data.Variables, data.CodeLocals?.For(code), data.CodeLocals is null);
                        else if (project is null || !project.TryGetCodeSource(code, out codeText))
                            codeText = new Underanalyzer.Decompiler.DecompileContext(decompileContext!, code, data.ToolInfo.DecompilerSettings).DecompileToString();

                        List<SearchInCodeResult> found = SearchInCodeText(code.Name.Content, codeText, text, keywordRegex, caseSensitive);
                        if (found.Count > 0)
                            resultsDict[code] = found;
                    }
                }
                catch (Exception)
                {
                    if (code is not null)
                        failedList.Add(code);
                }

                int done = Interlocked.Increment(ref progressCount);
                if (done % 64 == 0)
                    Dispatcher.UIThread.Post(() => Progress = done);
            }

            await Task.Run(() => Parallel.ForEach(codeEntriesToSearch, SearchInUndertaleCode));

            Dictionary<UndertaleCode, int> order = new();
            for (int i = 0; i < data.Code.Count; i++)
                if (data.Code[i] is UndertaleCode code)
                    order.TryAdd(code, i);

            var resultsSorted = resultsDict.OrderBy(x => order.GetValueOrDefault(x.Key, int.MaxValue)).ToList();
            int resultCount = 0;
            List<SearchInCodeResult> all = new();
            foreach (var result in resultsSorted)
            {
                all.AddRange(result.Value);
                resultCount += result.Value.Count;
            }

            Results.Clear();
            foreach (SearchInCodeResult result in all)
                Results.Add(result);

            searchedData = data;
            resultsTab = inAssembly ? UndertaleCodeEditorViewModel.CodeEditorTab.Disassembly : UndertaleCodeEditorViewModel.CodeEditorTab.Decompiled;

            static string GetWordEnding(int quantity, bool isResults)
            {
                if (isResults)
                    return quantity != 1 ? "s" : "";
                return quantity != 1 ? "ies" : "y";
            }

            string status = $"{resultCount} result{GetWordEnding(resultCount, true)} found in {resultsSorted.Count} code entr{GetWordEnding(resultsSorted.Count, false)}.";
            if (failedList.Count > 0)
                status += $" {failedList.Count} code entr{GetWordEnding(failedList.Count, false)} with an error.";
            StatusText = status;
            Progress = ProgressMaximum;
            IsSearching = false;
        }

        private static List<SearchInCodeResult> SearchInCodeText(string codeName, string codeText, string text, Regex? keywordRegex, bool caseSensitive)
        {
            List<int> matches = new();
            if (keywordRegex is not null)
            {
                foreach (Match match in keywordRegex.Matches(codeText))
                    matches.Add(match.Index);
            }
            else
            {
                StringComparison comparisonType = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                int index = 0;
                while ((index = codeText.IndexOf(text, index, comparisonType)) != -1)
                {
                    matches.Add(index);
                    index += text.Length;
                }
            }

            List<SearchInCodeResult> results = new();
            int lineNumber = 0;
            int lineStartIndex = 0;
            int scanned = 0;
            foreach (int index in matches)
            {
                for (; scanned < index; scanned++)
                {
                    if (codeText[scanned] == '\n')
                    {
                        lineNumber++;
                        lineStartIndex = scanned + 1;
                    }
                }

                int lineEndIndex = codeText.IndexOf('\n', index);
                lineEndIndex = lineEndIndex == -1 ? codeText.Length : lineEndIndex;

                string lineText = lineEndIndex - lineStartIndex > 128
                                  ? codeText[lineStartIndex..(lineStartIndex + 128)] + "..."
                                  : codeText[lineStartIndex..lineEndIndex];

                results.Add(new SearchInCodeResult(codeName, lineNumber + 1, lineText));
            }
            return results;
        }

        [RelayCommand]
        private async Task Open(IList? items)
        {
            await OpenResults(items, false);
        }

        [RelayCommand]
        private async Task OpenInNewTab(IList? items)
        {
            await OpenResults(items, true);
        }

        public async Task OpenResults(IList? items, bool inNewTab)
        {
            if (IsSearching)
            {
                await App.Current!.ShowError("Can't open results while a search is in progress.");
                return;
            }

            if (!ReferenceEquals(searchedData, AppConstants.Data))
            {
                await App.Current!.ShowError("The search results are stale - a different game data was loaded.");
                return;
            }

            foreach (SearchInCodeResult result in (items ?? Array.Empty<object>()).OfType<SearchInCodeResult>().ToList())
            {
                UndertaleCode? code = AppConstants.Data!.Code.ByName(result.Code);
                if (code is null)
                {
                    await App.Current!.ShowError($"Can't find code entry \"{result.Code}\".\n(probably, different game data was loaded)");
                    return;
                }

                WeakReferenceMessenger.Default.Send(new OpenCodeLineMessage(code, result.LineNumber, resultsTab, inNewTab));
                inNewTab = true;
            }
        }

        [RelayCommand]
        private async Task Copy(IList? items)
        {
            await CopyResults((items ?? Array.Empty<object>()).OfType<SearchInCodeResult>().OrderBy(Results.IndexOf));
        }

        [RelayCommand]
        private async Task CopyAll()
        {
            await CopyResults(Results);
        }

        private async Task CopyResults(IEnumerable<SearchInCodeResult> results)
        {
            string str = string.Join('\n', results.Select(result => $"{result.Code}\t{result.LineNumber}\t{result.LineText}"));
            try
            {
                await clipboardService!.SetTextAsync(str);
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Can't copy the results to clipboard due to this error:\n" +
                                             ex.Message + ".\nYou probably should try again.");
            }
        }
    }
}