using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents.UndertaleFontEditorView;

public partial class UndertaleFontEditorView : UserControl
{
    public UndertaleFontEditorView()
    {
        InitializeComponent();
        TexturePreviewGrid.PointerPressed += TexturePreviewGrid_PointerPressed;
    }

    private void TexturePreviewGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not UndertaleFontEditorViewModel vm || vm.UndertaleFont?.Texture?.TexturePage?.TextureData?.Image == null)
            return;

        var pos = e.GetPosition(sender as Visual);
        foreach (var glyph in vm.UndertaleFont.Glyphs)
        {
            if (pos.X > glyph.SourceX && pos.X < glyph.SourceX + glyph.SourceWidth &&
                pos.Y > glyph.SourceY && pos.Y < glyph.SourceY + glyph.SourceHeight)
            {
                vm.SelectedGlyph = glyph;
                ScrollGlyphIntoView(glyph);
                break;
            }
        }
    }

    private void ScrollGlyphIntoView(UndertaleFont.Glyph glyph)
    {
        if (DataContext is not UndertaleFontEditorViewModel vm)
            return;

        if (glyph == null) return;

        // Avalonia's DataGrid handles scrolling into view much more simply.
        GlyphsGrid.ScrollIntoView(glyph, null);
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {

    }

    private void KerningBackButton_Click(object sender, RoutedEventArgs e)
    {
        
        //BindingOperations.ClearBinding(GlyphKerningGrid, DataGrid.ItemsSourceProperty);
        GlyphKerningBorder.IsVisible = false;
        //GlyphKerningGrid.IsEnabled = false;

        GlyphsGrid.IsVisible = true;
        GlyphsGrid.IsEnabled = true;

        GlyphsLabel.Text = "Glyphs:";
    }
    private void Command_GoBack(object sender, ExecutedRoutedEventArgs e)
    {
        //if (GlyphKerningGrid.IsEnabled)
            KerningBackButton_Click(null, null);
    }
}