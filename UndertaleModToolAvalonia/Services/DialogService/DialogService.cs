using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
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

        public async Task<TResult?> ShowDialogAsync<TViewModel, TParams, TResult>(Window owner, TParams parameters, string initializationMethod = "") where TViewModel : ViewModelBase
        {
            var viewModel = services.GetRequiredService<TViewModel>();

            var initMethod = typeof(TViewModel).GetMethod(initializationMethod, new[] { typeof(TParams) });
            if (initMethod != null)
            {
                await (Task)initMethod.Invoke(viewModel, new object[] { parameters });
            }

            // 4. Create the View and show the dialog
            var dialogView = viewLocator.Build(viewModel);
            if (dialogView is Window dialogWindow)
            {
                dialogWindow.DataContext = viewModel;
                return await dialogWindow.ShowDialog<TResult>(owner);
            }

            throw new InvalidOperationException($"Could not find a View for ViewModel '{typeof(TViewModel).FullName}'.");
        }
    }
}
