using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.X11;
using log4net;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.ViewModels.EditorsViewModels;
using UndertaleModToolAvalonia.Views;
using UndertaleModToolAvalonia.Views.EditorViews;
using static UndertaleModLib.Compiler.Compiler.Lexer;

namespace UndertaleModToolAvalonia.Utility
{
    public class LoaderDialogFactory
    {
        private static LoaderDialogView loaderDialogView;

        private static LoaderDialogViewModel loaderDialogViewModel;

        private static CancellationTokenSource cts;

        private static CancellationToken cToken;

        private static int progressValue;

        private static Task updater;

        private static bool preventClose;

        public static bool Created { get; set; } = false;

        public static bool IsClosed { get; set; } = false;

        private LoaderDialogFactory() {}

        public static async Task Create(Window perent, bool preventClose = false, string title = "", string msg = "")
        {
            if (!Created || IsClosed)
            {
                if (loaderDialogView is null)
                    loaderDialogView = (LoaderDialogView?)await WindowLoader.createWindowAsync(perent,
                                    typeof(LoaderDialogView), typeof(LoaderDialogViewModel), true, true, title, msg);
                if (loaderDialogViewModel is null)
                    loaderDialogViewModel = loaderDialogView.DataContext as LoaderDialogViewModel;
                else
                    loaderDialogView.DataContext = loaderDialogViewModel;

                loaderDialogView.Closing += OnClosing;
                loaderDialogView.Closed += OnClosed;
                LoaderDialogFactory.preventClose = preventClose;
                Created = true;
                IsClosed = false;
            }
            else
                await loaderDialogView.ShowDialog(perent);
        }

        private static void OnClosed(object? sender, EventArgs e)
        {
            IsClosed = true;
        }

        private static void OnClosing(object? sender, CancelEventArgs e)
        {
            e.Cancel = preventClose;
        }

        public static void HideProgressBar()
        {
            if (loaderDialogView != null)
                loaderDialogView.Hide();
        }

        public static async Task UpdateProgressBar(string message, string status, double progressValue, double maxValue)
        {
            if (loaderDialogViewModel != null)
            {
                await Dispatcher.UIThread.InvokeAsync((Action)(async () =>
                {
                    await loaderDialogViewModel.Update(message, status, progressValue, maxValue);
                }), DispatcherPriority.Normal);
            }
        }

        public static async Task SetProgressBar(string message, string status, double progressValue, double maxValue)
        {
            if (loaderDialogViewModel != null)
            {
                await loaderDialogViewModel.UpdateValue(progressValue);
                loaderDialogViewModel.SavedStatusText = status;

                await UpdateProgressBar(message, status, progressValue, maxValue);
            }
        }
        public static async Task SetProgressBar()
        {
            if (loaderDialogView != null && !loaderDialogView.IsVisible)
                await Dispatcher.UIThread.InvokeAsync(loaderDialogView.Show);
        }

        public static async Task UpdateProgressValue(double progressValue)
        {
            if (loaderDialogViewModel != null)
            {
                await Dispatcher.UIThread.InvokeAsync((Action)(async () =>
                {
                    await loaderDialogViewModel.ReportProgress(progressValue);
                }), DispatcherPriority.Normal);
            }
        }

        public static async Task UpdateProgressStatus(string status)
        {
            if (loaderDialogViewModel != null)
            {
                await Dispatcher.UIThread.InvokeAsync((Action)(async () =>
                {
                    await loaderDialogViewModel.ReportProgress(status);
                }), DispatcherPriority.Normal);
            }
        }

        private static void ProgressUpdater()
        {
            DateTime prevTime = default;
            int prevValue = 0;

            while (true)
            {
                if (cToken.IsCancellationRequested)
                {
                    if (prevValue >= progressValue) //if reached maximum
                        return;
                    else
                    {
                        if (prevTime == default)
                            prevTime = DateTime.UtcNow;                                       //begin measuring
                        else if (DateTime.UtcNow.Subtract(prevTime).TotalMilliseconds >= 500) //timeout - 0.5 seconds
                            return;
                    }
                }

                _ = UpdateProgressValue(progressValue);

                prevValue = progressValue;

                Thread.Sleep(100); //10 times per second
            }
        }

        public static void StartProgressBarUpdater()
        {
            if (cts is not null)
                MessageBox.Show("Warning - there is another progress bar updater task running (hangs) in the background.\nRestart the application to prevent some unexpected behavior.");

            cts = new CancellationTokenSource();
            cToken = cts.Token;

            updater = Task.Run(ProgressUpdater);
        }

        public static async Task StopProgressBarUpdater() //async because "Wait()" blocks UI thread
        {
            if (cts is null) return;

            cts.Cancel();

            if (await Task.Run(() => !updater.Wait(2000))) //if ProgressUpdater isn't responding
                MessageBox.Show("Stopping the progress bar updater task is failed.\nIt's highly recommended to restart the application.");
            else
            {
                cts.Dispose();
                cts = null;
            }

            updater.Dispose();
        }

        public static void Dispose()
        {
            loaderDialogView.Close();
            loaderDialogView = null;
            loaderDialogViewModel = null;
            Created = false;
            IsClosed = false;
        }
    }
}
