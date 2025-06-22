using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.Views.EditorViews;

namespace UndertaleModToolAvalonia.Services.LoadingDialogService;

public class LoadingDialogService(IServiceProvider services) : ILoadingDialogService
{
    private LoaderDialogView? dialogView;
    private LoaderDialogViewModel? viewModel;

    public void Show(string title = "Loading...", string message = "Please wait...")
    {
        // Ensure we're on the UI thread before creating a window
        Dispatcher.UIThread.Invoke(() =>
        {
            if (dialogView is not null)
            {
                // If a dialog is already showing, just bring it to the front and update its text
                viewModel!.MessageTitle = title;
                viewModel!.Message = message;
                dialogView.Activate();
                return;
            }

            // Create the UI
            dialogView = new LoaderDialogView();
            viewModel = services.GetRequiredService<LoaderDialogViewModel>();

            // Configure the ViewModel
            viewModel.MessageTitle = title;
            viewModel.Message = message;
            dialogView.DataContext = viewModel;

            // Show the window modelessly (doesn't block the UI)
            dialogView.Show();
        });
    }

    public void Hide()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            dialogView?.Close();
            dialogView = null;
            viewModel = null;
        });
    }

    public async Task UpdateStatusAsync(string status)
    {
        if (viewModel is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                viewModel.StatusText = status;
            });
        }
    }

    public async Task UpdateProgressAsync(double value, double max)
    {
        if (viewModel is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                viewModel.Maximum = max;
                viewModel.Value = value;
            });
        }
    }

    public async Task SetIndeterminateAsync(bool isIndeterminate)
    {
        if (viewModel is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                viewModel.IsIndeterminate = isIndeterminate;
            });
        }
    }
}