using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.Views.EditorViews;

namespace UndertaleModToolAvalonia.Services.LoadingDialogService;

public class LoadingDialogService : ILoadingDialogService
{
    private ContentDialog? _dialog;
    private readonly LoaderDialogView? _dialogView;
    private readonly LoaderDialogViewModel? _viewModel;
    private bool _isShowing = false;

    public LoadingDialogService(IServiceProvider services)
    {
        _dialogView = new LoaderDialogView();
        _viewModel = services.GetRequiredService<LoaderDialogViewModel>();
    }

    public void Initialize()
    {
        
    }

    public void Show(string title = "Loading...", string message = "Please wait...")
    {
        if (_isShowing) return; // Already shown
        _isShowing = true;
        
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _dialog = new ContentDialog
            {
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                Content = _dialogView,
                DataContext = _viewModel,
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false
            };
            _dialog.Closing += (sender, args) =>
            {
                if (_isShowing) args.Cancel = true;
            };
            _dialog.Opened += (sender, args) =>
            {
                sender.IsVisible = true;
                foreach (var visual in _dialog.GetSelfAndVisualDescendants())
                {
                    if (visual is Button { Name: "PrimaryButton" or "CloseButton" } button)
                        button.IsVisible = false;
                    if (visual.Name == "BackgroundElement")
                        (visual as Border).Height = 210;
                    if (visual is Grid { Name: "DialogSpace" } space)
                    {
                        space.Margin = new Thickness(0, 50, 0, 0);
                        space.Height = 270;
                    }
                }
            };
            _dialogView?.DataContext = _viewModel;
            //.FindControl<Button>("PrimaryButton").IsVisible = false;
            
            if (_viewModel != null)
            {
                _viewModel.MessageTitle = title;
                _viewModel.Message = message;
                _dialog.Title = title;
                _dialog.IsVisible = true;
            }
            await _dialog.ShowAsync();
        });
        
        
    }

    public void Hide()
    {
        if (!_isShowing || _dialog == null) return;
        
        _isShowing = false; // Allow the Closing event to pass
        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _dialog.Hide(); // This actually removes it from the Visual Tree
            _dialog = null; // Clean up
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