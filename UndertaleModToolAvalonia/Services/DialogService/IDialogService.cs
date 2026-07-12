using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.Services.DialogService
{
    public interface IDialogViewModel<out TResult>
    {
        string Title { get; }
        TResult? Result { get; }
        void FinalizeResult(bool success);
    }
    
    public interface IDialogService
    {
        public Task<TResult?> ShowDialogAsync<TViewModel, TResult>() where TViewModel : ViewModelBase, IDialogViewModel<TResult>;

        public Task<TResult?> ShowDialogAsync<TViewModel, TParams, TResult>(TParams parameters) where TViewModel : ViewModelBase, IInitializable<TParams>, IDialogViewModel<TResult>;

        public void Show<TViewModel>() where TViewModel : ViewModelBase;

        public Task ShowAsync<TViewModel, TParams>(TParams parameters) where TViewModel : ViewModelBase, IInitializable<TParams>;
    }
}
