using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.Services.DialogService
{
    internal class DialogService(IServiceProvider services) : IDialogService
    {
        private readonly ViewLocator viewLocator = new();

        public async Task<TResult?> ShowDialogAsync<TViewModel, TResult>(Window owner) where TViewModel : ViewModelBase
        {
            var viewModel = services.GetRequiredService<TViewModel>();
            var dialogView = viewLocator.Build(viewModel);

            if (dialogView is Window dialogWindow)
            {
                dialogWindow.DataContext = viewModel;
                return await dialogWindow.ShowDialog<TResult>(owner);
            }
            throw new InvalidOperationException($"Could not find a matching View for ViewModel '{typeof(TViewModel).FullName}'. Was it named correctly (e.g., 'MyViewModel' -> 'MyView')?");
        }

        public async Task<TResult?> ShowDialogAsync<TViewModel, TParams, TResult>(Window owner, TParams parameters) where TViewModel : ViewModelBase, IInitializable<TParams>
        {
            var viewModel = services.GetRequiredService<TViewModel>();

            bool init = await viewModel.InitializeAsync(parameters);

            if (!init)
            {
                await App.Current!.ShowError("Failed to finish initializing the dialog.");
                return default;
            }
            var dialogView = viewLocator.Build(viewModel);
            if (dialogView is Window dialogWindow)
            {
                dialogWindow.DataContext = viewModel;
                return await dialogWindow.ShowDialog<TResult>(owner);
            }

            throw new InvalidOperationException($"Could not find a View for ViewModel '{typeof(TViewModel).FullName}'.");
        }

        public void Show<TViewModel>() where TViewModel : ViewModelBase
        {
            var viewModel = services.GetRequiredService<TViewModel>();
            var dialogView = viewLocator.Build(viewModel);

            if (dialogView is Window dialogWindow)
            {
                dialogWindow.DataContext = viewModel;
                dialogWindow.Show();
                return;
            }
            throw new InvalidOperationException($"Could not find a matching View for ViewModel '{typeof(TViewModel).FullName}'. Was it named correctly (e.g., 'MyViewModel' -> 'MyView')?");
        }

        public async Task ShowAsync<TViewModel, TParams>(TParams parameters) where TViewModel : ViewModelBase, IInitializable<TParams>
        {
            var viewModel = services.GetRequiredService<TViewModel>();

            bool init = await viewModel.InitializeAsync(parameters);

            if (!init)
            {
                await App.Current!.ShowError("Failed to finish initializing the dialog.");
                return;
            }
            var dialogView = viewLocator.Build(viewModel);
            if (dialogView is Window dialogWindow)
            {
                dialogWindow.DataContext = viewModel;
                dialogWindow.Show();
                return;
            }

            throw new InvalidOperationException($"Could not find a View for ViewModel '{typeof(TViewModel).FullName}'.");
        }
    }
}
