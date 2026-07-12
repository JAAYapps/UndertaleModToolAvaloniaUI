using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Services.DialogService;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class DebugDataDialogViewModel : ViewModelBase, IDialogViewModel<DebugDataDialogViewModel.DebugDataMode>
    {
        public enum DebugDataMode
        {
            FullAssembler,
            PartialAssembler,
            Decompiled,
            NoDebug
        }

        public string Title { get; }
        public DebugDataMode Result { get; private set; } = DebugDataMode.NoDebug;

        [RelayCommand]
        private void SetDecompiled()
        {
            Result = DebugDataMode.Decompiled;
            WeakReferenceMessenger.Default.Send(new CloseDialogMessage());
        }

        [RelayCommand]
        private void SetPartialAssembler()
        {
            Result = DebugDataMode.PartialAssembler;
            WeakReferenceMessenger.Default.Send(new CloseDialogMessage());
        }

        [RelayCommand]
        private void SetFullAssembler()
        {
            Result = DebugDataMode.FullAssembler;
            WeakReferenceMessenger.Default.Send(new CloseDialogMessage());
        }

        [RelayCommand]
        private void SetNoDebug()
        {
            Result = DebugDataMode.NoDebug;
            WeakReferenceMessenger.Default.Send(new CloseDialogMessage());
        }
        
        public void FinalizeResult(bool success)
        {
            
        }
    }
}
