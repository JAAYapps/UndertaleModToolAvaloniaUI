using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using UndertaleModToolAvalonia.Messages;

namespace UndertaleModToolAvalonia.Views.EditorViews;

public partial class DebugDataDialog : ContentDialog
{
    public DebugDataDialog()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<CloseDialogMessage>(this, (r, m) =>
        {
            this.CloseButtonCommand.Execute(null);
        });
    }
}