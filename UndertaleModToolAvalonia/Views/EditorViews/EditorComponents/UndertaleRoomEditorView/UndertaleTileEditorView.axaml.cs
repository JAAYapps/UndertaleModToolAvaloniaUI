using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Remote.Protocol.Input;
using System;
using UndertaleModToolAvalonia.Controls;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel;
using static UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel.UndertaleTileEditorViewModel;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents.UndertaleRoomEditorView;

public partial class UndertaleTileEditorView : Window
{
    private ScrollViewer FocusedTilesScroll { get; set; }

    private PixelPerfectImage FocusedTilesImage { get; set; }

    public UndertaleTileEditorView()
    {
        InitializeComponent();
    }

    #region Tile painting and picking
    private void Tiles_MouseDown(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is not UndertaleTileEditorViewModel vm)
            return;

        if (sender == TilesCanvas)
        {
            FocusedTilesScroll = TilesScroll;
            FocusedTilesImage = LayerImage;
            vm.FocusedTilesData = vm.TilesData;
        }
        else
        {
            FocusedTilesScroll = PaletteScroll;
            FocusedTilesImage = PaletteLayerImage;
            vm.FocusedTilesData = vm.PaletteTilesData;
        }

        vm.DrawingStart = e.GetPosition(FocusedTilesImage);
        vm.LastMousePos = vm.DrawingStart;

        if (e.Properties.IsMiddleButtonPressed)
        {
            vm.Painting = PaintAction.DragPick;
            vm.DrawingStart = e.GetPosition(this);
            vm.ScrollViewStart = new Point(FocusedTilesScroll.Offset.X, FocusedTilesScroll.Offset.Y);
            vm.UpdateBrush(FocusedTilesImage == LayerImage);
        }
        else if (FocusedTilesScroll == PaletteScroll)
        {
            if (vm.PositionToTile(vm.DrawingStart, vm.FocusedTilesData, out _, out _))
            {
                vm.Painting = PaintAction.Pick;
                vm.Pick(vm.DrawingStart, vm.DrawingStart, vm.FocusedTilesData);
            }
        }
        else if (e.Properties.IsRightButtonPressed)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (vm.PositionToTile(vm.DrawingStart, vm.FocusedTilesData, out int x, out int y))
                {
                    vm.RecordUndoCommand.Execute(null);
                    vm.Fill(vm.FocusedTilesData, x, y, e.KeyModifiers.HasFlag(KeyModifiers.Shift), true);
                    vm.Painting = PaintAction.None;
                }
            }
            else
            {
                vm.RecordUndoCommand.Execute(null);
                vm.PositionToTile(vm.DrawingStart, vm.FocusedTilesData, out int x, out int y);
                vm.PaintTile(x, y, x, y, vm.FocusedTilesData, true);
                vm.Painting = PaintAction.Erase;
            }
        }
        else if (e.Properties.IsLeftButtonPressed)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                if (vm.PositionToTile(vm.DrawingStart, vm.FocusedTilesData, out _, out _))
                {
                    vm.Pick(vm.DrawingStart, vm.DrawingStart, vm.FocusedTilesData);
                    vm.Painting = PaintAction.Pick;
                }
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (vm.PositionToTile(vm.DrawingStart, vm.FocusedTilesData, out int x, out int y))
                {
                    vm.RecordUndoCommand.Execute(null);
                    vm.Fill(vm.FocusedTilesData, x, y, e.KeyModifiers.HasFlag(KeyModifiers.Shift), false);
                    vm.Painting = PaintAction.None;
                }
            }
            else
            {
                vm.RecordUndoCommand.Execute(null);
                vm.PositionToTile(vm.DrawingStart, vm.FocusedTilesData, out int x, out int y);
                vm.PaintTile(x, y, x, y, vm.FocusedTilesData, false);
                vm.Painting = PaintAction.Draw;
            }
        }
        vm.UpdateBrushVisibility(FocusedTilesImage == LayerImage);
    }


    private void Tiles_MouseUp(object sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not UndertaleTileEditorViewModel vm)
            return;

        if (vm.Painting == PaintAction.DragPick)
        {
            vm.Pick(e.GetPosition(FocusedTilesImage), e.GetPosition(FocusedTilesImage), vm.FocusedTilesData);
            vm.FindPaletteCursor();
            vm.RefreshBrush++;
        }
        vm.EndDrawing(e.GetPosition(LayerImage));
        //TODO FocusedTilesScroll = null; //<- A ScrollViewer
        //TODO FocusedTilesImage = null;  //<- A PixelPerfectImage
    }
    private void Tiles_MouseMove(object sender, PointerEventArgs e)
    {
        if (DataContext is not UndertaleTileEditorViewModel vm)
            return;

        vm.PositionToTile(e.GetPosition(LayerImage), vm.TilesData, out int mapX, out int mapY);
        vm.StatusText = $"x: {mapX}  y: {mapY}";

        if (vm.Painting != PaintAction.Pick)
        {
            vm.BrushPreviewX = Convert.ToDouble((long)mapX * (long)vm.TilesData.Background.GMS2TileWidth);
            vm.BrushPreviewY = Convert.ToDouble((long)mapY * (long)vm.TilesData.Background.GMS2TileHeight);
        }
        else
        {
            vm.PositionToTile(
                vm.DrawingStart, vm.TilesData, out int startX, out int startY
            );
            if (mapX < startX) startX = mapX;
            if (mapY < startY) startY = mapY;
            vm.BrushPreviewX = Convert.ToDouble((long)Math.Max(startX, 0) * (long)vm.TilesData.Background.GMS2TileWidth);
            vm.BrushPreviewY = Convert.ToDouble((long)Math.Max(startY, 0) * (long)vm.TilesData.Background.GMS2TileHeight);
        }

        vm.UpdateBrushVisibility(FocusedTilesImage == LayerImage);

        if (FocusedTilesScroll is null)
            return;

        if (vm.Painting == PaintAction.DragPick || vm.Painting == PaintAction.Drag)
        {
            Point pos = e.GetPosition(this as Window);
            if (vm.Painting == PaintAction.DragPick && pos != vm.DrawingStart)
            {
                vm.Painting = PaintAction.Drag;
                vm.UpdateBrush(FocusedTilesImage == LayerImage);
            }
            FocusedTilesScroll.Offset = new Vector(
                Math.Clamp(vm.ScrollViewStart.X + -(pos.X - vm.DrawingStart.X), 0, FocusedTilesScroll.Extent.Width),
                Math.Clamp(vm.ScrollViewStart.Y + -(pos.Y - vm.DrawingStart.Y), 0, FocusedTilesScroll.Extent.Height)
            );
            return;
        }

        if (vm.Painting == PaintAction.Draw)
        {
            Point pos = e.GetPosition(FocusedTilesImage);
            vm.PaintLine(vm.FocusedTilesData, vm.LastMousePos, pos, vm.DrawingStart, false);
            vm.LastMousePos = pos;
        }
        else if (vm.Painting == PaintAction.Erase)
        {
            Point pos = e.GetPosition(FocusedTilesImage);
            vm.PaintLine(vm.FocusedTilesData, vm.LastMousePos, pos, vm.DrawingStart, true);
            vm.LastMousePos = pos;
        }
        else if (vm.Painting == PaintAction.Pick)
            vm.Pick(e.GetPosition(FocusedTilesImage), vm.DrawingStart, vm.FocusedTilesData);
    }
    private void Window_MouseLeave(object sender, PointerEventArgs e)
    {
        if (DataContext is not UndertaleTileEditorViewModel vm)
            return;

        vm.EndDrawing(e.GetPosition(LayerImage));
    }


    #endregion
}