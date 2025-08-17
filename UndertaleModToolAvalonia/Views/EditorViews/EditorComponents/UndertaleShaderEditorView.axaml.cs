using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using System;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleShaderEditorView : UserControl
{
    public UndertaleShaderEditorView()
    {
        InitializeComponent();
    }

    private void TextEditor_Loaded(object sender, RoutedEventArgs e)
    {
        var editor = sender as TextEditor;
        if (editor is null)
        {
            _ = App.Current!.ShowError("Cannot load the code of one of the shader properties - the editor is not found?");
            return;
        }

        var srcString = editor.DataContext as UndertaleString;
        if (srcString is null)
        {
            _ = App.Current!.ShowError("Cannot load the code of one of the shader properties - the source string object is null.");
            return;
        }

        editor.Text = srcString.Content;
    }

    private void TextEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        var editor = sender as TextEditor;
        if (editor is null)
        {
            _ = App.Current!.ShowError("The changes weren't saved - the editor is not found?");
            return;
        }

        var srcString = editor.DataContext as UndertaleString;
        if (srcString is null)
        {
            _ = App.Current!.ShowError("The changes weren't saved - the source string object is null.");
            return;
        }

        srcString.Content = editor.Text;
    }

    private void Button_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not UndertaleShaderEditorViewModel vm)
            return;
        vm.Shader.VertexShaderAttributes.Add(new UndertaleShader.VertexShaderAttribute());
    }

    private void TextEditor_DataContextChanged(object? sender, EventArgs e)
    {
        var editor = sender as TextEditor;
        if (editor is null)
            return;

        var srcString = editor.DataContext as UndertaleString;
        if (srcString is null)
            return;

        editor.Text = srcString.Content;
    }
}