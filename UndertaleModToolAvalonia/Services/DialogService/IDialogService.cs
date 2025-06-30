using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.Services.DialogService
{
    public interface IDialogService
    {
        public Task<TResult?> ShowDialogAsync<TViewModel, TResult>(Window owner) where TViewModel : ViewModelBase;

        public Task<TResult?> ShowDialogAsync<TViewModel, TParams, TResult>(Window owner, TParams parameters) where TViewModel : ViewModelBase, IInitializable<TParams>;

        public void Show<TViewModel>() where TViewModel : ViewModelBase;

        public Task ShowAsync<TViewModel, TParams>(TParams parameters) where TViewModel : ViewModelBase, IInitializable<TParams>;
    }
}
