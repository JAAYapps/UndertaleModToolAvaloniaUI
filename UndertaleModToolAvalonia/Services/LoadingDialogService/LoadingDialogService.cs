using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.ViewModels.EditorsViewModels;
using UndertaleModToolAvalonia.Views.EditorViews;

namespace UndertaleModToolAvalonia.Services.LoadingDialogService;

public class LoadingDialogService : ILoadingDialogService
{
    private readonly IServiceProvider _services;
    private LoaderDialogView? _dialogView;
    private LoaderDialogViewModel? _viewModel;

    // The service provider is injected so we can create ViewModels on demand.
    public LoadingDialogService(IServiceProvider services)
    {
        _services = services;
    }

    public void Show(string title = "Loading...", string message = "Please wait...")
    {
        // Ensure we're on the UI thread before creating a window
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_dialogView is not null)
            {
                // If a dialog is already showing, just bring it to the front and update its text
                _viewModel!.MessageTitle = title;
                _viewModel!.Message = message;
                _dialogView.Activate();
                return;
            }

            // Create the UI
            _dialogView = new LoaderDialogView();
            _viewModel = _services.GetRequiredService<LoaderDialogViewModel>();

            // Configure the ViewModel
            _viewModel.MessageTitle = title;
            _viewModel.Message = message;
            _dialogView.DataContext = _viewModel;

            // Show the window modelessly (doesn't block the UI)
            _dialogView.Show();
        });
    }

    public void Hide()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            _dialogView?.Close();
            _dialogView = null;
            _viewModel = null;
        });
    }

    public async Task UpdateStatusAsync(string status)
    {
        if (_viewModel is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _viewModel.StatusText = status;
            });
        }
    }

    public async Task UpdateProgressAsync(double value, double max)
    {
        if (_viewModel is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _viewModel.Maximum = max;
                _viewModel.Value = value;
            });
        }
    }

    public async Task SetIndeterminateAsync(bool isIndeterminate)
    {
        if (_viewModel is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _viewModel.IsIndeterminate = isIndeterminate;
            });
        }
    }
}