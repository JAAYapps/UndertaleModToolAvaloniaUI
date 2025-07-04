﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModToolAvalonia.Converters;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Models.EditorModels;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Services.ProfileService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog;
using UndertaleModToolAvalonia.Views.EditorViews;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class EditorViewModel : ViewModelBase
    {
        [ObservableProperty] private int currentTabIndex = 0;

        [ObservableProperty] private object highlighted;

        [ObservableProperty] private bool isEnabled = true;

        private ILoadingDialogService loadingDialogService;

        private IProfileService profileService;

        private IDialogService dialogService;

        public object Selected
        {
            get => null;// TODO Implement CurrentTab?.CurrentObject;
            set
            {
                OnPropertyChanged();
                OpenInTab(value);
            } 
        }
        
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
        
        private int progressValue;
        private Task updater;
        private CancellationTokenSource cts;
        private CancellationToken cToken;
        private readonly object bindingLock = new();
        private HashSet<string> syncBindings = new();
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
        
        private HttpClient httpClient;
        
        private LoaderDialogView scriptDialog;
        
        public static string AppDataFolder => Settings.AppDataFolder;
        public static string ProfilesFolder = Path.Combine(Settings.AppDataFolder, "Profiles");
        public static string CorrectionsFolder = Path.Combine(Program.GetExecutableDirectory(), "Corrections");
        public string ProfileHash = "Unknown";
        public bool CrashedWhileEditing = false;

        // Scripting interface-related
        private ScriptOptions scriptOptions;
        private Task scriptSetupTask;
        
        [ObservableProperty] private string objectLabel  = string.Empty;
        
        // TODO Not sure if I can just use Avalonia's theme system.
        // private static readonly Color darkColor = Color.FromArgb(255, 32, 32, 32);
        // private static readonly Color darkLightColor = Color.FromArgb(255, 48, 48, 48);
        // private static readonly Color whiteColor = Color.FromArgb(255, 222, 222, 222);
        // private static readonly SolidColorBrush grayTextBrush = new(Color.FromArgb(255, 179, 179, 179));
        // private static readonly SolidColorBrush inactiveSelectionBrush = new(Color.FromArgb(255, 212, 212, 212));
        // private static readonly Dictionary<ResourceKey, object> appDarkStyle = new()
        // {
        //     { SystemColors.WindowTextBrushKey, new SolidColorBrush(whiteColor) },
        //     { SystemColors.ControlTextBrushKey, new SolidColorBrush(whiteColor) },
        //     { SystemColors.WindowBrushKey, new SolidColorBrush(darkColor) },
        //     { SystemColors.ControlBrushKey, new SolidColorBrush(darkLightColor) },
        //     { SystemColors.ControlLightBrushKey, new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) },
        //     { SystemColors.MenuTextBrushKey, new SolidColorBrush(whiteColor) },
        //     { SystemColors.MenuBrushKey, new SolidColorBrush(darkLightColor) },
        //     { SystemColors.GrayTextBrushKey, new SolidColorBrush(Color.FromArgb(255, 136, 136, 136)) },
        //     { SystemColors.InactiveSelectionHighlightBrushKey, new SolidColorBrush(Color.FromArgb(255, 112, 112, 112)) }
        // };

        public EditorViewModel(ILoadingDialogService loadingDialogService, IProfileService profileService, IDialogService dialogService)
        {
            this.loadingDialogService = loadingDialogService;
            this.profileService = profileService;
            Highlighted = new Description("Welcome to UndertaleModTool!", "Open a data.win file to get started, then double click on the items on the left to view them.");
            // TODO Implement OpenInTab(Highlighted);

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

            // TODO Same thing about Avalonia theme system.
            // var resources = Application.Current.Resources;
            // resources["CustomTextBrush"] = SystemColors.ControlTextBrush;
            // resources[SystemColors.GrayTextBrushKey] = grayTextBrush;
            // resources[SystemColors.InactiveSelectionHighlightBrushKey] = inactiveSelectionBrush;
        }

        public async Task LoadFileAsync(string filename, bool preventClose = false, bool onlyGeneralInfo = false)
        {
            // --- Start Loading Process ---
            loadingDialogService.Show("Loading File", "Reading data.win, please wait...");
            await loadingDialogService.SetIndeterminateAsync(true);

            GameSpecificResolver.BaseDirectory = Program.GetExecutableDirectory();

            await Task.Run(async () =>
            {
                bool hadImportantWarnings = false;
                UndertaleData data = null;
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        data = UndertaleIO.Read(stream, async (string warning, bool isImportant) =>
                        {
                            await App.Current.ShowWarning(warning, "Loading warning");
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
                    await App.Current.ShowError("An error occurred while trying to load:\n" + e.Message, "Load error");
                }

                if (onlyGeneralInfo)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        loadingDialogService.Hide();
                        AppConstants.Data = data;
                        AppConstants.FilePath = filename;
                    });

                    return;
                }

                await Dispatcher.UIThread.Invoke(async () =>
                {
                    if (data != null)
                    {
                        if (data.UnsupportedBytecodeVersion)
                        {
                            await App.Current.ShowWarning("Only bytecode versions 13 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.", "Unsupported bytecode version");
                            Settings.Instance.CanSave = false;
                            Settings.Instance.CanSafelySave = false;
                        }
                        else if (hadImportantWarnings)
                        {
                            await App.Current.ShowWarning("Warnings occurred during loading. Data loss will likely occur when trying to save!", "Loading problems");
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
                            await App.Current.ShowWarning("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.", "YYC");
                        }
                        if (data.GeneralInfo != null)
                        {
                            if (!data.GeneralInfo.IsDebuggerDisabled)
                            {
                                await App.Current.ShowWarning("This game is set to run with the GameMaker Studio debugger and the normal runtime will simply hang after loading if the debugger is not running. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger");
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
                        OnPropertyChanged("Data");
                        OnPropertyChanged("FilePath");
                        OnPropertyChanged("IsGMS2");

                        /*BackgroundsItemsList.Header = IsGMS2 == Visibility.Visible
                                                      ? "Tile sets"
                                                      : "Backgrounds & Tile sets";*/

                        /*UndertaleCodeEditor.gettext = null;
                        UndertaleCodeEditor.gettextJSON = null;*/
                    }
                });
            });

            loadingDialogService.Hide();

            // Clear "GC holes" left in the memory in process of data unserializing
            // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-6.0
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
        private void OpenAssetInTab(object asset)
        {
            if (asset == null) return;
            // TODO I will adapt the old OpenInTab method here
        }

        [RelayCommand]
        private void OpenAssetInNewTab(object asset)
        {
            if (asset == null) return;
            // TODO I will adapt the old OpenInTab(asset, true) method here
        }

        [RelayCommand]
        private async Task OpenGitHub()
        {
            await UndertaleHelper.OpenBrowser("https://github.com/UnderminersTeam/UndertaleModTool");
        }

        [RelayCommand]
        private async Task OpenAbout()
        {
            await App.Current.ShowMessage("UndertaleModTool by krzys_h and the Underminers team\nVersion " + AppConstants.Version, "About");
        }

        [RelayCommand]
        private async Task FindAllReferences(object sender)
        {
            var obj = (sender as Control)?.DataContext;
            if (obj is not UndertaleResource res)
            {
                await App.Current!.ShowError("The selected object is not an \"UndertaleResource\".");
                return;
            }

            try
            {
                await dialogService.ShowAsync<FindReferencesTypesDialogViewModel, UndertaleReferenceParameters>(new UndertaleReferenceParameters(res, AppConstants.Data!));
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("An error occurred in the object references related window.\n" +
                                     $"Please report this on GitHub.\n\n{ex}");
            }
        }

        internal void OpenInTab(object obj, bool isNewTab = false, string tabTitle = null)
        {
            if (obj is null)
                return;

            // TODO Implement tab opening
            /*if (obj is DescriptionView && CurrentTab is not null && !CurrentTab.AutoClose)
                return;

            // close auto-closing tab
            if (Tabs.Count > 0 && CurrentTabIndex >= 0 && CurrentTab.AutoClose)
                CloseTab(CurrentTab.TabIndex, false);

            if (isNewTab || Tabs.Count == 0)
            {
                int newIndex = Tabs.Count;
                Tab newTab = new(obj, newIndex, tabTitle);

                Tabs.Add(newTab);
                CurrentTabIndex = newIndex;

                newTab.History.Add(obj);

                if (!TabController.IsLoaded)
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
                UpdateObjectLabel(obj);

                CurrentTab.History.Add(obj);
                CurrentTab.HistoryPosition++;
            }

            if (DataEditor.IsLoaded)
                GetNearestParent<ScrollViewer>(DataEditor)?.ScrollToTop();*/
        }
    }
}
