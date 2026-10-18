using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;
using UndertaleModLib.Project;
using UndertaleModToolAvalonia.Converters;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Models.EditorModels;
using UndertaleModToolAvalonia.Models.UndertaleReferenceTypes;
using UndertaleModToolAvalonia.Services.ClipboardService;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Services.PlayerService;
using UndertaleModToolAvalonia.Services.ProfileService;
using UndertaleModToolAvalonia.Services.ProjectService;
using UndertaleModToolAvalonia.Services.ReferenceFinderService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class EditorViewModel : ViewModelBase
    {
        // [ObservableProperty] private int currentTabIndex = 0;

        [ObservableProperty] private object highlighted;

        [ObservableProperty] private bool isEnabled = true;

        private ILoadingDialogService loadingDialogService;

        private IProfileService profileService;

        private IDialogService dialogService;

        private IReferenceFinderService referenceFinderService;

        private IPlayer playerService;

        private IFileService fileService;

        private ITextureCacheService textureCacheService;

        private IClipboardService clipboardService;

        private IProjectService projectService;

        [ObservableProperty]
        private ObservableCollection<TabViewModel> tabs = new();

        [ObservableProperty]
        private TabViewModel? currentTab;

        public List<TabViewModel> ClosedTabsHistory { get; } = new();

        [ObservableProperty] private bool wasWarnedAboutTempRun = false;
        
        [ObservableProperty] private string commandBoxText = string.Empty;
        
        public enum SaveResult
        {
            NotSaved,
            Saved,
            Error
        }
        public enum ScrollDirection
        {
            Left,
            Right
        }
        
        [ObservableProperty] private bool roomRendererEnabled;

        partial void OnRoomRendererEnabledChanged(bool value)
        {
            // TODO Add Avalonia implementation.
            // if (UndertaleRoomRenderer.RoomRendererTemplate is null)
            //     UndertaleRoomRenderer.RoomRendererTemplate = (DataTemplate)DataEditor.FindResource("roomRendererTemplate");

            if (value)
            {
                // DataEditor.ContentTemplate = UndertaleRoomRenderer.RoomRendererTemplate;
                UndertaleCachedImageLoader.ReuseTileBuffer = true;
            }
            else
            {
                // DataEditor.ContentTemplate = null;
                // CurrentTab.CurrentObject = LastOpenedObject;
                // LastOpenedObject = null;
                UndertaleCachedImageLoader.Reset();
                // CachedTileDataLoader.Reset();
            }
        }

        [ObservableProperty] private object lastOpenedObject; // for restoring the object that was opened before room rendering
        
        [ObservableProperty] private bool lsAppClosed = false;
        
        public static string AppDataFolder => Settings.AppDataFolder;
        public static string ProfilesFolder = Path.Combine(Settings.AppDataFolder, "Profiles");
        public static string CorrectionsFolder = Path.Combine(Settings.GetExecutableDirectory() ?? string.Empty, "Corrections");
        public string ProfileHash = "Unknown";
        public bool CrashedWhileEditing = false;

        [ObservableProperty] private string objectLabel  = string.Empty;

        [ObservableProperty] private bool isSelectedProjectExportable;

        [ObservableProperty] private bool isMarkedForExport;

        private bool updatingObjectLabel;

        private object? SelectedAsset => (CurrentTab?.CurrentObject as EditorContentViewModel)?.Asset;

        private object? watchedAsset;
        private readonly List<INotifyPropertyChanged> watchedObjects = new();
        private readonly List<INotifyCollectionChanged> watchedCollections = new();
        private readonly HashSet<object> watchedVisited = new(ReferenceEqualityComparer.Instance);
        private static readonly Dictionary<Type, PropertyInfo[]> watchedProperties = new();
        private static readonly HashSet<string> unwatchedPropertyNames = ["GridWidth", "GridHeight", "ParentRoom", "ParentLayer", "CalcScaleX", "CalcScaleY", "XOffset", "YOffset"];

        private List<ResourceNodeViewModel> unfilteredRootNodes = new();

        public ObservableCollection<ResourceNodeViewModel> FilteredRootNodes { get; } = new();

        [ObservableProperty]
        private ResourceNodeViewModel? selectedNode;

        [ObservableProperty]
        private string searchText = string.Empty;

        public EditorViewModel()
        {
            ApplyFilter(null);
            Highlighted = new Description("Welcome to UndertaleModTool!", "Open a data.win file to get started, then double click on the items on the left to view them.");
            var asset = GetViewModelFromObject(Highlighted);
            OpenInTab(asset, false, asset.MainText);
        }

        public EditorViewModel(ILoadingDialogService loadingDialogService, IProfileService profileService, IDialogService dialogService, IReferenceFinderService referenceFinderService, IPlayer playerService, IFileService fileService, ITextureCacheService textureCacheService, IClipboardService clipboardService, IProjectService projectService)
        {
            this.projectService = projectService;
            projectService.ProjectChanged += (sender, args) =>
            {
                if (projectService.Project is ProjectContext project)
                    project.UnexportedAssetsChanged += (s, a) => Dispatcher.UIThread.Post(() => UpdateObjectLabel(SelectedAsset));
                UpdateObjectLabel(SelectedAsset);
                WatchSelectedAsset();
            };
            this.loadingDialogService = loadingDialogService;
            this.profileService = profileService;
            this.dialogService = dialogService;
            this.referenceFinderService = referenceFinderService;
            this.playerService = playerService;
            this.fileService = fileService;
            this.textureCacheService = textureCacheService;
            this.clipboardService = clipboardService;
            Highlighted = new Description("Welcome to UndertaleModTool!", "Open a data.win file to get started, then double click on the items on the left to view them.");
            var asset = GetViewModelFromObject(Highlighted);
            OpenInTab(asset, false, asset.MainText);
            
            WeakReferenceMessenger.Default.Register<OpenAssetMessage>(this, (recipient, message) =>
            {
                if (message.InNewTab)
                    OpenAssetInNewTab(message.Asset);
                else
                    OpenAssetInTab(message.Asset);

                if (message.SelectedObject is not null && CurrentTab?.CurrentObject is UndertaleRoomEditorViewModel roomEditor)
                    roomEditor.SelectedObject = message.SelectedObject;
            });
            WeakReferenceMessenger.Default.Register<OpenCodeLineMessage>(this, (recipient, message) => OpenCodeLine(message));
            WeakReferenceMessenger.Default.Register<FindReferencesMessage>(this, (recipient, message) =>
            {
                FindAllReferencesCommand.Execute(message.Asset);
            });

            _ = new Settings();

            Settings.Instance.CanSave = false;
            Settings.Instance.CanSafelySave = false;
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter(AppConstants.Data!);
        }

        //private void BuildChildren(ResourceNodeViewModel root, string category, IEnumerable<UndertaleObject> children)
        //{

        //    if (children == null || !children.Any())
        //        return;
        //    var cat = new ResourceNodeViewModel(category, null, this, referenceFinderService, false, children);
        //    root.Children.Add(cat);
        //}

        private void BuildChildren(ResourceNodeViewModel root, string category, IEnumerable<UndertaleObject> children, bool virtualize = true)
        {
            // We pass the children to the constructor only if we want virtualization.
            var cat = new ResourceNodeViewModel(category, null, this, referenceFinderService, false, (virtualize ? children : null));

            if (children == null || !children.Any())
            {
                root.Children.Add(cat);
                return;
            }

            // If we are NOT virtualizing (i.e., showing search results),
            // we create all the child nodes immediately.
            if (!virtualize)
            {
                foreach (var child in children)
                {
                    cat.Children.Add(new ResourceNodeViewModel(child.ToString(), child, this, referenceFinderService));
                }

                cat.MarkAsLoaded();
            }

            root.Children.Add(cat);
        }

        public void BuildTree(UndertaleData data)
        {
            var allNodes = new List<ResourceNodeViewModel>();
            var dataNode = new ResourceNodeViewModel("Data", null, this, referenceFinderService, true);
            
            if (data?.GeneralInfo != null)
                dataNode.Children.Add(new ResourceNodeViewModel("General info", data.GeneralInfo, this, referenceFinderService));
            if (data?.GlobalInitScripts != null)
                dataNode.Children.Add(new ResourceNodeViewModel("Global init", data.GlobalInitScripts, this, referenceFinderService));
            if (data?.GameEndScripts != null)
                dataNode.Children.Add(new ResourceNodeViewModel("Game End scripts", data.GameEndScripts, this, referenceFinderService));

            BuildChildren(dataNode, "Audio groups", data != null ? data.AudioGroups : []);
            BuildChildren(dataNode, "Sounds", data != null ? data.Sounds : []);
            BuildChildren(dataNode, "Sprites", data != null ? data.Sprites : []);
            BuildChildren(dataNode, "Backgrounds & Tile sets", data != null ? data.Backgrounds : []);
            BuildChildren(dataNode, "Paths", data != null ? data.Paths : []);
            BuildChildren(dataNode, "Scripts", data != null ? data.Scripts : []);
            BuildChildren(dataNode, "Shaders", data != null ? data.Shaders : []);
            BuildChildren(dataNode, "Fonts", data != null ? data.Fonts: []);
            BuildChildren(dataNode, "Timelines", data != null ? data.Timelines : []);
            BuildChildren(dataNode, "Game objects", data != null ? data.GameObjects : []);
            BuildChildren(dataNode, "Rooms", data != null ? data.Rooms : []);
            BuildChildren(dataNode, "Extensions", data != null ? data.Extensions : []);
            BuildChildren(dataNode, "Texture page items", data != null ? data.TexturePageItems : []);
            BuildChildren(dataNode, "Code", data != null ? data.Code : []);
            BuildChildren(dataNode, "Variables", data != null ? data.Variables : []);
            BuildChildren(dataNode, "Functions", data != null ? data.Functions : []);
            BuildChildren(dataNode, "Code locals", data != null ? data.CodeLocals : []);
            BuildChildren(dataNode, "Strings", data != null ? data.Strings : []);
            BuildChildren(dataNode, "Embedded textures", data != null ? data.EmbeddedTextures : []);
            BuildChildren(dataNode, "Embedded audio", data != null ? data.EmbeddedAudio : []);
            BuildChildren(dataNode, "Texture group information", data != null ? data.TextureGroupInfo : []);
            BuildChildren(dataNode, "Embedded images", data != null ? data.EmbeddedImages : []);
            BuildChildren(dataNode, "Particle systems", data != null ? data.ParticleSystems : []);
            BuildChildren(dataNode, "Particle system emitters", data != null ? data.ParticleSystemEmitters : []);
            allNodes.Add(dataNode);
            unfilteredRootNodes = allNodes;
        }

        //private void ApplyFilter()
        //{
        //    FilteredRootNodes.Clear();

        //    if (string.IsNullOrWhiteSpace(SearchText))
        //    {
        //        // If search is empty, show the entire unfiltered tree
        //        foreach (var node in unfilteredRootNodes)
        //        {
        //            FilteredRootNodes.Add(node);
        //        }
        //    }
        //    else
        //    {
        //        // If there is search text, build a new filtered tree
        //        var filteredNodes = FilterNodeList(unfilteredRootNodes);
        //        foreach (var node in filteredNodes)
        //        {
        //            FilteredRootNodes.Add(node);
        //        }
        //    }
        //}

        private void ApplyFilter(UndertaleData data)
        {
            FilteredRootNodes.Clear();
            unfilteredRootNodes.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                BuildTree(data);
                foreach (var node in unfilteredRootNodes)
                {
                    FilteredRootNodes.Add(node);
                }
            }
            else
            {
                var searchRootNode = new ResourceNodeViewModel("Search Results", null, this, referenceFinderService, true);

                if (AppConstants.Data != null)
                {
                    BuildChildren(searchRootNode, "Sounds", data != null ? data.Sounds?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Sprites", data != null ? data.Sprites?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Backgrounds & Tile sets", data != null ? data.Backgrounds?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Paths", data != null ? data.Paths?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Scripts", data != null ? data.Scripts?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Shaders", data != null ? data.Shaders?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Fonts", data != null ? data.Fonts?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Game objects", data != null ? data.GameObjects?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Rooms", data != null ? data.Rooms?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : [], false);
                    BuildChildren(searchRootNode, "Extensions", data != null ? data.Extensions?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Texture page items", data != null ? data.TexturePageItems?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Code", data != null ? data.Code?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Variables", data != null ? data.Variables?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Functions", data != null ? data.Functions?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Code locals", data != null ? data.CodeLocals?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Strings", data != null ? data.Strings?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Embedded textures", data != null ? data.EmbeddedTextures?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Embedded audio", data != null ? data.EmbeddedAudio?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Texture group information", data != null ? data.TextureGroupInfo?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Embedded images", data != null ? data.EmbeddedImages?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Particle systems", data != null ? data.ParticleSystems?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                    BuildChildren(searchRootNode, "Particle system emitters", data != null ? data.ParticleSystemEmitters?.Where(x => x.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)) : []);
                }

                FilteredRootNodes.Add(searchRootNode);
            }
        }

        public void ReloadTree()
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                ApplyFilter(AppConstants.Data!);
                return;
            }

            foreach (ResourceNodeViewModel root in FilteredRootNodes)
            {
                foreach (ResourceNodeViewModel category in root.Children)
                {
                    if (category.SourceList is not null)
                        category.ReloadChildren();
                }
            }
        }

        private List<ResourceNodeViewModel> FilterNodeList(IEnumerable<ResourceNodeViewModel> sourceList)
        {
            var filteredList = new List<ResourceNodeViewModel>();

            foreach (var node in sourceList)
            {
                var filteredChildren = FilterNodeList(node.Children);

                bool selfIsMatch = node.Header.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

                if (selfIsMatch || filteredChildren.Any())
                {
                    var newNode = new ResourceNodeViewModel(node.Header, node.Model, this, referenceFinderService);

                    foreach (var child in filteredChildren)
                    {
                        newNode.Children.Add(child);
                    }

                    filteredList.Add(newNode);
                }
            }

            return filteredList;
        }
        
        private static readonly string[] dataFileNames = ["data.win", "game.unx", "game.ios", "game.droid"];
        
        public async Task<bool> LoadFolderAsync(IStorageFolder? folder, bool preventClose = false, bool onlyGeneralInfo = false)
        {
            if (folder is not { }) return false;

            IStorageFile? dataFile = null;
            int dataFileRank = dataFileNames.Length;
            await foreach (var item in folder.GetItemsAsync())
            {
                if (item is not IStorageFile file)
                    continue;
                int rank = Array.FindIndex(dataFileNames, name => name.Equals(file.Name, StringComparison.OrdinalIgnoreCase));
                if (rank >= 0 && rank < dataFileRank)
                {
                    dataFile = file;
                    dataFileRank = rank;
                }
            }

            if (dataFile is null)
            {
                await App.Current!.ShowError("No GameMaker data file (" + string.Join(", ", dataFileNames) + ") was found in:\n" + (folder.TryGetLocalPath() ?? folder.Name), "Load error");
                return false;
            }
            return await LoadFileAsync(dataFile, preventClose, onlyGeneralInfo);
        }

        public async Task<bool> LoadFileAsync(IStorageFile? dataFile, bool preventClose = false, bool onlyGeneralInfo = false)
        {
            if (dataFile is not { }) return false;
            
            string filePath = dataFile.TryGetLocalPath() ?? dataFile.Name;

            if (AppConstants.Data is not null)
                projectService?.UnloadProject();

            IsEnabled = false;
            loadingDialogService.Show("Loading File", "Reading " + dataFile.Name + ", please wait...");
            await loadingDialogService.SetIndeterminateAsync(true);

            GameSpecificResolver.BaseDirectory = Settings.GetExecutableDirectory();

            bool loaded = await Task.Run<bool>(async () =>
            {
                bool hadImportantWarnings = false;
                UndertaleData data = null;
                try
                {
                    using (var stream = await dataFile.OpenReadAsync())
                    {
                        var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        data = UndertaleIO.Read(memoryStream, (string warning, bool isImportant) =>
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                _ = App.Current!.ShowWarning(warning, "Loading warning");
                            });
                            if (isImportant)
                            {
                                hadImportantWarnings = true;
                            }
                        }, message =>
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                loadingDialogService.UpdateStatusAsync(message);
                            });
                        }, onlyGeneralInfo);
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    Debug.WriteLine(e);
#endif
                    Console.WriteLine(e.Message + "\n" + e.StackTrace);
                    Dispatcher.UIThread.Post(() =>
                    {
                        loadingDialogService.Hide();
                    });
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await App.Current!.ShowError("An error occurred while trying to load:\n" + e.Message, "Load error");
                    });
                    return false;
                }

                if (onlyGeneralInfo)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        loadingDialogService.Hide();
                        AppConstants.Data = data;
                        AppConstants.FilePath = filePath;
                    });

                    return true;
                }

                await Dispatcher.UIThread.Invoke(async () =>
                {
                    if (data != null)
                    {
                        if (data.UnsupportedBytecodeVersion)
                        {
                            await App.Current!.ShowWarning("Only bytecode versions 13 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.", "Unsupported bytecode version");
                            Settings.Instance.CanSave = false;
                            Settings.Instance.CanSafelySave = false;
                        }
                        else if (hadImportantWarnings)
                        {
                            await App.Current!.ShowWarning("Warnings occurred during loading. Data loss will likely occur when trying to save!", "Loading problems");
                            Settings.Instance.CanSave = true;
                            Settings.Instance.CanSafelySave = false;
                        }
                        else
                        {
                            Settings.Instance.CanSave = true;
                            Settings.Instance.CanSafelySave = true;
                            await profileService.UpdateProfileAsync(data, dataFile);
                            if (data != null)
                            {
                                data.ToolInfo.DecompilerSettings = Settings.Instance.DecompilerSettings;
                                data.ToolInfo.InstanceIdPrefix = () => Settings.Instance.InstanceIdPrefix;
                            }
                        }
                        if (data.IsYYC())
                        {
                            await App.Current!.ShowWarning("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.", "YYC");
                        }
                        if (data.GeneralInfo != null)
                        {
                            if (!data.GeneralInfo.IsDebuggerDisabled)
                            {
                                await App.Current!.ShowWarning("This game is set to run with the GameMaker Studio debugger and the normal runtime will simply hang after loading if the debugger is not running. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger");
                            }
                        }
                        if (!OperatingSystem.IsBrowser() && Path.GetDirectoryName(AppConstants.FilePath) != Path.GetDirectoryName(filePath))
                            UndertaleHelper.CloseChildFiles();

                        AppConstants.Data = data;
                        AppConstants.FilePath = filePath;
                        ResetTabs();

                        UndertaleCachedImageLoader.Reset();
                        // CachedTileDataLoader.Reset();

                        AppConstants.Data.ToolInfo.DecompilerSettings = Settings.Instance.DecompilerSettings;
                        AppConstants.Data.ToolInfo.InstanceIdPrefix = () => Settings.Instance.InstanceIdPrefix;
                        OnPropertyChanged(nameof(AppConstants.Data));
                        OnPropertyChanged(nameof(AppConstants.FilePath));
                        OnPropertyChanged(nameof(UndertaleHelper.IsGMS2));

                        /*BackgroundsItemsList.Header = IsGMS2 == Visibility.Visible
                                                      ? "Tile sets"
                                                      : "Backgrounds & Tile sets";*/

                        /*UndertaleCodeEditor.gettext = null;
                        UndertaleCodeEditor.gettextJSON = null;*/
                    }
                });
                return true;
            });
            loadingDialogService.Hide();
            IsEnabled = true;
            if (!loaded)
                return false;
            // Clear "GC holes" left in the memory in process of data unserializing
            // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-6.0
            if (!OperatingSystem.IsBrowser())
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }
            ApplyFilter(AppConstants.Data);
            WeakReferenceMessenger.Default.Send(new TitleUpdateMessage());
            return true;
        }
        
        public async Task<bool> SaveFileAsync(string filename, bool suppressDebug = false)
        {
            if (AppConstants.Data == null || AppConstants.Data.UnsupportedBytecodeVersion)
                return false;
            
            // --- Start Saving Process ---
            loadingDialogService.Show("Saving File", "Saving " + Path.GetFileName(filename) + ", please wait...");
            await loadingDialogService.SetIndeterminateAsync(true);

            IProgress<Tuple<int, string, double>> progress = new Progress<Tuple<int, string, double>>(i => { loadingDialogService.UpdateStatusAsync(i.Item2); loadingDialogService.UpdateProgressAsync(i.Item1, i.Item3); });

            if (Path.GetDirectoryName(AppConstants.FilePath) != Path.GetDirectoryName(filename))
                UndertaleHelper.CloseChildFiles();

            DebugDataDialogViewModel.DebugDataMode debugMode = DebugDataDialogViewModel.DebugDataMode.NoDebug;
            if (!suppressDebug && AppConstants.Data.GeneralInfo != null && !AppConstants.Data.GeneralInfo.IsDebuggerDisabled)
                await App.Current!.ShowWarning("You are saving the game in GameMaker Studio debug mode. Unless the debugger is running, the normal runtime will simply hang after loading. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger");
            Task<bool> t = Task.Run(async () =>
            {
                if (Settings.Instance.RecompileAllCodeSourcesOnProjectSave && projectService?.Project is ProjectContext project)
                {
                    try
                    {
                        await loadingDialogService.UpdateStatusAsync("Recompiling GML code, please wait...");
                        project.RecompileAllCodeSources();
                        await loadingDialogService.UpdateStatusAsync("Saving data file, please wait...");
                    }
                    catch (ProjectException e)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            loadingDialogService.Hide();
                            await App.Current!.ShowError(e.Message, "Recompile error");
                        });
                        return false;
                    }
                    catch (Exception e)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            loadingDialogService.Hide();
                            await App.Current!.ShowError("An error occurred while trying to recompile code sources:\n" + e.Message, "Recompile error");
                        });
                        return false;
                    }
                }

                bool SaveSucceeded = true;

                try
                {
                    using (var stream = new FileStream(filename + "temp", FileMode.Create, FileAccess.Write))
                    {
                        UndertaleIO.Write(stream, AppConstants.Data, message =>
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                loadingDialogService.UpdateStatusAsync(message);
                            });
                        });
                    }

                    if (debugMode != DebugDataDialogViewModel.DebugDataMode.NoDebug)
                    {
                        await loadingDialogService.UpdateStatusAsync("Generating debugger data...");

                        UndertaleDebugData debugData = UndertaleDebugData.CreateNew();

                        // setMax.Report(,,AppConstants.Data.Code.Count);
                        int count = 0;
                        object countLock = new object();
                        string[] outputs = new string[AppConstants.Data.Code.Count];
                        UndertaleDebugInfo[] outputsOffsets = new UndertaleDebugInfo[AppConstants.Data.Code.Count];
                        GlobalDecompileContext context = new(AppConstants.Data);
                        Parallel.For(0, AppConstants.Data.Code.Count, (i) =>
                        {
                            var code = AppConstants.Data.Code[i];

                            if (debugMode == DebugDataDialogViewModel.DebugDataMode.Decompiled)
                            {
                                //Debug.WriteLine("Decompiling " + code.Name.Content);
                                string output;
                                try
                                {
                                    output = new Underanalyzer.Decompiler.DecompileContext(context, code, AppConstants.Data.ToolInfo.DecompilerSettings)
                                        .DecompileToString();
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e.Message);
                                    output = "/*\nEXCEPTION!\n" + e.ToString() + "\n*/";
                                }
                                outputs[i] = output;

                                UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();
                                debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = 0, BytecodeOffset = 0 }); // TODO: generate this too! :D
                                outputsOffsets[i] = debugInfo;
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();
                                UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();

                                uint addr = 0;
                                foreach (var instr in code.Instructions)
                                {
                                    if (debugMode == DebugDataDialogViewModel.DebugDataMode.FullAssembler || instr.Kind == UndertaleInstruction.Opcode.Pop || instr.Kind == UndertaleInstruction.Opcode.Popz || instr.Kind == UndertaleInstruction.Opcode.B || instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf || instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                                        debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = (uint)sb.Length, BytecodeOffset = addr * 4 });
                                    instr.ToString(sb, code, addr);
                                    addr += instr.CalculateInstructionSize();
                                    sb.Append('\n');
                                }
                                outputs[i] = sb.ToString();
                                outputsOffsets[i] = debugInfo;
                            }

                            lock (countLock)
                            {
                                progress.Report(new Tuple<int, string, double>(++count, code.Name.Content, AppConstants.Data.Code.Count));
                            }
                        });

                        for (int i = 0; i < AppConstants.Data.Code.Count; i++)
                        {
                            debugData.SourceCode.Add(new UndertaleScriptSource() { SourceCode = debugData.Strings.MakeString(outputs[i]) });
                            debugData.DebugInfo.Add(outputsOffsets[i]);
                            // FIXME: Probably should write something regardless.
                            if (AppConstants.Data.CodeLocals is not null)
                            {
                                debugData.LocalVars.Add(AppConstants.Data.CodeLocals[i]);
                                if (debugData.Strings.IndexOf(AppConstants.Data.CodeLocals[i].Name) < 0)
                                    debugData.Strings.Add(AppConstants.Data.CodeLocals[i].Name);
                                foreach (var local in AppConstants.Data.CodeLocals[i].Locals)
                                    if (debugData.Strings.IndexOf(local.Name) < 0)
                                        debugData.Strings.Add(local.Name);
                            }
                        }

                        using (UndertaleWriter writer = new UndertaleWriter(new FileStream(Path.ChangeExtension(filename, ".yydebug"), FileMode.Create, FileAccess.Write)))
                        {
                            debugData.FORM.Serialize(writer);
                            writer.ThrowIfUnwrittenObjects();
                            writer.Flush();
                        }
                    }
                }
                catch (Exception e)
                {
                    await Dispatcher.UIThread.Invoke(async () =>
                    {
                        await App.Current!.ShowError("An error occurred while trying to save:\n" + e.Message, "Save error");
                    });

                    SaveSucceeded = false;
                }
                // Don't make any changes unless the save succeeds.
                try
                {
                    if (SaveSucceeded)
                    {
                        // It saved successfully!
                        // If we're overwriting a previously existing data file, we're going to overwrite it now.
                        // Then, we're renaming it back to the proper (non-temp) file name.
                        File.Move(filename + "temp", filename, true);

                        // Also make the changes to the profile system.
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            await profileService.ProfileSaveEventAsync(AppConstants.Data, filename);
                        });
                    }
                    else
                    {
                        // It failed, but since we made a temp file for saving, no data was overwritten or destroyed (hopefully)
                        // We need to delete the temp file though (if it exists).
                        if (File.Exists(filename + "temp"))
                            File.Delete(filename + "temp");
                        // No profile system changes, since the save failed, like a save was never attempted.
                    }
                }
                catch (Exception exc)
                {
                    await Dispatcher.UIThread.Invoke(async () =>
                    {
                        await App.Current!.ShowError("An error occurred while trying to save:\n" + exc.Message, "Save error");
                    });

                    SaveSucceeded = false;
                }

                // TODO UndertaleCodeEditor.gettextJSON = null;

                Dispatcher.UIThread.Invoke(() =>
                {
                    loadingDialogService.Hide();
                });
                return SaveSucceeded;
            });

            bool saved = await t;
            
            if (saved)
            {
                AppConstants.FilePath = filename;
                WeakReferenceMessenger.Default.Send(new TitleUpdateMessage());
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            return saved;
        }
        
        public void NewFile()
        {
            UndertaleHelper.CloseChildFiles();
            projectService?.UnloadProject();

            AppConstants.Data = UndertaleData.CreateNew();
            AppConstants.Data.ToolInfo.DecompilerSettings = Settings.Instance.DecompilerSettings;
            AppConstants.Data.ToolInfo.InstanceIdPrefix = () => Settings.Instance.InstanceIdPrefix;
            AppConstants.FilePath = null;
            Settings.Instance.CanSave = true;
            Settings.Instance.CanSafelySave = true;

            UndertaleCachedImageLoader.Reset();
            ResetTabs();
            ApplyFilter(AppConstants.Data);
            WeakReferenceMessenger.Default.Send(new TitleUpdateMessage());
        }
        
        private void ResetTabs()
        {
            Tabs.Clear();
            ClosedTabsHistory.Clear();
            CurrentTab = null;
            var asset = GetViewModelFromObject(Highlighted);
            OpenInTab(asset, false, asset.MainText);
        }

        private void DisposeGameData()
        {
            if (AppConstants.Data is not null)
            {
                // This also clears all their game object references
                // CurrentTab = null;
                // Tabs.Clear();
                // ClosedTabsHistory.Clear();

                // Update GUI and wait for all background processes to finish
                OnPropertyChanged(); // Replaces the Update Layout to let Avalonia know to update the UI.
                Dispatcher.UIThread.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                AppConstants.Data.Dispose();
                AppConstants.Data = null;

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }
        }

        [RelayCommand]
        private async Task CopyItemToClipboard(ResourceNodeViewModel? node)
        {
            string? name = null;

            if (node?.Model is UndertaleNamedResource namedRes)
                name = namedRes.Name?.Content;
            else if (node?.Model is UndertaleString str && str.Content?.Length > 0)
                name = StringTitleConverter.Instance.Convert(str.Content, null, null, null) as string;

            if (name is null)
            {
                await App.Current!.ShowWarning("Item name is null.");
                return;
            }

            try
            {
                await clipboardService.SetTextAsync(name);
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("Can't copy the item name to clipboard due to this error:\n" +
                                     ex.Message + ".\nYou probably should try again.");
            }
        }

        [RelayCommand]
        private async Task AddNewItem(ResourceNodeViewModel? categoryNode)
        {
            if (categoryNode is null || !categoryNode.CanAddItem) return;
            if (categoryNode.SourceList is not IList list) return;

            Type t = list.GetType().GetGenericArguments()[0];
            UndertaleResource obj = Activator.CreateInstance(t) as UndertaleResource;
            if (obj is UndertaleNamedResource namedResource)
            {
                bool doMakeString = obj is not (UndertaleTexturePageItem or UndertaleEmbeddedAudio or UndertaleEmbeddedTexture);
                string notDataNewName = null;
                if (obj is UndertaleTexturePageItem)
                {
                    notDataNewName = "PageItem " + list.Count;
                }
                if (obj is UndertaleEmbeddedAudio)
                {
                    notDataNewName = "EmbeddedSound " + list.Count;
                }
                if (obj is UndertaleEmbeddedTexture)
                {
                    notDataNewName = "Texture " + list.Count;
                }

                if (doMakeString)
                {
                    string assetTypeName = obj.GetType().Name.Replace("Undertale", "").Replace("GameObject", "Object").ToLower();
                    string newName = $"{assetTypeName}{list.Count}";
                    string? userNewName = await dialogService.ShowDialogAsync<TextInputViewModel, TextInputParameters, string>(new TextInputParameters($"Choose new {assetTypeName} name", "Name of new asset:", newName));
                    if (userNewName is null)
                    {
                        return;
                    }
                    if (IsValidAssetIdentifier(userNewName))
                    {
                        newName = userNewName;
                    }
                    else
                    {
                        if (await App.Current!.ShowQuestionWithCancel($"Asset name \"{userNewName}\" is not a valid identifier. Add a new asset using an auto-generated name instead?",
                            MsBox.Avalonia.Enums.Icon.Warning, "Invalid name") != MsBox.Avalonia.Enums.ButtonResult.Yes)
                        {
                            return;
                        }
                    }
                    namedResource.Name = AppConstants.Data.Strings.MakeString(newName);
                    if (obj is UndertaleRoom roomResource)
                    {
                        if (AppConstants.Data.IsGameMaker2())
                        {
                            roomResource.Caption = null;
                            roomResource.Backgrounds.Clear();
                            if (AppConstants.Data.IsVersionAtLeast(2024, 13))
                            {
                                roomResource.Flags |= UndertaleRoom.RoomEntryFlags.IsGM2024_13;
                                roomResource.InstanceCreationOrderIDs ??= new();
                            }
                            else
                            {
                                roomResource.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
                                if (AppConstants.Data.IsVersionAtLeast(2, 3))
                                {
                                    roomResource.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2_3;
                                }
                            }
                        }
                        else
                        {
                            roomResource.Caption = AppConstants.Data.Strings.MakeString("");
                        }

                        if (await App.Current!.ShowQuestion("Add the new room to the end of the room order list?", MsBox.Avalonia.Enums.Icon.Question, "Add to room order list") == MsBox.Avalonia.Enums.ButtonResult.Yes)
                        {
                            AppConstants.Data.GeneralInfo.RoomOrder.Add(new(roomResource));
                        }
                    }
                    else if (obj is UndertaleScript scriptResource)
                    {
                        if (AppConstants.Data.IsVersionAtLeast(2, 3))
                        {
                            scriptResource.Code = UndertaleCode.CreateEmptyEntry(AppConstants.Data, $"gml_GlobalScript_{newName}");
                            if (AppConstants.Data.GlobalInitScripts is IList<UndertaleGlobalInit> globalInitScripts)
                            {
                                globalInitScripts.Add(new UndertaleGlobalInit()
                                {
                                    Code = scriptResource.Code
                                });
                            }
                        }
                        else
                        {
                            scriptResource.Code = UndertaleCode.CreateEmptyEntry(AppConstants.Data, $"gml_Script_{newName}");
                        }

                        projectService?.Project?.MarkAssetForExport(scriptResource.Code);
                    }
                    else if (obj is UndertaleCode codeResource)
                    {
                        if (AppConstants.Data.CodeLocals is not null)
                        {
                            codeResource.LocalsCount = 1;
                            UndertaleCodeLocals.CreateEmptyEntry(AppConstants.Data, codeResource.Name);
                        }
                        else
                        {
                            codeResource.WeirdLocalFlag = true;
                        }
                    }
                    else if (obj is UndertaleExtension extension)
                    {
                        extension.FolderName = AppConstants.Data.Strings.MakeString("");
                        extension.ClassName = AppConstants.Data.Strings.MakeString("");
                        extension.Version = AppConstants.Data.Strings.MakeString("1.0.0");

                        if (UndertaleExtension.ProductDataEligible(AppConstants.Data))
                        {
                            var newProductID = new byte[] { 0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60, 0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE };
                            AppConstants.Data.FORM.EXTN.productIdData.Add(newProductID);
                        }
                    }
                    else if (obj is UndertaleShader shader)
                    {
                        shader.GLSL_ES_Vertex = AppConstants.Data.Strings.MakeString("", true);
                        shader.GLSL_ES_Fragment = AppConstants.Data.Strings.MakeString("", true);
                        shader.GLSL_Vertex = AppConstants.Data.Strings.MakeString("", true);
                        shader.GLSL_Fragment = AppConstants.Data.Strings.MakeString("", true);
                        shader.HLSL9_Vertex = AppConstants.Data.Strings.MakeString("", true);
                        shader.HLSL9_Fragment = AppConstants.Data.Strings.MakeString("", true);
                    }
                    else if (obj is UndertaleSound soundResource)
                    {
                        soundResource.Flags = UndertaleSound.AudioEntryFlags.IsEmbedded | UndertaleSound.AudioEntryFlags.Regular;
                        soundResource.GroupID = 0;
                    }
                }
                else
                {
                    namedResource.Name = new UndertaleString(notDataNewName);
                }
            }
            else if (obj is UndertaleString str)
            {
                str.Content = "string" + list.Count;
            }
            list.Add(obj);

            if (projectService?.Project is not null && obj is IProjectAsset projectAsset)
            {
                projectService.Project.MarkAssetForExport(projectAsset);
            }

            categoryNode.ReloadChildren();
            OpenAssetInNewTab(obj);
        }

        private static bool IsValidAssetIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            char firstChar = name[0];
            if (!char.IsAsciiLetter(firstChar) && firstChar != '_')
            {
                return false;
            }
            foreach (char c in name.Skip(1))
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        [RelayCommand]
        private async Task DeleteItem(ResourceNodeViewModel? resourceNode)
        {
            if (resourceNode?.Model is not UndertaleResource resource) return;
            if (AppConstants.Data?[resource.GetType()] is not IList list || !list.Contains(resource)) return;

            bool isLast = list.IndexOf(resource) == list.Count - 1;
            if (await App.Current!.ShowQuestion("Delete " + resource + "?" + (!isLast ? "\n\nNote that the code often references objects by ID, so this operation is likely to break stuff because other items will shift up!" : ""),
                isLast ? MsBox.Avalonia.Enums.Icon.Question : MsBox.Avalonia.Enums.Icon.Warning, "Confirmation") != MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                return;
            }

            list.Remove(resource);

            if (projectService?.Project is not null && resource is IProjectAsset projectAsset)
            {
                projectService.Project.UnmarkAssetForExport(projectAsset);
            }

            RemoveAssetFromTabs(resource);
            RemoveNodeFromTree(resourceNode, list);
        }

        private void RemoveAssetFromTabs(object asset)
        {
            bool IsAsset(object obj) => obj is EditorContentViewModel content && ReferenceEquals(content.Asset, asset);

            foreach (TabViewModel tab in Tabs.Where(x => IsAsset(x.CurrentObject)).ToList())
            {
                CloseTab(tab, Tabs.Count == 1);
            }

            ClosedTabsHistory.RemoveAll(x => IsAsset(x.CurrentObject));

            foreach (TabViewModel tab in Tabs.Concat(ClosedTabsHistory))
            {
                for (int i = tab.History.Count - 1; i >= 0; i--)
                {
                    if (!IsAsset(tab.History[i]))
                        continue;

                    if (i < tab.HistoryPosition)
                        tab.HistoryPosition--;

                    tab.History.RemoveAt(i);
                }

                for (int i = tab.History.Count - 1; i > 0; i--)
                {
                    if (tab.History[i] is not EditorContentViewModel { Asset: not null } current
                        || tab.History[i - 1] is not EditorContentViewModel previous
                        || !ReferenceEquals(current.Asset, previous.Asset))
                        continue;

                    int duplicate = tab.History[i] == tab.CurrentObject ? i - 1 : i;
                    if (duplicate < tab.HistoryPosition)
                        tab.HistoryPosition--;

                    tab.History.RemoveAt(duplicate);
                }
            }

            NotifyHistoryChanged();
        }

        private void RemoveNodeFromTree(ResourceNodeViewModel resourceNode, IList list)
        {
            ResourceNodeViewModel? category = FindNode(FilteredRootNodes, x => x.CanAddItem && ReferenceEquals(x.SourceList, list));
            if (category is not null)
                category.ReloadChildren();
            else
                FindNode(FilteredRootNodes, x => x.Children.Contains(resourceNode))?.Children.Remove(resourceNode);

            if (SelectedNode == resourceNode)
                SelectedNode = null;
        }

        private static ResourceNodeViewModel? FindNode(IEnumerable<ResourceNodeViewModel> nodes, Func<ResourceNodeViewModel, bool> match)
        {
            foreach (ResourceNodeViewModel node in nodes)
            {
                if (match(node))
                    return node;

                ResourceNodeViewModel? found = FindNode(node.Children, match);
                if (found is not null)
                    return found;
            }

            return null;
        }

        [RelayCommand]
        private void OpenAssetInTab(object asset)
        {
            if (asset == null) return;
            if (CurrentTab?.CurrentObject is EditorContentViewModel current && ReferenceEquals(current.Asset, asset))
                return;
            var assetObject = GetViewModelFromObject(asset);
            if (!ReferenceEquals(assetObject, asset))
                assetObject.Asset = asset;
            OpenInTab(assetObject, false, assetObject.MainText);
        }

        private void OpenCodeLine(OpenCodeLineMessage message)
        {
            if (!message.InNewTab && CurrentTab?.CurrentObject is UndertaleCodeEditorViewModel open && ReferenceEquals(open.Asset, message.Code))
            {
                WeakReferenceMessenger.Default.Send(new GoToCodeLineMessage(open, message.Line, message.Tab));
                return;
            }

            if (GetViewModelFromObject(message.Code) is not UndertaleCodeEditorViewModel editor)
                return;
            editor.Asset = message.Code;
            editor.EditorTab = message.Tab;
            if (message.Tab == UndertaleCodeEditorViewModel.CodeEditorTab.Disassembly)
                editor.OverriddenDisasmPos = (message.Line, -1, -1);
            else
                editor.OverriddenDecompPos = (message.Line, -1, -1);
            OpenInTab(editor, message.InNewTab, editor.MainText);
        }

        [RelayCommand]
        private void OpenAssetInNewTab(object asset)
        {
            if (asset == null) return;
            var assetObject = GetViewModelFromObject(asset);
            if (!ReferenceEquals(assetObject, asset))
                assetObject.Asset = asset;
            OpenInTab(assetObject, true, assetObject.MainText);
        }

        [RelayCommand]
        private void CloseAssetTab(TabViewModel tab)
        {
            if (tab != null && Tabs.Count > 0)
            {
                CloseTab(tab, Tabs.Count == 1);
            }
        }

        [RelayCommand]
        private void CloseCurrentTab()
        {
            CloseTab();
        }

        private bool CanCloseOtherTabs(TabViewModel tab) => tab != null && Tabs.Count > 1;

        [RelayCommand(CanExecute = nameof(CanCloseOtherTabs))]
        private void CloseOtherTabs(TabViewModel tab)
        {
            foreach (TabViewModel other in Tabs.Where(t => t != tab).Reverse().ToList())
            {
                other.TabIndex = Tabs.IndexOf(other);
                if (!other.AutoClose)
                    ClosedTabsHistory.Add(other);
                Tabs.Remove(other);
            }

            CurrentTab = tab;
            CloseOtherTabsCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void CloseAllTabs()
        {
            if (Tabs.Count == 1 && CurrentTab is { AutoClose: true })
                return;

            ResetTabs();
        }

        [RelayCommand]
        private void RestoreClosedTab()
        {
            if (ClosedTabsHistory.Count == 0)
                return;

            TabViewModel lastTab = ClosedTabsHistory[^1];
            ClosedTabsHistory.RemoveAt(ClosedTabsHistory.Count - 1);

            if (CurrentTab is { AutoClose: true })
                CloseTab(false);

            Tabs.Insert(Math.Min(lastTab.TabIndex, Tabs.Count), lastTab);
            CurrentTab = lastTab;
            CloseOtherTabsCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void SwitchToNextTab()
        {
            int index = Tabs.IndexOf(CurrentTab);
            if (index >= 0 && index < Tabs.Count - 1)
                CurrentTab = Tabs[index + 1];
        }

        [RelayCommand]
        private void SwitchToPreviousTab()
        {
            int index = Tabs.IndexOf(CurrentTab);
            if (index > 0)
                CurrentTab = Tabs[index - 1];
        }

        private bool CanGoBack() => CurrentTab is { HistoryPosition: > 0 };

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void GoBack()
        {
            CurrentTab.HistoryPosition--;
            ShowHistoryObject();
        }

        private bool CanGoForward() => CurrentTab is not null && CurrentTab.HistoryPosition < CurrentTab.History.Count - 1;

        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void GoForward()
        {
            CurrentTab.HistoryPosition++;
            ShowHistoryObject();
        }

        private void ShowHistoryObject()
        {
            object obj = CurrentTab.History[CurrentTab.HistoryPosition];
            CurrentTab.CurrentObject = obj;
            CurrentTab.TabTitle = GetTitleForObject(obj);
            NotifyHistoryChanged();
        }

        private void NotifyHistoryChanged()
        {
            GoBackCommand.NotifyCanExecuteChanged();
            GoForwardCommand.NotifyCanExecuteChanged();
        }

        partial void OnCurrentTabChanged(TabViewModel? value)
        {
            NotifyHistoryChanged();
            CloseOtherTabsCommand.NotifyCanExecuteChanged();
        }

        partial void OnCurrentTabChanged(TabViewModel? oldValue, TabViewModel? newValue)
        {
            if (oldValue is not null)
                oldValue.PropertyChanged -= CurrentTab_PropertyChanged;
            if (newValue is not null)
                newValue.PropertyChanged += CurrentTab_PropertyChanged;
            UpdateObjectLabel(SelectedAsset);
            WatchSelectedAsset();
        }

        private void CurrentTab_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TabViewModel.CurrentObject))
            {
                UpdateObjectLabel(SelectedAsset);
                WatchSelectedAsset();
            }
        }

        private void WatchSelectedAsset()
        {
            UnwatchAsset();
            if (projectService?.Project is null || SelectedAsset is not IProjectAsset { ProjectExportable: true })
                return;

            watchedAsset = SelectedAsset;
            WatchValue(watchedAsset);
        }

        private void UnwatchAsset()
        {
            foreach (INotifyPropertyChanged obj in watchedObjects)
                obj.PropertyChanged -= WatchedObject_PropertyChanged;
            foreach (INotifyCollectionChanged collection in watchedCollections)
                collection.CollectionChanged -= WatchedCollection_CollectionChanged;
            watchedObjects.Clear();
            watchedCollections.Clear();
            watchedVisited.Clear();
            watchedAsset = null;
        }

        private void WatchValue(object? value)
        {
            if (value is null || value is string || value is UndertaleString)
                return;
            if (value is UndertaleResource && !ReferenceEquals(value, watchedAsset))
                return;
            if (!watchedVisited.Add(value))
                return;

            if (value is INotifyPropertyChanged obj)
            {
                obj.PropertyChanged += WatchedObject_PropertyChanged;
                watchedObjects.Add(obj);
            }

            if (value is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += WatchedCollection_CollectionChanged;
                watchedCollections.Add(collection);
            }

            if (value is IEnumerable items)
            {
                Type? itemType = value.GetType().GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
                if (itemType is null || !itemType.IsValueType)
                {
                    foreach (object? item in items)
                        WatchValue(item);
                }
            }

            if (value is not UndertaleObject)
                return;

            foreach (PropertyInfo property in GetWatchedProperties(value.GetType()))
            {
                object? propertyValue;
                try
                {
                    propertyValue = property.GetValue(value);
                }
                catch (Exception)
                {
                    continue;
                }
                WatchValue(propertyValue);
            }
        }

        private static PropertyInfo[] GetWatchedProperties(Type type)
        {
            if (!watchedProperties.TryGetValue(type, out PropertyInfo[]? properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(x => x.CanRead && x.GetIndexParameters().Length == 0)
                                 .Where(x => typeof(UndertaleObject).IsAssignableFrom(x.PropertyType)
                                             || (typeof(IEnumerable).IsAssignableFrom(x.PropertyType) && x.PropertyType != typeof(string)))
                                 .ToArray();
                watchedProperties[type] = properties;
            }
            return properties;
        }

        private void WatchedObject_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not null && unwatchedPropertyNames.Contains(e.PropertyName))
                return;

            object? asset = watchedAsset;
            Dispatcher.UIThread.Post(() => MarkWatchedAssetForExport(asset));
        }

        private void WatchedCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            object? asset = watchedAsset;
            Dispatcher.UIThread.Post(() =>
            {
                if (e.NewItems is not null && ReferenceEquals(asset, watchedAsset))
                {
                    foreach (object? item in e.NewItems)
                        WatchValue(item);
                }
                MarkWatchedAssetForExport(asset);
            });
        }

        private void MarkWatchedAssetForExport(object? asset)
        {
            if (projectService?.Project is not ProjectContext project || AppConstants.Data is null)
                return;
            if (asset is not IProjectAsset { ProjectExportable: true } projectAsset || asset is not UndertaleResource resource)
                return;
            if (AppConstants.Data.IndexOf(resource, false) < 0)
                return;

            project.MarkAssetForExport(projectAsset);
        }

        public void UpdateObjectLabel(object? obj)
        {
            int foundIndex = obj is UndertaleResource res && AppConstants.Data is not null ? AppConstants.Data.IndexOf(res, false) : -1;

            string idString;
            if (foundIndex == -2)
            {
                idString = "None";
            }
            else if (foundIndex == -1)
            {
                idString = "N/A";
            }
            else
            {
                idString = foundIndex.ToString();
            }

            ObjectLabel = $"ID: {idString}";

            updatingObjectLabel = true;
            if (projectService?.Project is ProjectContext project && foundIndex >= 0 && obj is IProjectAsset { ProjectExportable: true } projectAsset)
            {
                IsSelectedProjectExportable = true;
                IsMarkedForExport = project.IsAssetMarkedForExport(projectAsset);
            }
            else
            {
                IsSelectedProjectExportable = false;
            }
            updatingObjectLabel = false;
        }

        partial void OnIsMarkedForExportChanged(bool value)
        {
            if (updatingObjectLabel)
                return;
            if (projectService?.Project is not ProjectContext project)
                return;
            if (SelectedAsset is not IProjectAsset projectAsset)
                return;

            if (value)
            {
                project.MarkAssetForExport(projectAsset);
            }
            else
            {
                project.UnmarkAssetForExport(projectAsset);
            }
        }

        [RelayCommand]
        private void DeleteAsset(object asset)
        {

        }

        [RelayCommand]
        private async Task FindAllTileReferences(object sender)
        {
            if (sender is not (UndertaleBackground, UndertaleBackground.TileID))
            {
                await App.Current!.ShowError("The selected object is not an \"UndertaleObject\".");
                return;
            }
            var obj = ((UndertaleBackground, UndertaleBackground.TileID))sender;
            var tileSet = obj.Item1;
            var selectedID = obj.Item2;
            try
            {
                IsEnabled = false;
                var typeList = new HashSetTypesOverride() { typeof(UndertaleRoom.Layer) };
                var tuple = (tileSet, selectedID);
                var results = referenceFinderService.GetReferencesOfObject(tuple, AppConstants.Data!, typeList);
                await dialogService.ShowAsync<FindReferencesResultsViewModel, FindReferencesResultsParameters>(new FindReferencesResultsParameters(tuple, AppConstants.Data!, results));
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("An error occurred in the object references related window.\n" +
                                    $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                IsEnabled = true;
            }
        }

        [RelayCommand]
        private async Task FindAllReferences(object sender)
        {
            if (sender is not UndertaleObject res)
            {
                await App.Current!.ShowError($"The selected object is not an \"UndertaleObject\". {sender}");
                return;
            }

            try
            {
                IsEnabled = false;
                await dialogService.ShowAsync<FindReferencesTypesDialogViewModel, UndertaleReferenceParameters>(new UndertaleReferenceParameters(res, AppConstants.Data!));
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("An error occurred in the object references related window.\n" +
                                     $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private EditorContentViewModel GetViewModelFromObject(object asset)
        {
            if (asset is EditorContentViewModel editorContent)
                return editorContent;
            if (asset is Description)
                return new DescriptionViewModel(GetTitleForObject(asset) ?? "Unknown", (Description)asset);
            if (asset is UndertaleGeneralInfo)
                return new UndertaleGeneralInfoEditorViewModel(GetTitleForObject(asset) ?? "Unknown", (UndertaleGeneralInfo)asset, AppConstants.Data!.Options, AppConstants.Data.Language);
            if (asset is IList<UndertaleGlobalInit> globalInits)
            {
                string? title = GetTitleForObject(asset);
                if (title == "Global Init")
                    return new UndertaleGlobalInitEditorViewModel(title, globalInits);
                if (title == "Game End")
                    return new UndertaleGameEndEditorViewModel(title, globalInits);
            }
            if (asset is UndertaleObject dataRes)
            {
                string name = GetTitleForObject(asset) ?? "Unknown";
                EditorContentViewModel? model = dataRes switch
                {
                    UndertaleAudioGroup => new UndertaleAudioGroupEditorViewModel(name, (UndertaleAudioGroup)asset),
                    UndertaleSound => new UndertaleSoundEditorViewModel(name, (UndertaleSound)asset, playerService),
                    UndertaleSprite => new UndertaleSpriteEditorViewModel(name, (UndertaleSprite)asset, this, fileService, textureCacheService),
                    UndertaleBackground => new UndertaleBackgroundEditorViewModel(name, (UndertaleBackground)asset, this, textureCacheService, fileService),
                    UndertalePath => new UndertalePathEditorViewModel(name, (UndertalePath)asset),
                    UndertaleScript => new UndertaleScriptEditorViewModel(name, (UndertaleScript)asset),
                    UndertaleShader => new UndertaleShaderEditorViewModel(name, (UndertaleShader)asset),
                    UndertaleFont => new UndertaleFontEditorViewModel(name, (UndertaleFont)asset, this, textureCacheService, fileService, dialogService),
                    UndertaleTimeline => new UndertaleTimelineEditorViewModel(name, (UndertaleTimeline)asset),
                    UndertaleGameObject => new UndertaleGameObjectEditorViewModel(name, (UndertaleGameObject)asset, this, fileService, textureCacheService, referenceFinderService, dialogService),
                    UndertaleRoom => new UndertaleRoomEditorViewModel(name, (UndertaleRoom)asset, this, fileService, textureCacheService, dialogService),
                    UndertaleExtension => new UndertaleExtensionEditorViewModel(name, (UndertaleExtension)asset),
                    UndertaleExtensionFile => new UndertaleExtensionFileEditorViewModel(name, (UndertaleExtensionFile)asset),
                    UndertaleExtensionFunction => new UndertaleExtensionFunctionEditorViewModel(name, (UndertaleExtensionFunction)asset),
                    UndertaleTexturePageItem => new UndertaleTexturePageItemEditorViewModel(name, (UndertaleTexturePageItem)asset, this, textureCacheService, fileService),
                    UndertaleCode => new UndertaleCodeEditorViewModel(name, (UndertaleCode)asset, this, profileService, loadingDialogService, fileService, projectService),
                    UndertaleVariable => new UndertaleVariableEditorViewModel(name, (UndertaleVariable)asset),
                    UndertaleFunction => new UndertaleFunctionEditorViewModel(name, (UndertaleFunction)asset),
                    UndertaleCodeLocals => new UndertaleCodeLocalsEditorViewModel(name, (UndertaleCodeLocals)asset),
                    UndertaleString => new UndertaleStringEditorViewModel(name, (UndertaleString)asset),
                    UndertaleEmbeddedTexture => new UndertaleEmbeddedTextureEditorViewModel(name, (UndertaleEmbeddedTexture)asset, this, fileService, textureCacheService),
                    UndertaleEmbeddedAudio => new UndertaleEmbeddedAudioEditorViewModel(name, (UndertaleEmbeddedAudio)asset, playerService, fileService),
                    UndertaleTextureGroupInfo => new UndertaleTextureGroupInfoEditorViewModel(name, (UndertaleTextureGroupInfo)asset),
                    UndertaleEmbeddedImage => new UndertaleEmbeddedImageEditorViewModel(name, (UndertaleEmbeddedImage)asset),
                    // TODO Figure out what this is. UndertaleSequence => "Sequence",
                    // TODO Figure out what this is too. UndertaleAnimationCurve => "Animation Curve",
                    UndertaleParticleSystem => new UndertaleParticleSystemEditorViewModel(name, (UndertaleParticleSystem)asset),
                    UndertaleParticleSystemEmitter => new UndertaleParticleSystemEmitterEditorViewModel(name, (UndertaleParticleSystemEmitter)asset, this, fileService, textureCacheService),
                    _ => new EditorContentViewModel($"There is no Editor for {name}.")
                };
                return model;
            }
            return new EditorContentViewModel($"There is no Editor for {GetTitleForObject(asset)}.");
        }

        private void OpenInTab(object obj, bool isNewTab = false, string tabTitle = null)
        {
            if (obj is null)
                return;

            if (obj is DescriptionViewModel && CurrentTab is not null && !CurrentTab.AutoClose)
                return;

            // close auto-closing tab
            if (Tabs.Count > 0 /*&& CurrentTabIndex >= 0*/ && CurrentTab.AutoClose)
                CloseTab(CurrentTab, false);

            if (isNewTab || Tabs.Count == 0)
            {
                int newIndex = Tabs.Count;
                TabViewModel newTab = new(obj, newIndex, tabTitle, CloseAssetTabCommand, CloseOtherTabsCommand, CloseAllTabsCommand, GetTitleForObject);

                Tabs.Add(newTab);
                // CurrentTabIndex = newIndex;

                newTab.History.Add(obj);

                CurrentTab = newTab;
            }
            else if (obj != CurrentTab?.CurrentObject)
            {
                if (CurrentTab.HistoryPosition < CurrentTab.History.Count - 1)
                {
                    // Remove all objects after the current one (overwrite)
                    int count = CurrentTab.History.Count - CurrentTab.HistoryPosition - 1;
                    for (int i = 0; i < count; i++)
                        CurrentTab.History.RemoveAt(CurrentTab.History.Count - 1);
                }

                CurrentTab.CurrentObject = obj;
                CurrentTab.TabTitle = tabTitle;
                CurrentTab.History.Add(obj);
                CurrentTab.HistoryPosition++;
                NotifyHistoryChanged();
            }
        }

        public void CloseTab(bool addDefaultTab = true) // close the current tab
        {
            if (CurrentTab != null)
                CloseTab(CurrentTab, addDefaultTab);
        }
        public void CloseTab(int tabIndex, bool addDefaultTab = true)
        {
            if (tabIndex >= 0 && tabIndex < Tabs.Count)
            {
                TabViewModel closingTab = Tabs[tabIndex];
                closingTab.TabIndex = tabIndex;

                int currIndex = Tabs.IndexOf(CurrentTab);

                Tabs.RemoveAt(tabIndex);

                if (!closingTab.AutoClose)
                    ClosedTabsHistory.Add(closingTab);
                CloseOtherTabsCommand.NotifyCanExecuteChanged();

                if (Tabs.Count == 0)
                {
                    /*if (!closingTab.AutoClose)
                        CurrentTab.SaveTabContentState();*/ // TODO Needed to apply changes to code in code editor if tab is closed of not in view.

                    CurrentTab = null;
                    
                    if (addDefaultTab)
                    {
                        OpenInTab(GetViewModelFromObject(new Description("Welcome to UndertaleModTool!",
                                                      "Open a data.win file to get started, then double click on the items on the left to view them")), false, "Welcome!");
                        CurrentTab = Tabs[0];
                    }
                }
                else
                {
                    bool tabIsChanged = false;

                    // if closing the currently open tab
                    if (currIndex == tabIndex)
                    {
                        // and if that tab is not the last
                        if (Tabs.Count > 1 && tabIndex < Tabs.Count - 1)
                        {
                            // switch to the last tab
                            currIndex = Tabs.Count - 1;
                        }
                        else
                        {
                            if (currIndex != 0)
                                currIndex -= 1;

                            tabIsChanged = true;
                            /*CurrentTab.SaveTabContentState();*/   // TODO Needed to apply changes to code in code editor if tab is closed of not in view.
                        }
                    }
                    else if (currIndex > tabIndex)
                    {
                        currIndex -= 1;
                    }

                    TabViewModel newTab = Tabs[currIndex];

                    if (tabIsChanged)
                    {
/*                        if (closingTab.CurrentObject != newTab.CurrentObject)
                            newTab.PrepareCodeEditor();*/   // TODO Need to do this in viewmodel I think?
                    }

                    CurrentTab = newTab;

/*                    if (tabIsChanged)
                        CurrentTab.RestoreTabContentState();*/
                }
            }
        }
        public bool CloseTab(object obj, bool addDefaultTab = true)
        {
            if (obj is not null)
            {
                var tab = Tabs.FirstOrDefault(x => x == obj);
                if (tab != null)
                {
                    CloseTab(Tabs.IndexOf(tab), addDefaultTab);
                    return true;
                }
            }
            else
                Debug.WriteLine("Can't close the tab - object is null.");

            return false;
        }

        /// <summary>Generates a tab title depending on a type of the object.</summary>
        public string? GetTitleForObject(object obj)
        {
            if (obj is null)
                return "No Valid Data";
            if (obj is EditorContentViewModel editorContent)
                return editorContent.MainText;
            string? title = null;
            if (obj is Description view)
            {
                if (view.Heading.Contains("Welcome"))
                {
                    title = "Welcome!";
                }
                else
                {
                    title = view.Heading;
                }
            }
            else if (obj is UndertaleNamedResource namedRes)
            {
                string? content = namedRes.Name?.Content;

                string? header = obj switch
                {
                    UndertaleAudioGroup => "Audio Group",
                    UndertaleSound => "Sound",
                    UndertaleSprite => "Sprite",
                    UndertaleBackground => "Background",
                    UndertalePath => "Path",
                    UndertaleScript => "Script",
                    UndertaleShader => "Shader",
                    UndertaleFont => "Font",
                    UndertaleTimeline => "Timeline",
                    UndertaleGameObject => "Game Object",
                    UndertaleRoom => "Room",
                    UndertaleExtension => "Extension",
                    UndertaleTexturePageItem => "Texture Page Item",
                    UndertaleCode => "Code",
                    UndertaleVariable => "Variable",
                    UndertaleFunction => "Function",
                    UndertaleCodeLocals => "Code Locals",
                    UndertaleEmbeddedTexture => "Embedded Texture",
                    UndertaleEmbeddedAudio => "Embedded Audio",
                    UndertaleTextureGroupInfo => "Texture Group Info",
                    UndertaleEmbeddedImage => "Embedded Image",
                    UndertaleSequence => "Sequence",
                    UndertaleAnimationCurve => "Animation Curve",
                    UndertaleParticleSystem => "Particle System",
                    UndertaleParticleSystemEmitter => "Particle System Emitter",
                    _ => null
                };

                if (header is not null)
                    title = header + " - " + content;
                else
                    Debug.WriteLine($"Could not handle type {obj.GetType()}");
            }
            else if (obj is UndertaleString str)
            {
                string stringFirstLine = str.Content;
                if (stringFirstLine is not null)
                {
                    if (stringFirstLine.Length == 0)
                        stringFirstLine = "(empty string)";
                    else
                    {
                        int stringLength = StringTitleConverter.NewLineRegex.Match(stringFirstLine).Index;
                        if (stringLength != 0)
                            stringFirstLine = stringFirstLine[..stringLength] + " ...";
                    }
                }

                title = "String - " + stringFirstLine;
            }
            else if (obj is UndertaleExtensionFile file)
            {
                title = $"Extension file - {file.Filename}";
            }
            else if (obj is UndertaleExtensionFunction func)
            {
                title = $"Extension function - {func.Name}";
            }
            else if (obj is UndertaleChunkVARI)
            {
                title = "Variables Overview";
            }
            else if (obj is UndertaleGeneralInfo)
            {
                title = "General Info";
            }
            else if (obj is IList<UndertaleGlobalInit>)
            {
                title = "Global Init";
            }
            else if (obj is UndertaleGameEndEditorViewModel)    // TODO make this work correctly when testing on a supported win file.
            {
                title = "Game End";
            }
            else
            {
                title = obj.GetType().FullName ?? "Null Type";
                Debug.WriteLine($"Could not handle type {obj.GetType()}");
            }

            if (title is not null)
            {
                // "\t" is displayed as 8 spaces.
                // So, replace all "\t" with spaces,
                // in order to properly shorten the title.
                title = title.Replace("\t", "        ");

                if (title.Length > 64)
                    title = title[..64] + "...";
            }
            return title;
        }
    }
}
