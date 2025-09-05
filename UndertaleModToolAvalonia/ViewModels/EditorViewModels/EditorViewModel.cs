using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;
using UndertaleModToolAvalonia.Converters;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Models.EditorModels;
using UndertaleModToolAvalonia.Models.UndertaleReferenceTypes;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Services.PlayerService;
using UndertaleModToolAvalonia.Services.ProfileService;
using UndertaleModToolAvalonia.Services.ReferenceFinderService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog;
using UndertaleModToolAvalonia.Views.EditorViews;
using UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;
using static UndertaleModLib.Models.UndertaleRoom;

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

        [ObservableProperty]
        private ObservableCollection<TabViewModel> tabs = new();

        [ObservableProperty]
        private TabViewModel? currentTab;

        public List<TabViewModel> ClosedTabsHistory { get; } = new();

        [ObservableProperty] private bool wasWarnedAboutTempRun = false;
        
        [ObservableProperty] private bool finishedMessageEnabled = true;
        
        [ObservableProperty] private bool scriptExecutionSuccess = true;
        
        [ObservableProperty] private bool isSaving = false;
        
        [ObservableProperty] private string scriptErrorMessage = "";
        
        [ObservableProperty] private string scriptErrorType = "";
        
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
        public static string CorrectionsFolder = Path.Combine(Program.GetExecutableDirectory(), "Corrections");
        public string ProfileHash = "Unknown";
        public bool CrashedWhileEditing = false;

        // Scripting interface-related
        private ScriptOptions scriptOptions;
        private Task scriptSetupTask;
        
        [ObservableProperty] private string objectLabel  = string.Empty;

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

        public EditorViewModel(ILoadingDialogService loadingDialogService, IProfileService profileService, IDialogService dialogService, IReferenceFinderService referenceFinderService, IPlayer playerService, IFileService fileService, ITextureCacheService textureCacheService)
        {
            this.loadingDialogService = loadingDialogService;
            this.profileService = profileService;
            this.dialogService = dialogService;
            this.referenceFinderService = referenceFinderService;
            this.playerService = playerService;
            this.fileService = fileService;
            this.textureCacheService = textureCacheService;
            Highlighted = new Description("Welcome to UndertaleModTool!", "Open a data.win file to get started, then double click on the items on the left to view them.");
            var asset = GetViewModelFromObject(Highlighted);
            OpenInTab(asset, false, asset.MainText);

            _ = new Settings();

            Settings.Instance.CanSave = false;
            Settings.Instance.CanSafelySave = false;

            scriptSetupTask = Task.Run(() =>
            {
                scriptOptions = ScriptOptions.Default
                    .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler",
                        "UndertaleModLib.Scripting", "UndertaleModLib.Compiler",
                        "UndertaleModToolAvalonia", "System", "System.IO", "System.Collections.Generic",
                        "System.Text.RegularExpressions")
                    .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly,
                        GetType().GetTypeInfo().Assembly,
                        typeof(JsonConvert).GetTypeInfo().Assembly,
                        typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly)
                    .WithEmitDebugInformation(true); //when script throws an exception, add an exception location (line number)
            });
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

        public async Task<bool> LoadFileAsync(string filename, bool preventClose = false, bool onlyGeneralInfo = false)
        {
            IsEnabled = false;
            loadingDialogService.Show("Loading File", "Reading data.win, please wait...");
            await loadingDialogService.SetIndeterminateAsync(true);

            GameSpecificResolver.BaseDirectory = Program.GetExecutableDirectory();

            bool loaded = await Task.Run<bool>(async () =>
            {
                bool hadImportantWarnings = false;
                UndertaleData data = null;
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        data = UndertaleIO.Read(stream, async (string warning, bool isImportant) =>
                        {
                            await App.Current!.ShowWarning(warning, "Loading warning");
                            if (isImportant)
                            {
                                hadImportantWarnings = true;
                            }
                        }, message =>
                        {
                            loadingDialogService.UpdateStatusAsync(message);
                        }, onlyGeneralInfo);
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    Debug.WriteLine(e);
#endif
                    loadingDialogService.Hide();
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
                        AppConstants.FilePath = filename;
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
                            await profileService.UpdateProfileAsync(data, filename);
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
                        if (Path.GetDirectoryName(AppConstants.FilePath) != Path.GetDirectoryName(filename))
                            UndertaleHelper.CloseChildFiles();

                        AppConstants.Data = data;

                        UndertaleCachedImageLoader.Reset();
                        // CachedTileDataLoader.Reset();

                        AppConstants.Data.ToolInfo.DecompilerSettings = Settings.Instance.DecompilerSettings;
                        AppConstants.Data.ToolInfo.InstanceIdPrefix = () => Settings.Instance.InstanceIdPrefix;
                        AppConstants.FilePath = filename;
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
            if (!loaded)
                return false;
            loadingDialogService.Hide();
            IsEnabled = true;
            // Clear "GC holes" left in the memory in process of data unserializing
            // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-6.0
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            ApplyFilter(AppConstants.Data);
            WeakReferenceMessenger.Default.Send(new TitleUpdateMessage());
            return true;
        }

        public async Task SaveFileAsync(string filename, bool suppressDebug = false)
        {
            if (AppConstants.Data == null || AppConstants.Data.UnsupportedBytecodeVersion)
                return;
            
            bool isDifferentPath = AppConstants.FilePath != filename;
            
            // --- Start Saving Process ---
            loadingDialogService.Show("Saving File", "Saveing data.win, please wait...");
            await loadingDialogService.SetIndeterminateAsync(true);

            IProgress<Tuple<int, string, double>> progress = new Progress<Tuple<int, string, double>>(i => { loadingDialogService.UpdateStatusAsync(i.Item2); loadingDialogService.UpdateProgressAsync(i.Item1, i.Item3); });

            AppConstants.FilePath = filename;

            if (Path.GetDirectoryName(AppConstants.FilePath) != Path.GetDirectoryName(filename))
                UndertaleHelper.CloseChildFiles();

            DebugDataDialogViewModel.DebugDataMode debugMode = DebugDataDialogViewModel.DebugDataMode.NoDebug;
            if (!suppressDebug && AppConstants.Data.GeneralInfo != null && !AppConstants.Data.GeneralInfo.IsDebuggerDisabled)
                await App.Current!.ShowWarning("You are saving the game in GameMaker Studio debug mode. Unless the debugger is running, the normal runtime will simply hang after loading. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger");
            Task t = Task.Run(async () =>
            {
                bool SaveSucceeded = true;

                try
                {
                    using (var stream = new FileStream(filename + "temp", FileMode.Create, FileAccess.Write))
                    {
                        UndertaleIO.Write(stream, AppConstants.Data, message =>
                        {
                            loadingDialogService.UpdateStatusAsync(message);
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

                        using (UndertaleWriter writer = new UndertaleWriter(new FileStream(Path.ChangeExtension(AppConstants.FilePath, ".yydebug"), FileMode.Create, FileAccess.Write)))
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
                        await profileService.ProfileSaveEventAsync(AppConstants.Data, filename);
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
            });

            await t;

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
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
        private void CopyItemToClipboard(ResourceNodeViewModel? node)
        {
            // TODO Add clipboard service
        }

        [RelayCommand]
        private void AddNewItem(ResourceNodeViewModel? categoryNode)
        {
            if (categoryNode is null) return;

            // Logic to add a new item based on the category
            // For example:
            if (categoryNode.Header == "Sounds")
            {
                var newSound = new UndertaleSound();
                newSound.Name = AppConstants.Data.Strings.MakeString("new_sound");
                AppConstants.Data.Sounds.Add(newSound);

                // Add the new item to the tree view
                categoryNode.Children.Add(new ResourceNodeViewModel(newSound.Name.Content, newSound, this, referenceFinderService));
            }
            // ... add logic for other categories
        }

        [RelayCommand]
        private void DeleteItem(ResourceNodeViewModel? resourceNode)
        {
            if (resourceNode?.Model is not UndertaleObject resource) return;

            // Find the parent node to remove the child from its collection
            // (This requires having a reference to the parent in the child node, or searching the tree)

            // Then, remove the item from the main data list
            // For example:
            if (resource is UndertaleSound sound)
            {
                AppConstants.Data.Sounds.Remove(sound);
                // And also remove the ViewModel node from the tree
                // ParentNode.Children.Remove(resourceNode);
            }
        }

        [RelayCommand]
        private void OpenAssetInTab(object asset)
        {
            if (asset == null) return;
            var assetObject = GetViewModelFromObject(asset);
            OpenInTab(assetObject, false, assetObject.MainText);
        }

        [RelayCommand]
        private void OpenAssetInNewTab(object asset)
        {
            if (asset == null) return;
            var assetObject = GetViewModelFromObject(asset);
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
                var results = await referenceFinderService.GetReferencesOfObject(tuple, AppConstants.Data!, typeList);
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
                    //UndertaleGameObject => "Game Object",
                    //UndertaleRoom => "Room",
                    //UndertaleExtension => "Extension",
                    UndertaleTexturePageItem => new UndertaleTexturePageItemEditorViewModel(name, (UndertaleTexturePageItem)asset, this, textureCacheService, fileService),
                    UndertaleCode => new UndertaleCodeEditorViewModel(name, (UndertaleCode)asset, this, profileService, loadingDialogService, fileService),
                    UndertaleVariable => new UndertaleVariableEditorViewModel(name, (UndertaleVariable)asset),
                    UndertaleFunction => new UndertaleFunctionEditorViewModel(name, (UndertaleFunction)asset),
                    //UndertaleCodeLocals => "Code Locals",
                    UndertaleEmbeddedTexture => new UndertaleEmbeddedTextureEditorViewModel(name, (UndertaleEmbeddedTexture)asset, this, fileService, textureCacheService),
                    UndertaleEmbeddedAudio => new UndertaleEmbeddedAudioEditorViewModel(name, (UndertaleEmbeddedAudio)asset, playerService, fileService),
                    UndertaleTextureGroupInfo => new UndertaleTextureGroupInfoEditorViewModel(name, (UndertaleTextureGroupInfo)asset),
                    UndertaleEmbeddedImage => new UndertaleEmbeddedImageEditorViewModel(name, (UndertaleEmbeddedImage)asset),
                    // TODO Figure out what this is. UndertaleSequence => "Sequence",
                    // TODO Figure out what this is too. UndertaleAnimationCurve => "Animation Curve",
                    UndertaleParticleSystem => new UndertaleParticleSystemEditorViewModel(name, (UndertaleParticleSystem)asset),
                    //UndertaleParticleSystemEmitter => "Particle System Emitter",
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
                TabViewModel newTab = new(obj, newIndex, tabTitle, CloseAssetTabCommand);

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

                int currIndex = Tabs.IndexOf(CurrentTab);

                Tabs.RemoveAt(tabIndex);

                if (!closingTab.AutoClose)
                    ClosedTabsHistory.Add(closingTab);

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
