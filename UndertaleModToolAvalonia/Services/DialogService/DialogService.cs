using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.Services.DialogService
{
    internal class DialogService(IServiceProvider services) : IDialogService
    {
        private readonly ViewLocator viewLocator = new();
        
        public async Task<TResult?> ShowDialogAsync<TViewModel, TResult>() where TViewModel : ViewModelBase, IDialogViewModel<TResult>
        {
            var viewModel = services.GetRequiredService<TViewModel>();
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var userControlView = viewLocator.Build(viewModel);
                if (userControlView is null)
                    throw new InvalidOperationException(
                        $"Could not find a matching View for ViewModel '{typeof(TViewModel).FullName}'. Was it named correctly (e.g., 'MyViewModel' -> 'MyView')?");
                userControlView.DataContext = viewModel;
                var dialog = new FAContentDialog
                {
                    Title = viewModel.Title,
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                    Content = userControlView,
                    DataContext = viewModel
                };
                var result = await dialog.ShowAsync();
                viewModel.FinalizeResult(result == FAContentDialogResult.Primary);
                return viewModel.Result;
            });
        }

        public async Task<TResult?> ShowDialogAsync<TViewModel, TParams, TResult>(TParams parameters) where TViewModel : ViewModelBase, IInitializable<TParams>, IDialogViewModel<TResult>
        {
            var viewModel = services.GetRequiredService<TViewModel>();
            if (!await viewModel.InitializeAsync(parameters))
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await App.Current!.ShowError("Failed to finish initializing the dialog.");
                });
                return default;
            }

            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var userControlView = viewLocator.Build(viewModel);
                if (userControlView is null)
                    throw new InvalidOperationException(
                        $"Could not find a View for ViewModel '{typeof(TViewModel).FullName}'.");
                userControlView.DataContext = viewModel;
                var dialog = new FAContentDialog
                {
                    Title = viewModel.Title,
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                    Content = userControlView,
                    DataContext = viewModel
                };
                var result = await dialog.ShowAsync();
                viewModel.FinalizeResult(result == FAContentDialogResult.Primary);
                return viewModel.Result;
            });
        }

        public void Show<TViewModel>() where TViewModel : ViewModelBase
        {
#if !BROWSER
            Dispatcher.UIThread.Post(() =>
            {
                var viewModel = services.GetRequiredService<TViewModel>();
                var dialogView = viewLocator.Build(viewModel);

                if (dialogView is Window dialogWindow)
                {
                    dialogWindow.DataContext = viewModel;
                    dialogWindow.Show();
                    return;
                }

                throw new InvalidOperationException(
                    $"Could not find a matching View for ViewModel '{typeof(TViewModel).FullName}'. Was it named correctly (e.g., 'MyViewModel' -> 'MyView')?");
            });
#else
            throw new PlatformNotSupportedException("Showing non-modal windows is not supported in the browser.");
#endif
        }

        public async Task ShowAsync<TViewModel, TParams>(TParams parameters) where TViewModel : ViewModelBase, IInitializable<TParams>
        {
#if !BROWSER
            var viewModel = services.GetRequiredService<TViewModel>();
            bool init = await viewModel.InitializeAsync(parameters);

            if (!init)
            {
                await Dispatcher.UIThread.InvokeAsync(async () => await App.Current!.ShowError("Failed to finish initializing the dialog."));
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var dialogView = viewLocator.Build(viewModel);
                if (dialogView is Window dialogWindow)
                {
                    dialogWindow.DataContext = viewModel;
                    dialogWindow.Show();
                    return;
                }

                throw new InvalidOperationException(
                    $"Could not find a View for ViewModel '{typeof(TViewModel).FullName}'.");
            });
#else
            throw new PlatformNotSupportedException("Showing non-modal windows is not supported in the browser.");
#endif
        }
    }
}
