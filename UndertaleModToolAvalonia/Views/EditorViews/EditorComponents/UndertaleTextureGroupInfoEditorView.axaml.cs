using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleTextureGroupInfoEditorView : UserControl
{
    public UndertaleTextureGroupInfoEditorView()
    {
        InitializeComponent();
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is not Button button)
            return;
        if (DataContext is not UndertaleTextureGroupInfoEditorViewModel vm)
            return;
        if (button.Name == "TexturePagesButton")
        {
            vm.AddTexturePageCommand.Execute(null);
        }

        if (button.Name == "SpritesButton")
        {
            vm.AddSpriteCommand.Execute(null);
        }

        if (button.Name == "SpineSpritesButton")
        {
            vm.AddSpineSpriteCommand.Execute(null);
        }

        if (button.Name == "FontsButton")
        {
            vm.AddFontCommand.Execute(null);
        }

        if (button.Name == "TilesetsButton")
        {
            vm.AddTileSetCommand.Execute(null);
        }
    }
}