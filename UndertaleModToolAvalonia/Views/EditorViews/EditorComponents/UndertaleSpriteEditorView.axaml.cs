using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SkiaSharp;
using System;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleSpriteEditorView : UserControl
{
    public UndertaleSpriteEditorView()
    {
        InitializeComponent();
    }

    private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleSpriteEditorViewModel vm)
            return;
        var objRef = sender as UndertaleObjectReference;
        ToolTip.SetTip(objRef.RemoveButton, "Remove texture entry");
        ToolTip.SetTip(objRef.DetailsButton, "Open texture entry");
    }

    private void TextBlock_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is not UndertaleSpriteEditorViewModel vm)
            return;
        vm.AddEntryCommand?.Execute(null);
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is not UndertaleSpriteEditorViewModel vm)
            return;
        vm.AddMaskEntryCommand?.Execute(null);
    }
}