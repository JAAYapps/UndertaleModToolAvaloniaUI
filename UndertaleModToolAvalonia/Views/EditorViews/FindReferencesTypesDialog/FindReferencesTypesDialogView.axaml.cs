using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;

using UndertaleModToolAvalonia.Messages;

namespace UndertaleModToolAvalonia.Views.EditorViews.FindReferencesTypesDialog;

public partial class FindReferencesTypesDialogView : Window
{
    public FindReferencesTypesDialogView()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<CloseDialogMessage>(this, (r, m) =>
        {
            this.Close();
        });
    }
}