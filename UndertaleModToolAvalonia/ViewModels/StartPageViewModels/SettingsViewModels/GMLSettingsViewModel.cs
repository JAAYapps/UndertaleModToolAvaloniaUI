using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using static UndertaleModToolAvalonia.DecompilerSettings;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels.SettingsViewModels
{
    public partial class GMLSettingsViewModel : ViewModelBase
    {
        public string InstanceIdPrefix 
        {
            get => Settings.Instance.InstanceIdPrefix;
            set
            {
                Settings.Instance.InstanceIdPrefix = value;
                OnPropertyChanged();
            } 
        }

        public IEnumerable<IndentStyleKind> IndentStyles { get; } = Enum.GetValues(typeof(IndentStyleKind)).Cast<IndentStyleKind>();

        public DecompilerSettings DecompilerSettings { get => Settings.Instance.DecompilerSettings; }

        [RelayCommand]
        private void RestoreSettings()
        {
            Settings.Instance.DecompilerSettings.RestoreDefaults();
            Settings.Instance.InstanceIdPrefix = Settings.DefaultInstanceIdPrefix;

            // Force all bindings to be updated
            OnPropertyChanged();
        }
    }
}
