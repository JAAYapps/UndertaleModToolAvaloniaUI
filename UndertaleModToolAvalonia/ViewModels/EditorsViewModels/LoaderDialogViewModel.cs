using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.ViewModels.EditorsViewModels
{
    public class LoaderDialogViewModel : ViewModelBase
    {
        private readonly Window perent;

        private double value = 0;

        public double Value { get => this.value; set => this.RaiseAndSetIfChanged(ref this.value, value); }

        private string messageTitle = string.Empty;

        public string MessageTitle { get => this.messageTitle; set => this.RaiseAndSetIfChanged(ref this.messageTitle, value); }

        private string message = string.Empty;

        public string Message { get => this.message; set => this.RaiseAndSetIfChanged(ref this.message, value); }

        private bool preventClose = true;

        public bool PreventClose { get => this.preventClose; set => this.RaiseAndSetIfChanged(ref this.preventClose, value); }

        private string statusText = "Please wait...";

        public string StatusText { get => this.statusText; set => this.RaiseAndSetIfChanged(ref this.statusText, value); }

        private bool isIndeterminate = true;

        public bool IsIndeterminate { get => this.isIndeterminate; set => this.RaiseAndSetIfChanged(ref this.isIndeterminate, value); }

        private string savedStatusText = string.Empty;

        public string SavedStatusText { get => this.savedStatusText; set => this.RaiseAndSetIfChanged(ref this.savedStatusText, value); }

        private double maximum = 100;

        public double? Maximum
        {
            get
            {
                return !IsIndeterminate ? maximum : null;
            }

            set
            {
                IsIndeterminate = !value.HasValue;
                if (value.HasValue)
                    this.RaiseAndSetIfChanged(ref this.maximum, value.Value);
            }
        }

        public LoaderDialogViewModel(Window perent, string title, string msg)
        {
            MessageTitle = title;
            Message = msg;
            this.perent = perent;
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
