using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Linq;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleEmbeddedTextureEditorView : UserControl
{
    private readonly MenuFlyout pageContextMenu = new();
    private bool isMenuOpen;
    private Point lastMousePosition;
    private Size lastExtent;

    public UndertaleEmbeddedTextureEditorView()
    {
        InitializeComponent();

        // Happens when in designer mode in avalonia.
        if (DataContext is not UndertaleEmbeddedTextureEditorViewModel vm)
            vm = new UndertaleEmbeddedTextureEditorViewModel("Design View", null, new ViewModels.EditorViewModels.EditorViewModel(), null, null);

        pageContextMenu.Closed += PageContextMenu_Closed;

        DataContextChanged += SwitchDataContext;
        Unloaded += UnloadTexture;
        TextureGrid.PointerPressed += TextureGrid_PointerPressed;
        TextureGrid.PointerMoved += TextureGrid_PointerMoved;
        TextureGrid.PointerExited += TextureGrid_PointerExited;
    }

    private void TextureGrid_PointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is not UndertaleEmbeddedTextureEditorViewModel vm)
            return;

        if (isMenuOpen)
            return;

        PageItemBorder.Width = PageItemBorder.Height = 0;
        vm.HoveredItem = null;
    }

    private void TextureGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not UndertaleEmbeddedTextureEditorViewModel vm || vm.Items is null || vm.EmbeddedTexture is null || TexturePageImage.Source is null)
        {
            PageItemBorder.Width = PageItemBorder.Height = 0;
            return;
        }

        var posOnImage = e.GetPosition(TexturePageImage);

        var imageBounds = TexturePageImage.Bounds;
        if (imageBounds.Width <= 0 || imageBounds.Height <= 0)
            return;

        var sourceBitmap = (Bitmap)TexturePageImage.Source;
        var sourceSize = sourceBitmap.Size;

        // Scale factor of the rendered image vs. the source bitmap (e.g., 800px wide render / 1024px wide bitmap).
        // I had to do this to account for the PanAndZoom changes to the size of the image displayed so I can use the zoom factor to ajust the width and heigh to properly get the correct detection.
        double imageStretchScaleX = imageBounds.Width / sourceSize.Width;
        double imageStretchScaleY = imageBounds.Height / sourceSize.Height;

        // The mouse position needs to be unstretched to find the corresponding pixel on the bitmap.
        var posOnBitmap = new Point(
            posOnImage.X / imageStretchScaleX,
            posOnImage.Y / imageStretchScaleY
        );

        float gameTextureScale = vm.EmbeddedTexture.Scaled > 0 ? vm.EmbeddedTexture.Scaled : 1.0f;

        // Scale the position UP to match the item data's coordinate system (e.g., from 1024px space to 2048px space).
        var finalPos = new Point(
            posOnBitmap.X * gameTextureScale,
            posOnBitmap.Y * gameTextureScale
        );

        // The rest of the old code from the old tool is mostly the same.
        var prevItem = vm.HoveredItem;
        vm.HoveredItem = null;

        foreach (var item in vm.Items)
        {
            if (finalPos.X >= item.SourceX && finalPos.X < item.SourceX + item.SourceWidth
                && finalPos.Y >= item.SourceY && finalPos.Y < item.SourceY + item.SourceHeight)
            {
                vm.HoveredItem = item;
                break;
            }
        }

        if (vm.HoveredItem is null)
        {
            PageItemBorder.Width = PageItemBorder.Height = 0;
            return;
        }

        if (prevItem == vm.HoveredItem)
            return;

        // Drawing the highlight border by scaling the item data DOWN to the final rendered size.
        // This reverses the process: (Original Coords / Game Scale) * Image Stretch Scale.
        double left = (vm.HoveredItem.SourceX / gameTextureScale) * imageStretchScaleX;
        double top = (vm.HoveredItem.SourceY / gameTextureScale) * imageStretchScaleY;
        double width = (vm.HoveredItem.SourceWidth / gameTextureScale) * imageStretchScaleX;
        double height = (vm.HoveredItem.SourceHeight / gameTextureScale) * imageStretchScaleY;

        PageItemBorder.Width = width;
        PageItemBorder.Height = height;
        Canvas.SetLeft(PageItemBorder, left);
        Canvas.SetTop(PageItemBorder, top);
    }

    private void TextureGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not UndertaleEmbeddedTextureEditorViewModel vm)
            return;

        if (vm.HoveredItem is null)
            return;

        if (e.Properties.IsRightButtonPressed)
        {
            pageContextMenu.Items.Clear();
            var newTabItem = new MenuItem()
            {
                Header = "Open in new tab"
            };
            newTabItem.Command = vm.editorViewModel.OpenAssetInNewTabCommand;
            newTabItem.CommandParameter = vm.HoveredItem;
            var referencesItem = new MenuItem()
            {
                Header = "Find all references to this page item"
            };
            referencesItem.Command = vm.editorViewModel.FindAllReferencesCommand;
            referencesItem.CommandParameter = vm.HoveredItem;
            pageContextMenu.Items.Add(newTabItem);
            pageContextMenu.Items.Add(referencesItem);
            isMenuOpen = true;
            pageContextMenu.ShowAt(PageItemBorder);
            return;
        }
        else if (e.Properties.IsLeftButtonPressed && e.KeyModifiers != KeyModifiers.Control)
            vm.editorViewModel.OpenAssetInTabCommand.Execute(vm.HoveredItem);
        else if (e.Properties.IsLeftButtonPressed && e.KeyModifiers == KeyModifiers.Control)
            vm.editorViewModel.OpenAssetInNewTabCommand.Execute(vm.HoveredItem);
        else if (e.Properties.IsMiddleButtonPressed && e.KeyModifiers != KeyModifiers.Control)
            vm.editorViewModel.OpenAssetInNewTabCommand.Execute(vm.HoveredItem);
    }

    private void ReloadTextureImage(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not UndertaleEmbeddedTextureEditorViewModel vm)
            return;

        UndertaleEmbeddedTexture texture = vm.EmbeddedTexture;

        if (!IsLoaded)
            return;
        
        if (texture is null)
            return;

        if (e.PropertyName != nameof(UndertaleEmbeddedTexture.TexData.Image))
            return;

        // If the texture's image was updated, reload it
        UpdateImage(texture);
    }

    private void UnloadTexture(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleEmbeddedTextureEditorViewModel vm)
            return;

        TexturePageImage.Source = null;

        // Stop listening for texture image updates
        if (vm.TextureDataContext is not null)
        {
            vm.TextureDataContext.PropertyChanged -= ReloadTextureImage;
            vm.TextureDataContext = null;
        }
    }

    private void SwitchDataContext(object? sender, EventArgs e)
    {
        if (DataContext is not UndertaleEmbeddedTextureEditorViewModel vm)
            return;

        UndertaleEmbeddedTexture texture = vm.EmbeddedTexture;
        if (texture is null)
        {
            vm.Items = Array.Empty<UndertaleTexturePageItem>(); // Ensure Items is never null
            return;
        }

        // Initialize Items as soon as the DataContext is valid.
        vm.Items = AppConstants.Data!.TexturePageItems.Where(x => x.TexturePage == texture).ToArray();

        // Load current image
        UpdateImage(texture);

        // Start listening for texture image updates
        if (vm.TextureDataContext is not null)
        {
            vm.TextureDataContext.PropertyChanged -= ReloadTextureImage;
        }

        vm.TextureDataContext = texture.TextureData;

        if (vm.TextureDataContext is not null)
        {
            vm.TextureDataContext.PropertyChanged += ReloadTextureImage;
        }
    }

    private void PageContextMenu_Closed(object? sender, EventArgs e)
    {
        isMenuOpen = false;
        TextureGrid_PointerExited(null, null);
    }

    private void UpdateImage(UndertaleEmbeddedTexture texture)
    {
        if (DataContext is not UndertaleEmbeddedTextureEditorViewModel vm)
            return;

        if (texture.TextureData?.Image is null)
        {
            TexturePageImage.Source = null;
            return;
        }

        GMImage image = texture.TextureData.Image;
        Bitmap bitmap = vm.textureCacheService.GetBitmapForImage(image);
        TexturePageImage.Source = bitmap;
    }
}