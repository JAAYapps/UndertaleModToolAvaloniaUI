using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

public partial class ProjectsPageViewModel : ViewModelBase
{
    private static ProjectsPageViewModel? instance;
    
    private EditorViewModel? editorViewModel;

    private readonly IServiceProvider? services;

    private IFileService? fileService;

    private IDialogService dialogService;

    private ILoadingDialogService loadingDialogService;

    [ObservableProperty] private bool menuEnabled = true;

    private bool CanSave { 
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

    public ProjectsPageViewModel()
    {
        Pages = new ObservableCollection<PageTemplate>()
        {
            new PageTemplate(typeof(DataFileViewModel), "Projects"),
            new PageTemplate(typeof(EditorViewModel), "Editor"),
        };
    }

    public ProjectsPageViewModel(IServiceProvider services, IFileService fileService, IDialogService dialogService, ILoadingDialogService loadingDialogServie)
    {
        this.services = services;
        this.fileService = fileService;
        this.dialogService = dialogService;
        this.loadingDialogService = loadingDialogServie;
        if (instance == null)
        {
            Console.WriteLine("Projects is initialized.");
            
            Pages = new ObservableCollection<PageTemplate>()
            {
                new PageTemplate(typeof(DataFileViewModel), "Projects"),
                new PageTemplate(typeof(EditorViewModel), "Editor"),
            };
            instance = this;
            OnSelectedPageChanged(Pages[0]); // Must be called to properly update UI.
        }
        else
        {
            Pages = instance.Pages;
            CurrentPage = instance.CurrentPage;
            selectedPage = instance.selectedPage;
            editorViewModel = instance.editorViewModel;
            Console.WriteLine("Already is initialized.");
            ScriptMessages.PlayInformationSound();
            instance = this;
            OnSelectedPageChanged(SelectedPage); // Must be called to properly update UI.
        }
        CanSave = Settings.Instance.CanSave;
    }
    
    [ObservableProperty] private ViewModelBase? currentPage;
    
    [ObservableProperty] private PageTemplate? selectedPage;
    
    public ObservableCollection<PageTemplate> Pages { get; }
    
    partial void OnSelectedPageChanged(PageTemplate value)
    {
        if (value is null) return;
        if (typeof(EditorViewModel) == value.PageType && editorViewModel is not null)
        {
            CurrentPage = (ViewModelBase)editorViewModel;
            selectedPage = value;
            return;
        }

        var newInstance = services?.GetRequiredService(value.PageType);
        if (newInstance is null) return;
        CurrentPage = (ViewModelBase)newInstance;
        selectedPage = value;
        if (typeof(EditorViewModel) == value.PageType && editorViewModel is null)
        {
            editorViewModel = (EditorViewModel)newInstance;
        }
        if (typeof(DataFileViewModel) == value.PageType)
        {
            ((DataFileViewModel)newInstance).FileLoaded += (s, e) => { OnSelectedPageChanged(Pages[1]); };
        }
    }

    [RelayCommand]
    private void NewProject()
    {
        OnSelectedPageChanged(Pages[1]);
        CanSave = Settings.Instance.CanSave;
    }

    [RelayCommand]
    private async Task OpenFileAsync(IStorageProvider storageProvider)
    {
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

        OnSelectedPageChanged(Pages[1]);
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
    private async void RunGame()
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
        string oldFilePath = AppConstants.FilePath;
        bool oldDisableDebuggerState = true;
        int oldSteamValue = 0;
        oldDisableDebuggerState = AppConstants.Data.GeneralInfo.IsDebuggerDisabled;
        oldSteamValue = AppConstants.Data.GeneralInfo.SteamAppID;
        AppConstants.Data.GeneralInfo.SteamAppID = 0;
        AppConstants.Data.GeneralInfo.IsDebuggerDisabled = true;
        string TempFilesFolder = (oldFilePath != null ? Path.Combine(Path.GetDirectoryName(oldFilePath), "MyMod.temp") : "");
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
            string gameExeName = AppConstants.Data?.GeneralInfo?.FileName?.Content;
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