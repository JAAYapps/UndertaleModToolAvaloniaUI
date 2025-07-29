using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.Core;
using System;
using System.Windows.Input;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleBackgroundEditorView : DataUserControl
{
    private readonly ContextMenu tileContextMenu = new();

    public static readonly StyledProperty<ICommand?> FindAllReferencesCommandProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(FindAllReferencesCommand));

    public ICommand? FindAllReferencesCommand
    {
        get => GetValue(FindAllReferencesCommandProperty);
        set => SetValue(FindAllReferencesCommandProperty, value);
    }

    public UndertaleBackgroundEditorView()
    {
        InitializeComponent();
        var item = new MenuItem()
        {
            Header = "Find all references of this tile"
        };
        item.Click += FindAllTileReferencesItem_Click;
        tileContextMenu.Items.Add(item);
        BGTexture.PointerPressed += BGTexture_PointerPressed;
        TileIdList.SelectionChanged += TileIdList_SelectionChanged;
    }

    private void TileIdList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if ((sender as DataGrid).SelectedItem is UndertaleBackground.TileID tileID)
        {
            UndertaleBackground bg = DataContext as UndertaleBackground;
            uint x = tileID.ID % bg.GMS2TileColumns;
            uint y = tileID.ID / bg.GMS2TileColumns;

            Canvas.SetLeft(TileRectangle, ((x + 1) * bg.GMS2OutputBorderX) + (x * (bg.GMS2TileWidth + bg.GMS2OutputBorderX)));
            Canvas.SetTop(TileRectangle, ((y + 1) * bg.GMS2OutputBorderY) + (y * (bg.GMS2TileHeight + bg.GMS2OutputBorderY)));
        }
    }

    private void FindAllTileReferencesItem_Click(object? sender, RoutedEventArgs e)
    {
        var obj = (DataContext as UndertaleBackgroundEditorViewModel);
        var tileSet = obj?.UndertaleBackground;
        if (tileSet is null || TileIdList.SelectedItem is not UndertaleBackground.TileID selectedID)
            return;
        if (FindAllReferencesCommand != null && FindAllReferencesCommand.CanExecute((tileSet, selectedID)))
            FindAllReferencesCommand.Execute((tileSet, selectedID));
    }

    private void BGTexture_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Properties.IsLeftButtonPressed)
        {
            _ = SelectTileRegion(sender, e);
        }
        else if (e.Properties.IsRightButtonPressed)
        {
            if (!SelectTileRegion(sender, e))
                return;

            UpdateLayout();

            tileContextMenu.Open();
        }
    }

    private bool SelectTileRegion(object sender, PointerPressedEventArgs e)
    {
        if (!TileRectangle.IsVisible) // UndertaleHelper.IsGMS2
            return false;

        Point pos = e.GetPosition((Visual)sender);
        UndertaleBackground bg = DataContext as UndertaleBackground;
        int x = (int)((int)pos.X / (bg.GMS2TileWidth + (2 * bg.GMS2OutputBorderX)));
        int y = (int)((int)pos.Y / (bg.GMS2TileHeight + (2 * bg.GMS2OutputBorderY)));
        int tileID = (int)((bg.GMS2TileColumns * y) + x);
        if (tileID > bg.GMS2TileCount - 1)
            return false;

        e.Handled = true;

        int tileIndex = bg.GMS2TileIds.FindIndex(x => x.ID == tileID);
        if (tileIndex == -1)
            return false;

        return ScrollTileIntoView(tileIndex);
    }

    private bool ScrollTileIntoView(int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= TileIdList.ItemsSource.Count())
            return false;

        TileIdList.SelectedIndex = tileIndex;

        var itemToScrollTo = TileIdList.ItemsSource.ElementAt(tileIndex);
        if (itemToScrollTo is null)
            return false;

        // Avalonia handles all the complex layout and offset calculations. Yay!!!
        TileIdList.ScrollIntoView(itemToScrollTo, null);

        return true;
    }
}