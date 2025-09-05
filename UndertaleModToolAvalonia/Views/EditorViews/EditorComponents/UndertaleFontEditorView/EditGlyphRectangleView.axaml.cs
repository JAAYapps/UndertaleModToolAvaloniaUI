using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using System;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel;
using static UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel.RectangleHelper;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents.UndertaleFontEditorView;

public partial class EditGlyphRectangleView : Window
{
    private bool isNewCharacter;
    private bool dragInProgress = false;
    private Canvas canvas;

    private Point mousePosition;

    public EditGlyphRectangleView()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<CloseDialogMessage>(this, (r, m) =>
        {
            if (DataContext is EditGlyphRectangleViewModel vm)
            {
                Close(vm.SelectedGlyph.Model);
            }
            Close();
        });
    }

    private void UpdateSelectedRect()
    {
        if (DataContext is not EditGlyphRectangleViewModel vm)
            return;

        if (canvas is null)
            return;

        canvas.UpdateLayout();
        foreach (var rect in canvas.GetVisualChildren())
        {
            if (rect is Rectangle rectangle && rectangle.DataContext == vm.SelectedGlyph)
            {
                vm.SelectedRect = vm.SelectedRect.WithWidth(rectangle.Width);
                vm.SelectedRect = vm.SelectedRect.WithHeight(rectangle.Height);
                vm.SelectedRect = vm.SelectedRect.WithX(rectangle.Margin.Left);
                vm.SelectedRect = vm.SelectedRect.WithY(rectangle.Margin.Top);
                return;
            }
        }
    }

    //private void OnPointerMoved(object? sender, PointerEventArgs e)
    //{
    //    mousePosition = e.GetPosition(TextureScroll);
    //}

    //private void ScrollViewer_ScrollChanged(object? sender, Avalonia.Controls.ScrollChangedEventArgs e)
    //{
    //    if (e.ExtentDelta.Y != 0 || e.ExtentDelta.X != 0)
    //    {
    //        var currentExtent = TextureScroll.Extent;
    //        var currentOffset = TextureScroll.Offset;

    //        double xMousePositionOnScrollViewer = mousePosition.X;
    //        double yMousePositionOnScrollViewer = mousePosition.Y;
    //        double offsetX = currentOffset.X + xMousePositionOnScrollViewer;
    //        double offsetY = currentOffset.Y + yMousePositionOnScrollViewer;

    //        double oldExtentWidth = currentExtent.Width - e.ExtentDelta.X;
    //        double oldExtentHeight = currentExtent.Height - e.ExtentDelta.Y;

    //        double relx = oldExtentWidth != 0 ? offsetX / oldExtentWidth : 0;
    //        double rely = oldExtentHeight != 0 ? offsetY / oldExtentHeight : 0;

    //        offsetX = Math.Max(relx * currentExtent.Width - xMousePositionOnScrollViewer, 0);
    //        offsetY = Math.Max(rely * currentExtent.Height - yMousePositionOnScrollViewer, 0);

    //        TextureScroll.Offset = new Vector(offsetX, offsetY);
    //    }
    //}

    //private void ScrollViewer_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    //{
    //    e.Handled = true;
    //    if (dragInProgress)
    //        return;

    //    var mousePos = e.GetPosition(TextureViewbox);
    //    var transform = TextureViewbox.RenderTransform as MatrixTransform;
    //    var matrix = transform?.Matrix ?? Matrix.Identity;
    //    var pow = Math.Pow(2, 1.0 / 8.0);
    //    var scale = e.Delta.Y >= 0 ? pow : (1.0 / pow); // choose appropriate scaling factor

    //    if ((matrix.M11 > 0.001 || (matrix.M11 <= 0.001 && scale > 1)) && (matrix.M11 < 1000 || (matrix.M11 >= 1000 && scale < 1)))
    //    {
    //        matrix = matrix.Prepend(Matrix.CreateScale(scale, scale))
    //                   .Prepend(Matrix.CreateTranslation(-mousePos.X * (scale - 1), -mousePos.Y * (scale - 1)));
    //    }
    //    TextureViewbox.RenderTransform = new MatrixTransform(matrix);
    //}

    //private void ScrollViewer_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    //{
    //    if (DataContext is not EditGlyphRectangleViewModel vm)
    //        return;

    //    if (vm.SelectedGlyph is null)
    //        return;

    //    if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
    //        GlyphTopLine.IsVisible = true;

    //    if ((e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
    //    {
    //        switch (e.Key)
    //        {
    //            case Key.Left:
    //                if (vm.SelectedGlyph.SourceWidth <= 1)
    //                    return;
    //                vm.SelectedGlyph.SourceWidth--;
    //                break;

    //            case Key.Right:
    //                if (vm.SelectedGlyph.SourceX + vm.SelectedGlyph.SourceWidth >= vm.Font.Texture.BoundingWidth)
    //                    return;
    //                vm.SelectedGlyph.SourceWidth++;
    //                break;

    //            case Key.Up:
    //                if (vm.SelectedGlyph.SourceHeight <= 1)
    //                    return;
    //                vm.SelectedGlyph.SourceHeight--;
    //                break;

    //            case Key.Down:
    //                if (vm.SelectedGlyph.SourceY + vm.SelectedGlyph.SourceHeight >= vm.Font.Texture.BoundingHeight)
    //                    return;
    //                vm.SelectedGlyph.SourceHeight++;
    //                break;
    //        }

    //        return;
    //    }

    //    switch (e.Key)
    //    {
    //        case Key.Left:
    //            if (vm.SelectedGlyph.SourceX <= 0)
    //                return;
    //            vm.SelectedGlyph.SourceX--;
    //            break;

    //        case Key.Right:
    //            if (vm.SelectedGlyph.SourceX + vm.SelectedGlyph.SourceWidth >= vm.Font.Texture.BoundingWidth)
    //                return;
    //            vm.SelectedGlyph.SourceX++;
    //            break;

    //        case Key.Up:
    //            if (vm.SelectedGlyph.SourceY <= 0)
    //                return;
    //            vm.SelectedGlyph.SourceY--;
    //            break;

    //        case Key.Down:
    //            if (vm.SelectedGlyph.SourceY + vm.SelectedGlyph.SourceHeight >= vm.Font.Texture.BoundingHeight)
    //                return;
    //            vm.SelectedGlyph.SourceY++;
    //            break;
    //    }
    //}

    //private void ScrollViewer_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    //{
    //    if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
    //        GlyphTopLine.IsVisible = false;
    //}

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not EditGlyphRectangleViewModel vm)
            return;

        //double initScale = 1;
        //int textureWidth = vm.Font.Texture?.BoundingWidth ?? 1;
        //if (textureWidth < TextureScroll.Width)
        //{
        //    initScale = TextureScroll.Width / textureWidth;
        //    initScale = Math.Pow(2, Math.Floor(Math.Log2(initScale))); // Round down to nearest power of 2
        //}

        //ContentGrid.RenderTransform = new MatrixTransform(new Matrix(initScale, 0, 0, initScale, 0, 0));
        //ContentGrid.UpdateLayout();
        Console.WriteLine("DataContext is viewmodel");
        if (vm.SelectedGlyph.SourceWidth == 0 || vm.SelectedGlyph.SourceHeight == 0)
        {
            isNewCharacter = true;
            ContentGrid.PointerMoved += ContentGrid_MouseMove_New;
            ContentGrid.Cursor = new Cursor(StandardCursorType.Cross);
        }
        else
            ContentGrid.PointerMoved += ContentGrid_MouseMove;
        ZoomBorder.AutoFit();
        TextureScroll.ScrollToHome();
        TextureScroll.Focus();
    }

    private void ContentGrid_MouseLeftButtonDown(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
            return;
        if (DataContext is not EditGlyphRectangleViewModel vm)
            return;

        if (!isNewCharacter)
        {
            if (e.Source is not Rectangle rect
                || rect.DataContext is not GlyphViewModel glyph)
                return;

            if (e.ClickCount >= 2)
            {
                vm.SelectedGlyph = new GlyphViewModel(glyph.Model);
                UpdateSelectedRect();

                if (isNewCharacter)
                {
                    isNewCharacter = false;
                    ContentGrid.PointerMoved -= ContentGrid_MouseMove_New;
                    ContentGrid.PointerMoved += ContentGrid_MouseMove;
                }
                return;
            }
        }

        vm.InitPoint = e.GetPosition(ContentGrid);

        if (isNewCharacter)
        {
            vm.SelectedGlyph.SourceX = (ushort)Math.Round(vm.InitPoint.X);
            vm.SelectedGlyph.SourceY = (ushort)Math.Round(vm.InitPoint.Y);
            vm.InitShift = vm.SelectedGlyph.Shift;
        }
        else
        {
            vm.InitType = GetHitType(vm.SelectedGlyph.Model, vm.SelectedRect, vm.InitPoint);
            if (vm.InitType == HitType.T || vm.InitType == HitType.UL || vm.InitType == HitType.UR)
                GlyphTopLine.IsVisible = true;
        }

        dragInProgress = true;
        ZoomBorder.EnablePan = false;
    }

    private void ContentGrid_MouseMove(object? sender, PointerEventArgs e)
    {
        if (DataContext is not EditGlyphRectangleViewModel vm)
            return;

        var pos = e.GetPosition(ContentGrid);

        if (dragInProgress)
        {
            double offsetX = pos.X - vm.InitPoint.X;
            double offsetY = pos.Y - vm.InitPoint.Y;

            double newX = vm.SelectedGlyph.SourceX;
            double newY = vm.SelectedGlyph.SourceY;
            double newWidth = vm.SelectedGlyph.SourceWidth;
            double newHeight = vm.SelectedGlyph.SourceHeight;
            double newShift = vm.SelectedGlyph.Shift;
            
            switch (vm.InitType)
            {
                case HitType.Body:
                    newX += offsetX;
                    newY += offsetY;
                    break;
                case HitType.UL:
                    newX += offsetX;
                    newY += offsetY;
                    newWidth -= offsetX;
                    newHeight -= offsetY;
                    newShift -= offsetX;
                    break;
                case HitType.UR:
                    newY += offsetY;
                    newWidth += offsetX;
                    newHeight -= offsetY;
                    newShift += offsetX;
                    break;
                case HitType.LR:
                    newWidth += offsetX;
                    newHeight += offsetY;
                    newShift += offsetX;
                    break;
                case HitType.LL:
                    newX += offsetX;
                    newWidth -= offsetX;
                    newHeight += offsetY;
                    newShift -= offsetX;
                    break;
                case HitType.L:
                    newX += offsetX;
                    newWidth -= offsetX;
                    newShift -= offsetX;
                    break;
                case HitType.R:
                    newWidth += offsetX;
                    newShift += offsetX;
                    break;
                case HitType.B:
                    newHeight += offsetY;
                    break;
                case HitType.T:
                    newY += offsetY;
                    newHeight -= offsetY;
                    break;
            }

            if (Math.Abs(offsetX) < 1 && Math.Abs(offsetY) < 1)
                return;

            bool outOfLeft = newX < 0;
            bool outOfTop = newY < 0;
            bool outOfRight = newX + newWidth > vm.Font.Texture.BoundingWidth;
            bool outOfBottom = newY + newHeight > vm.Font.Texture.BoundingHeight;
            if (!outOfLeft && !outOfRight)
                vm.SelectedGlyph.SourceX = (ushort)Math.Round(newX);
            if (!outOfTop && !outOfBottom)
                vm.SelectedGlyph.SourceY = (ushort)Math.Round(newY);
            if (newWidth > 0 && !outOfRight)
                vm.SelectedGlyph.SourceWidth = (ushort)Math.Round(newWidth);
            if (newHeight > 0 && !outOfBottom)
                vm.SelectedGlyph.SourceHeight = (ushort)Math.Round(newHeight);

            if (outOfLeft)
                vm.SelectedGlyph.SourceX = 0;
            if (outOfRight)
                vm.SelectedGlyph.SourceX = (ushort)(vm.Font.Texture.BoundingWidth - vm.SelectedGlyph.SourceWidth);
            if (outOfTop)
                vm.SelectedGlyph.SourceY = 0;
            if (outOfBottom)
                vm.SelectedGlyph.SourceY = (ushort)(vm.Font.Texture.BoundingHeight - vm.SelectedGlyph.SourceHeight);

            vm.SelectedGlyph.Shift = (short)Math.Round(newShift);

            vm.InitPoint = vm.InitPoint.WithX(Math.Round(pos.X));
            vm.InitPoint = vm.InitPoint.WithY(Math.Round(pos.Y));
        }
        else
        {
            var hitType = GetHitType(vm.SelectedGlyph.Model, vm.SelectedRect, pos);
            ContentGrid.Cursor = GetCursorForType(hitType);
        }
    }

    private void ContentGrid_MouseMove_New(object? sender, PointerEventArgs e)
    {
        if (DataContext is not EditGlyphRectangleViewModel vm)
            return;

        var pos = e.GetPosition(ContentGrid);
        
        if (dragInProgress)
        {
            double offsetX = pos.X - vm.InitPoint.X;
            double offsetY = pos.Y - vm.InitPoint.Y;

            if (offsetX < 1 || offsetY < 1)
                return;

            bool outOfRight = vm.SelectedGlyph.SourceX + offsetX > vm.Font.Texture.BoundingWidth;
            bool outOfBottom = vm.SelectedGlyph.SourceY + offsetY > vm.Font.Texture.BoundingHeight;
            if (!outOfRight)
                vm.SelectedGlyph.SourceWidth = (ushort)Math.Round(offsetX);
            if (!outOfBottom)
                vm.SelectedGlyph.SourceHeight = (ushort)Math.Round(offsetY);

            vm.SelectedGlyph.Shift = (short)(vm.InitShift + (short)Math.Round(offsetX));
        }
        else
        {
            vm.SelectedGlyph.SourceX = (ushort)Math.Round(pos.X);
            vm.SelectedGlyph.SourceY = (ushort)Math.Round(pos.Y);
        }
    }

    private void ContentGrid_MouseLeftButtonUp(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (DataContext is not EditGlyphRectangleViewModel vm)
            return;

        if (isNewCharacter)
        {
            isNewCharacter = false;
            ContentGrid.PointerMoved -= ContentGrid_MouseMove_New;
            ContentGrid.PointerMoved += ContentGrid_MouseMove;
            ContentGrid.Cursor = new Cursor(StandardCursorType.Arrow);
        }
        else
            GlyphTopLine.IsVisible = false;

        dragInProgress = false;
        ZoomBorder.EnablePan = true;
    }

    private void Canvas_Loaded(object? sender, RoutedEventArgs? e)
    {
        if (DataContext is not EditGlyphRectangleViewModel vm)
            return;

        canvas = sender as Canvas;
        UpdateSelectedRect();

        if (isNewCharacter)
        {
            vm.SelectedRect = vm.SelectedRect.WithWidth(1);
            vm.SelectedRect = vm.SelectedRect.WithHeight(1);
        }
    }
}