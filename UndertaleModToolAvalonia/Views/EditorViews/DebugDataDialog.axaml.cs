using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using UndertaleModToolAvalonia.Messages;

namespace UndertaleModToolAvalonia.Views.EditorViews;

public partial class DebugDataDialog : Window
{
    public DebugDataDialog()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<CloseDialogMessage>(this, (r, m) =>
        {
            this.Close();
        });
    }
}