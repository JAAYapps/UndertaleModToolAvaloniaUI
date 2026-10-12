using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleGameObjectEditorView : UserControl
{
    public UndertaleGameObjectEditorView()
    {
        InitializeComponent();
    }

    private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not UndertaleObjectReference objRef)
            return;
        ToolTip.SetTip(objRef.RemoveButton, "Remove action");
        objRef.RemoveButton.IsEnabled = true;
    }

    private void UndertaleObjectReference_GotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not UndertaleObjectReference objRef)
            return;
        if (objRef.FindAncestorOfType<DataGrid>()?.DataContext is UndertaleGameObject.Event ev)
            objRef.ObjectEventSubtype = ev.EventSubtype;
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is not UndertaleGameObjectEditorViewModel vm)
            return;
        vm.AddVertexCommand?.Execute(null);
    }

    private void EventButton_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is not Button button)
            return;
        if (DataContext is not UndertaleGameObjectEditorViewModel vm)
            return;
        vm.AddEventCommand?.Execute(button.DataContext);
    }
}