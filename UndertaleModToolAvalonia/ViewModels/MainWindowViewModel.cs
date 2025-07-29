using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
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
        
        [ObservableProperty]
        private IPlayer playerService;

        [ObservableProperty] private string titleMain = string.Empty;

        [ObservableProperty] public UndertaleData? data;

        [ObservableProperty] string? filePath = AppConstants.FilePath;
        
        public MainWindowViewModel(IServiceProvider services, EditorViewModel editorViewModel, IProfileService profileService, IDialogService dialogService, IPlayer playerService)
        {
            this.services = services;
            this.editorViewModel = editorViewModel;
            this.profileService = profileService;
            this.dialogService = dialogService;
            this.PlayerService = playerService;
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
    }
}
