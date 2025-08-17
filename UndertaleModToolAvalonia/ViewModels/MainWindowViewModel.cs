using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Services.PlayerService;
using UndertaleModToolAvalonia.Services.ProfileService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

namespace UndertaleModToolAvalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IServiceProvider services;
        private readonly EditorViewModel editorViewModel;
        private readonly IDialogService dialogService;
        private readonly IProfileService profileService;
        private readonly IFileService fileService;
        private readonly ILoadingDialogService loadingDialogService;

        [ObservableProperty]
        private IPlayer playerService;

        [ObservableProperty] private string titleMain = string.Empty;

        [ObservableProperty] public UndertaleData? data;

        [ObservableProperty] string? filePath = AppConstants.FilePath;

        [ObservableProperty] private bool menuEnabled = true;

        private bool CanSave
        {
            get
            {
                OnPropertyChanged(nameof(CanSave));
                return Settings.Instance.CanSave;
            }
            set
            {
                Settings.Instance.CanSave = value;
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public MainWindowViewModel(IServiceProvider services, EditorViewModel editorViewModel, IProfileService profileService, IDialogService dialogService, IPlayer playerService, IFileService fileService, ILoadingDialogService loadingDialogService)
        {
            this.services = services;
            this.editorViewModel = editorViewModel;
            this.profileService = profileService;
            this.dialogService = dialogService;
            this.PlayerService = playerService;
            this.fileService = fileService;
            this.loadingDialogService = loadingDialogService;
            TitleMain = "UndertaleModTool by krzys_h, recreated by Joshua Vanderzee v:" + AppConstants.Version;
            Data = AppConstants.Data;
            WeakReferenceMessenger.Default.Register<TitleUpdateMessage>(this, (r, m) =>
            {
                Data = AppConstants.Data;
                FilePath = AppConstants.FilePath;
            });
            Pages = new ObservableCollection<PageTemplate>()
            {
                new PageTemplate(typeof(ProjectsPageViewModel), "Projects"),
                new PageTemplate(typeof(SettingsPageViewModel), "Settings"),
            };
            OnSelectedPageChanged(Pages[0]);
            CanSave = Settings.Instance.CanSave;
        }

        [ObservableProperty] private bool isPaneOpen = false;
        
        [ObservableProperty] private ViewModelBase? currentPage;

        [ObservableProperty] private PageTemplate? selectedPage;

        partial void OnSelectedPageChanged(PageTemplate value)
        {
            if (value is null) return;
            var instance = services.GetRequiredService(value.PageType);
            if (instance is null) return;
            CurrentPage = (ViewModelBase)instance;
            selectedPage = value;
        }
        
        public ObservableCollection<PageTemplate> Pages { get; }

        [RelayCommand]
        private void OnPaneOpen()
        {
            IsPaneOpen = !IsPaneOpen;
        }

        [RelayCommand]
        private async Task Startup(Window owner)
        {
            Console.WriteLine("Running startup");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Load settings to see if we should associate files
                    if (Settings.Instance is null)
                    {
                        Settings.Load();
                    }
                    bool shouldAssociate = true;
                    if (File.Exists("dna.txt"))
                    {
                        shouldAssociate = false;
                    }
                    if (shouldAssociate && Settings.ShouldPromptForAssociations)
                    {
                        if (await App.Current!.ShowQuestion("First-time setup: Would you like to associate GameMaker data files (like .win) with UndertaleModTool?", MsBox.Avalonia.Enums.Icon.Question, "File associations") != MsBox.Avalonia.Enums.ButtonResult.Yes)
                        {
                            shouldAssociate = false;
                        }
                    }
                    if (!shouldAssociate)
                    {
                        // Shouldn't associate, so turn it off and save that setting
                        Settings.Instance!.AutomaticFileAssociation = false;
                        Settings.Save();
                    }

                    // Associate file types if enabled
                    if (Settings.Instance!.AutomaticFileAssociation)
                    {
                        FileAssociations.CreateAssociations();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            var args = Environment.GetCommandLineArgs();
            bool isLaunch = false;
            bool isSpecialLaunch = false;
            if (args.Length > 1)
            {
                if (args.Length > 2)
                {
                    isLaunch = args[2] == "launch";
                    isSpecialLaunch = args[2] == "special_launch";
                }

                string arg = args[1];
                if (File.Exists(arg))
                {
                    await editorViewModel.LoadFileAsync(arg, true, isLaunch || isSpecialLaunch);
                }
                else if (arg == "deleteTempFolder") // if was launched from UndertaleModToolUpdater
                {
                    _ = Task.Run(async () =>
                    {
                        Process[] updaterInstances = Process.GetProcessesByName("UndertaleModToolUpdater");
                        bool updaterClosed = false;

                        if (updaterInstances.Length > 0)
                        {
                            foreach (Process instance in updaterInstances)
                            {
                                if (!instance.WaitForExit(5000))
                                    await App.Current!.ShowWarning("UndertaleModToolUpdater app didn't exit.\nCan't delete its temp folder.");
                                else
                                    updaterClosed = true;
                            }
                        }
                        else
                            updaterClosed = true;

                        if (updaterClosed)
                        {
                            bool deleted = false;
                            string exMessage = "(error message is missing)";
                            string tempFolder = Path.Combine(Path.GetTempPath(), "UndertaleModTool");

                            for (int i = 0; i <= 5; i++)
                            {
                                try
                                {
                                    Directory.Delete(tempFolder, true);

                                    deleted = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    exMessage = ex.Message;
                                }

                                await Task.Delay(1000);
                            }

                            if (!deleted)
                                await App.Current!.ShowWarning($"The updater temp folder can't be deleted.\nError - {exMessage}.");
                        }
                    });
                }

                if (isSpecialLaunch)
                {
                    var runtimeParams = new RuntimePickerParameters(FilePath, Data);

                    var runtime = await dialogService.ShowDialogAsync<RuntimePickerViewModel, RuntimePickerParameters, RuntimeModel>(owner, runtimeParams);

                    if (runtime == null)
                        return;
                    Process.Start(runtime.Path, "-game \"" + FilePath + "\"");
                    Environment.Exit(0);
                }
                else if (isLaunch)
                {
                    string gameExeName = Data?.GeneralInfo?.FileName?.Content!;
                    if (gameExeName == null || FilePath == null)
                    {
                        ScriptMessages.ScriptError("Null game executable name or location");
                        Environment.Exit(0);
                    }
                    string gameExePath = Path.Combine(Path.GetDirectoryName(FilePath)!, gameExeName + ".exe");
                    if (!File.Exists(gameExePath))
                    {
                        ScriptMessages.ScriptError("Cannot find game executable path, expected: " + gameExePath);
                        Environment.Exit(0);
                    }
                    if (!File.Exists(FilePath))
                    {
                        ScriptMessages.ScriptError("Cannot find data file path, expected: " + FilePath);
                        Environment.Exit(0);
                    }
                    if (gameExeName != null)
                        Process.Start(gameExePath, "-game \"" + FilePath + "\" -debugoutput \"" + Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
                    Environment.Exit(0);
                }
                else if (args.Length > 2)
                {
                    _ = UndertaleHelper.ListenChildConnection(args[2]);
                }
            }

            // Copy the known code corrections into the profile, if they don't already exist.
            await profileService.ApplyCorrectionsAsync();
            await profileService.CrashCheckAsync();

            /*RunGMSDebuggerItem.Visibility = Settings.Instance.ShowDebuggerOption
                                            ? Visibility.Visible : Visibility.Collapsed;*/ // TODO Figure out what this is.
        }

        [RelayCommand]
        private void NewProject()
        {
            OnSelectedPageChanged(Pages[0]);
            if (CurrentPage is not ProjectsPageViewModel pvm)
                return;
            pvm.ChangePage(1);
            editorViewModel?.BuildTree(null);
            CanSave = Settings.Instance.CanSave;
        }

        [RelayCommand]
        private async Task OpenFileAsync(IStorageProvider storageProvider)
        {
            OnSelectedPageChanged(Pages[0]);
            if (CurrentPage is not ProjectsPageViewModel pvm)
                return;
            
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }
            MenuEnabled = false;
            // Use the FileService to get a file path
            var files = await fileService.LoadFileAsync(storageProvider);
            var filePath = files?.FirstOrDefault()?.Path.LocalPath;

            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled

            pvm.ChangePage(1);
            await editorViewModel.LoadFileAsync(filePath);
            Console.WriteLine(Settings.Instance.CanSave);
            CanSave = Settings.Instance.CanSave;
            MenuEnabled = true;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveFileAsync(IStorageProvider storageProvider)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }
            MenuEnabled = false;
            var storage = await fileService.SaveFileAsync(storageProvider);

            if (storage == null || string.IsNullOrEmpty(storage.Name))
                return; // User cancelled

            await editorViewModel.SaveFileAsync(storage.Path.AbsolutePath);
            MenuEnabled = true;
        }

        private async Task<bool> SaveFileSuppressDebugAsync(IStorageProvider storageProvider)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return false;
            }
            MenuEnabled = false;
            var storage = await fileService.SaveFileAsync(storageProvider);

            if (string.IsNullOrEmpty(storage.Name))
                return false; // User cancelled

            await editorViewModel.SaveFileAsync(storage.Path.AbsolutePath, true);
            MenuEnabled = true;
            return true;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task RunGame()
        {
            if (AppConstants.Data == null)
            {
                ScriptMessages.ScriptError("Nothing to run!");
                return;
            }
            if ((!editorViewModel.WasWarnedAboutTempRun) && Settings.Instance.TempRunMessageShow)
            {
                ScriptMessages.ScriptMessage(@"WARNING:
Temp running the game does not permanently 
save your changes. Please ""Save"" the game
to save your changes. Closing UndertaleModTool
without using the ""Save"" option can
result in loss of work.");
                editorViewModel.WasWarnedAboutTempRun = true;
            }
            bool saveOk = true;
            string oldFilePath = AppConstants.FilePath!;
            bool oldDisableDebuggerState = true;
            int oldSteamValue = 0;
            oldDisableDebuggerState = AppConstants.Data.GeneralInfo.IsDebuggerDisabled;
            oldSteamValue = AppConstants.Data.GeneralInfo.SteamAppID;
            AppConstants.Data.GeneralInfo.SteamAppID = 0;
            AppConstants.Data.GeneralInfo.IsDebuggerDisabled = true;
            string TempFilesFolder = (oldFilePath != null ? Path.Combine(Path.GetDirectoryName(oldFilePath)!, "MyMod.temp") : "");
            await editorViewModel.SaveFileAsync(TempFilesFolder, false);
            AppConstants.Data.GeneralInfo.SteamAppID = oldSteamValue;
            AppConstants.FilePath = oldFilePath;
            AppConstants.Data.GeneralInfo.IsDebuggerDisabled = oldDisableDebuggerState;
            if (TempFilesFolder == null)
            {
                await App.Current!.ShowWarning("Temp folder is null.");
                return;
            }
            else if (saveOk)
            {
                string gameExeName = AppConstants.Data?.GeneralInfo?.FileName?.Content!;
                if (gameExeName == null || AppConstants.FilePath == null)
                {
                    ScriptMessages.ScriptError("Null game executable name or location");
                    return;
                }
                string gameExePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AppConstants.FilePath), gameExeName + ".exe");
                if (!File.Exists(gameExePath))
                {
                    ScriptMessages.ScriptError("Cannot find game executable path, expected: " + gameExePath);
                    return;
                }
                if (!File.Exists(TempFilesFolder))
                {
                    ScriptMessages.ScriptError("Cannot find game path, expected: " + TempFilesFolder);
                    return;
                }
                if (gameExeName != null)
                    Process.Start(gameExePath, "-game \"" + TempFilesFolder + "\" -debugoutput \"" + Path.ChangeExtension(TempFilesFolder, ".gamelog.txt") + "\"");
            }
            else if (!saveOk)
            {
                await App.Current!.ShowWarning("Temp save failed, cannot run.");
                return;
            }
            if (File.Exists(TempFilesFolder))
            {
                await Task.Delay(3000);
                //File.Delete(TempFilesFolder);
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task RunSpecial(IStorageProvider storageProvider)
        {
            if (AppConstants.Data == null)
                return;

            bool saveOk = true;
            if (!AppConstants.Data.GeneralInfo.IsDebuggerDisabled)
            {
                if (await App.Current!.ShowQuestion("The game has the debugger enabled. Would you like to disable it so the game will run?") == MsBox.Avalonia.Enums.ButtonResult.Yes)
                {
                    AppConstants.Data.GeneralInfo.IsDebuggerDisabled = true;

                    if (!await SaveFileSuppressDebugAsync(storageProvider))
                    {
                        await App.Current!.ShowError("You must save your changes to run.");
                        AppConstants.Data.GeneralInfo.IsDebuggerDisabled = false;
                        return;
                    }
                }
                else
                {
                    await App.Current!.ShowError("Use the \"Run game using debugger\" option to run this game.");
                    return;
                }
            }
            else
            {
                AppConstants.Data.GeneralInfo.IsDebuggerDisabled = true;
                if (await App.Current!.ShowQuestion("Save changes first?") == MsBox.Avalonia.Enums.ButtonResult.Yes)
                    saveOk = await SaveFileSuppressDebugAsync(storageProvider);
            }

            if (AppConstants.FilePath == null)
            {
                await App.Current!.ShowWarning("The file must be saved in order to be run.");
            }
            else if (saveOk)
            {
                var runtime = await dialogService.ShowDialogAsync<RuntimePickerViewModel, RuntimePickerParameters, RuntimeModel>((App.Current!.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime)?.MainWindow!, new RuntimePickerParameters(AppConstants.FilePath, AppConstants.Data));
                if (runtime != null)
                    Process.Start(runtime.Path, "-game \"" + AppConstants.FilePath + "\" -debugoutput \"" + Path.ChangeExtension(AppConstants.FilePath, ".gamelog.txt") + "\"");
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task RunDebug(IStorageProvider storageProvider)
        {
            if (AppConstants.Data == null)
                return;

            var result = await App.Current!.ShowQuestion("Are you sure that you want to run the game with GMS debugger?\n" +
                                           "If you want to enable a debug mode in some game, then you need to use one of the scripts.");
            if (result != MsBox.Avalonia.Enums.ButtonResult.Yes)
                return;

            bool origDbg = AppConstants.Data.GeneralInfo.IsDebuggerDisabled;
            AppConstants.Data.GeneralInfo.IsDebuggerDisabled = false;

            bool saveOk = await SaveFileSuppressDebugAsync(storageProvider);
            if (AppConstants.FilePath == null)
            {
                await App.Current!.ShowWarning("The file must be saved in order to be run.");
            }
            else if (saveOk)
            {
                var runtime = await dialogService.ShowDialogAsync<RuntimePickerViewModel, RuntimePickerParameters, RuntimeModel>((App.Current!.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime)?.MainWindow!, new RuntimePickerParameters(AppConstants.FilePath, AppConstants.Data));
                if (runtime == null)
                    return;
                if (runtime.DebuggerPath == null)
                {
                    await App.Current!.ShowError("The selected runtime does not support debugging.", "Run error");
                    return;
                }


                string tempProject = Path.GetTempFileName().Replace(".tmp", ".gmx");
                File.WriteAllText(tempProject, @"<!-- Without this file the debugger crashes, but it doesn't actually need to contain anything! -->
<assets>
  <Configs name=""configs"">
    <Config>Configs\Default</Config>
  </Configs>
  <NewExtensions/>
  <sounds name=""sound""/>
  <sprites name=""sprites""/>
  <backgrounds name=""background""/>
  <paths name=""paths""/>
  <objects name=""objects""/>
  <rooms name=""rooms""/>
  <help/>
  <TutorialState>
    <IsTutorial>0</IsTutorial>
    <TutorialName></TutorialName>
    <TutorialPage>0</TutorialPage>
  </TutorialState>
</assets>");

                Process.Start(runtime.Path, "-game \"" + AppConstants.FilePath + "\" -debugoutput \"" + Path.ChangeExtension(AppConstants.FilePath, ".gamelog.txt") + "\"");
                Process.Start(runtime.DebuggerPath, "-d=\"" + Path.ChangeExtension(AppConstants.FilePath, ".yydebug") + "\" -t=\"127.0.0.1\" -tp=" + AppConstants.Data.GeneralInfo.DebuggerPort + " -p=\"" + tempProject + "\"");
            }
            AppConstants.Data.GeneralInfo.IsDebuggerDisabled = origDbg;
        }

        [RelayCommand]
        private async Task OffsetMap(IStorageProvider storageProvider)
        {
            if (storageProvider is null)
            {
                await App.Current!.ShowError("The dialog could not be opened.");
                return;
            }
            MenuEnabled = false;
            var files = await fileService!.LoadFileAsync(storageProvider);
            var filePath = files?.FirstOrDefault()?.Path.LocalPath;

            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled

            string suggestedName = Path.ChangeExtension(filePath, ".offsetmap.txt");

            var storage = await fileService!.SaveTextFileAsync(storageProvider, suggestedName);

            if (storage == null || string.IsNullOrEmpty(storage.Name))
                return; // User cancelled

            loadingDialogService.Show("Generating", "Loading, please wait...");
            await loadingDialogService.SetIndeterminateAsync(true);

            await Task.Run(async () =>
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var offsets = UndertaleIO.GenerateOffsetMap(stream);
                        using (var writer = File.CreateText(storage.Path.AbsolutePath))
                        {
                            foreach (var off in offsets.OrderBy((x) => x.Key))
                            {
                                writer.WriteLine(off.Key.ToString("X8") + " " + off.Value.ToString().Replace("\n", "\\\n"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await App.Current!.ShowError("An error occurred while trying to load:\n" + ex.Message, "Load error");
                }

                Dispatcher.UIThread.Invoke(() =>
                {
                    loadingDialogService.Hide();
                });
            });
            MenuEnabled = true;
        }

        [RelayCommand]
        private async Task OpenGitHub()
        {
            await UndertaleHelper.OpenBrowser("https://github.com/UnderminersTeam/UndertaleModTool");
        }

        [RelayCommand]
        private async Task OpenAbout()
        {
            await App.Current!.ShowMessage("UndertaleModTool by krzys_h and the Underminers team\nVersion " + AppConstants.Version, "About");
        }
    }
}
