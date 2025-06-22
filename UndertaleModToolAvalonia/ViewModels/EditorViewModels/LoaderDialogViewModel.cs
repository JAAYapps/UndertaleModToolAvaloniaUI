using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class LoaderDialogViewModel : ViewModelBase
    {
        [ObservableProperty] private double value = 0;

        [ObservableProperty] private string messageTitle = string.Empty;

        [ObservableProperty] private string message = string.Empty;

        [ObservableProperty] private bool preventClose = true;
        
        [ObservableProperty] private bool isClosed = false;

        [ObservableProperty] private string statusText = "Please wait...";

        [ObservableProperty] private bool isIndeterminate = true;

        [ObservableProperty] private string savedStatusText = string.Empty;

        [ObservableProperty] private double? maximum = 100;

        [ObservableProperty] private WindowState state = WindowState.Normal;
        
        public double? ComputedMaximum => !IsIndeterminate ? maximum : null;

        partial void OnIsIndeterminateChanged(bool value)
        {
            // Notify change of ComputedMaximum whenever IsIndeterminate changes
            OnPropertyChanged(nameof(ComputedMaximum));
        }

        partial void OnMaximumChanged(double? value)
        {
            // Update IsIndeterminate based on the value of Maximum
            IsIndeterminate = !value.HasValue;

            // Notify change of ComputedMaximum whenever Maximum changes
            OnPropertyChanged(nameof(ComputedMaximum));
        }
        
        public LoaderDialogViewModel()
        {
        }
        
        public async Task ReportProgress(string status)
        {
            await Task.Run(() => StatusText = status);
        }
        
        public async Task ReportProgress(double value) //update without status text changing
        {
            try
            {
                await ReportProgress(value + "/" + Maximum + (!String.IsNullOrEmpty(SavedStatusText) ? ": " + SavedStatusText : ""));
                await UpdateValue(value);
            }
            catch
            {
                //Silently fail...
            }
        }

        public async Task UpdateValue(double value)
        {
            await Task.Run(() => Value = value);
        }

        public async Task ReportProgress(string status, double value)
        {
            try
            {
                await ReportProgress(value + "/" + Maximum + (!String.IsNullOrEmpty(status) ? ": " + status : "")); //update status
                await UpdateValue(value);
            }
            catch
            {
                // Silently fail...
            }
        }

        public async Task Update(string message, string status, double progressValue, double maxValue)
        {
            if (message != null)
                await Task.Run(() => Message = message);

            if (maxValue != 0)
                await Task.Run(() => Maximum = maxValue);

            await ReportProgress(status, progressValue);
        }

        public async Task Update(string status)
        {
            await ReportProgress(status);
        }
    }
}
