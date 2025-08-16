using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertalePathEditorView : UserControl
{
    public UndertalePathEditorView()
    {
        InitializeComponent();
    }

    private void Point_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is UndertalePathEditorViewModel vm)
        {
            // After a textbox loses focus, the binding has updated the data.
            // Now, tell the ViewModel to refresh the path preview.
            vm.RefreshPathPreview();
        }
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (this.DataContext is UndertalePathEditorViewModel vm)
        {
            vm.AddPoint();
        }
    }
}