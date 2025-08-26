using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.UpdateService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels.SettingsViewModels;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels
{
    public partial class SettingsPageViewModel : ViewModelBase
    {
        private readonly IUpdateService updateService;

        private readonly IDialogService dialogService;

        [ObservableProperty]
        private bool isUpdaterButtonVisible;

        [ObservableProperty]
        private bool isUpdateButtonEnabled;

        public SettingsPageViewModel(IFileService fileService, IUpdateService updateService, IDialogService dialogService)
        {
            this.updateService = updateService;
            this.dialogService = dialogService;

            // We can bind the button's IsEnabled state directly to the service's property!
            // This requires the service to implement INotifyPropertyChanged, which is why we
            // used ObservableObject as the base class for AppUpdateService.
            updateService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IUpdateService.IsUpdateInProgress))
                {
                    UpdateAppCommand.NotifyCanExecuteChanged();
                }
            };
#if DEBUG
            IsUpdaterButtonVisible = true;
#else
            IsUpdaterButtonVisible = false;
#endif
        }

        public string GameMakerStudioPath
        {
            get => Settings.Instance.GameMakerStudioPath;
            set
            {
                Settings.Instance.GameMakerStudioPath = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public string GameMakerStudio2RuntimesPath
        {
            get => Settings.Instance.GameMakerStudio2RuntimesPath;
            set
            {
                Settings.Instance.GameMakerStudio2RuntimesPath = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public bool AssetOrderSwappingEnabled
        {
            get => Settings.Instance.AssetOrderSwappingEnabled;
            set
            {
                Settings.Instance.AssetOrderSwappingEnabled = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public bool ShowNullEntriesInResourceTree
        {
            get => Settings.Instance.ShowNullEntriesInResourceTree;
            set
            {
                Settings.Instance.ShowNullEntriesInResourceTree = value;
                Settings.Save();
                OnPropertyChanged();
                // Refresh the tree for the change to take effect
                WeakReferenceMessenger.Default.Send(new SettingChangedMessage("UpdateTreeView", value));
            }
        }

        public bool ProfileModeEnabled
        {
            get => Settings.Instance.ProfileModeEnabled;
            set
            {
                Settings.Instance.ProfileModeEnabled = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public bool ProfileMessageShown
        {
            get => Settings.Instance.ProfileMessageShown;
            set
            {
                Settings.Instance.ProfileMessageShown = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }
        public bool TempRunMessageShow
        {
            get => Settings.Instance.TempRunMessageShow;
            set
            {
                Settings.Instance.TempRunMessageShow = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public bool AutomaticFileAssociation
        {
            get => Settings.Instance.AutomaticFileAssociation;
            set
            {
                Settings.Instance.AutomaticFileAssociation = value;
                Settings.Save();
                OnPropertyChanged();

                if (!value && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Prompt user if they want to unassociate
                    if (App.Current.ShowQuestion("Remove current file associations, if they exist?", MsBox.Avalonia.Enums.Icon.Question, "File associations").Result == MsBox.Avalonia.Enums.ButtonResult.Yes)
                    {
                        try
                        {
                            FileAssociations.RemoveAssociations();
                        }
                        catch (Exception ex)
                        {
                            ScriptMessages.ScriptError(ex.ToString(), "Unassociation failed", false);
                        }
                    }
                }
            }
        }

        public bool DeleteOldProfileOnSave
        {
            get => Settings.Instance.DeleteOldProfileOnSave;
            set
            {
                Settings.Instance.DeleteOldProfileOnSave = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public bool WarnOnClose
        {
            get => Settings.Instance.WarnOnClose;
            set
            {
                Settings.Instance.WarnOnClose = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public double GlobalGridWidth
        {
            get => Settings.Instance.GlobalGridWidth;
            set
            {
                Settings.Instance.GlobalGridWidth = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public bool GridWidthEnabled
        {
            get => Settings.Instance.GridWidthEnabled;
            set
            {
                Settings.Instance.GridWidthEnabled = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public double GlobalGridHeight
        {
            get => Settings.Instance.GlobalGridHeight;
            set
            {
                Settings.Instance.GlobalGridHeight = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public bool GridHeightEnabled
        {
            get => Settings.Instance.GridHeightEnabled;
            set
            {
                Settings.Instance.GridHeightEnabled = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public double GlobalGridThickness
        {
            get => Settings.Instance.GlobalGridThickness;
            set
            {
                Settings.Instance.GlobalGridThickness = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public bool GridThicknessEnabled
        {
            get => Settings.Instance.GridThicknessEnabled;
            set
            {
                Settings.Instance.GridThicknessEnabled = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        public string TransparencyGridColor1
        {
            get => Settings.Instance.TransparencyGridColor1;
            set
            {
                try
                {
                    Settings.Instance.TransparencyGridColor1 = value;
                    Settings.Save();
                    WeakReferenceMessenger.Default.Send(new SettingChangedMessage(nameof(TransparencyGridColor1), value));
                }
                catch (FormatException) { }
            }
        }

        public string TransparencyGridColor2
        {
            get => Settings.Instance.TransparencyGridColor2;
            set
            {
                try
                {
                    Settings.Instance.TransparencyGridColor2 = value;
                    Settings.Save();
                    WeakReferenceMessenger.Default.Send(new SettingChangedMessage(nameof(TransparencyGridColor2), value));
                }
                catch (FormatException) { }
            }
        }

        public bool EnableDarkMode
        {
            get
            {
                return App.Current!.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
            }
            set
            {
                Settings.Instance.EnableDarkMode = value;
                Settings.Save();
                OnPropertyChanged();

                WeakReferenceMessenger.Default.Send(new SettingChangedMessage("EnableDarkMode", value));
            }
        }

        public bool ShowDebuggerOption
        {
            get => Settings.Instance.ShowDebuggerOption;
            set
            {
                Settings.Instance.ShowDebuggerOption = value;
                Settings.Save();
                OnPropertyChanged();
                // mainWindow.RunGMSDebuggerItem.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool RememberWindowPlacements
        {
            get => Settings.Instance.RememberWindowPlacements;
            set
            {
                Settings.Instance.RememberWindowPlacements = value;
                Settings.Save();
            }
        }

        public DecompilerSettings DecompilerSettings => Settings.Instance.DecompilerSettings;

        public string InstanceIdPrefix
        {
            get => Settings.Instance.InstanceIdPrefix;
            set
            {
                Settings.Instance.InstanceIdPrefix = value;
                Settings.Save();
                OnPropertyChanged();
            }
        }

        [RelayCommand]
        private void OpenAppData()
        {
            UndertaleHelper.OpenFolder(Settings.AppDataFolder);
        }

        [RelayCommand(CanExecute = nameof(CanUpdateApp))]
        private async Task UpdateAppAsync()
        {
            await updateService.CheckForUpdatesAsync();
        }

        private bool CanUpdateApp()
        {
            return !updateService.IsUpdateInProgress;
        }

        [RelayCommand]
        private async Task OpenGmlSettingsAsync(Window owner)
        {
            await dialogService.ShowDialogAsync<GMLSettingsViewModel, bool>(owner);
            Settings.Save();
        }
    }
}
