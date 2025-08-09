using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleTexturePageItemEditorView : UserControl
{
    public UndertaleTexturePageItemEditorView()
    {
        InitializeComponent();
        RenderOptions.SetBitmapInterpolationMode(ImageView, BitmapInterpolationMode.None);
        DataContextChanged += SwitchDataContext;
        Unloaded += UnloadTexture;
    }

    private void UpdateImages(UndertaleTexturePageItem item)
    {
        if (item.TexturePage?.TextureData?.Image is null)
        {
            ItemTextureBGImage.Source = null;
            ItemTextureImage.Source = null;
            return;
        }

        GMImage image = item.TexturePage.TextureData.Image;
        Bitmap bitmap = (DataContext as UndertaleTexturePageItemEditorViewModel).TextureCacheService.GetBitmapForImage(image);
        ItemTextureBGImage.Source = bitmap;
        ItemTextureImage.Source = bitmap;
    }

    private void SwitchDataContext(object? sender, EventArgs e)
    {
        Console.WriteLine("Data Context Changed.");
        UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItemEditorViewModel).TexturePageItem;
        if (item is null)
            return;

        // Load current image
        UpdateImages(item);

        // Start listening for texture page updates
        if ((DataContext as UndertaleTexturePageItemEditorViewModel).TextureItemContext is not null)
        {
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureItemContext.PropertyChanged -= ReloadTexturePage;
        }
        (DataContext as UndertaleTexturePageItemEditorViewModel).TextureItemContext = item;
        (DataContext as UndertaleTexturePageItemEditorViewModel).TextureItemContext.PropertyChanged += ReloadTexturePage;

        // Start listening for texture image updates
        if ((DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext is not null)
        {
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext.PropertyChanged -= ReloadTextureImage;
        }

        if (item.TexturePage?.TextureData is not null)
        {
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext = item.TexturePage.TextureData;
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext.PropertyChanged += ReloadTextureImage;
        }
    }

    private void ReloadTexturePage(object sender, PropertyChangedEventArgs e)
    {
        // Invoke dispatcher to only perform updates on UI thread
        Dispatcher.UIThread.Invoke(() =>
        {
            UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItemEditorViewModel).TexturePageItem;
            if (item is null)
                return;

            if (e.PropertyName != nameof(UndertaleTexturePageItem.TexturePage))
                return;

            UpdateImages(item);

            // Start listening for (new) texture image updates
            if ((DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext is not null)
            {
                (DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext.PropertyChanged -= ReloadTextureImage;
            }
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext = item.TexturePage.TextureData;
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext.PropertyChanged += ReloadTextureImage;
        });
    }

    private void ReloadTextureImage(object sender, PropertyChangedEventArgs e)
    {
        // Invoke dispatcher to only perform updates on UI thread
        Dispatcher.UIThread.Invoke(() =>
        {
            UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItemEditorViewModel).TexturePageItem;
            if (item is null)
                return;

            if (e.PropertyName != nameof(UndertaleEmbeddedTexture.TexData.Image))
                return;

            // If the texture's image was updated, reload it
            UpdateImages(item);
        });
    }

    private void UnloadTexture(object? sender, RoutedEventArgs e)
    {
        ItemTextureBGImage.Source = null;
        ItemTextureImage.Source = null;

        // Stop listening for texture page updates
        if ((DataContext as UndertaleTexturePageItemEditorViewModel).TextureItemContext is not null)
        {
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureItemContext.PropertyChanged -= ReloadTexturePage;
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureItemContext = null;
        }

        // Stop listening for texture image updates
        if ((DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext is not null)
        {
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext.PropertyChanged -= ReloadTextureImage;
            (DataContext as UndertaleTexturePageItemEditorViewModel).TextureDataContext = null;
        }
    }

    private void UpdateSpinnerValue(string spinnerName, bool increase)
    {
        if (DataContext is not UndertaleTexturePageItemEditorViewModel vm)
            return;

        ushort amount = (ushort)(increase ? 1 : -1);

        switch (spinnerName)
        {
            case "SourceXSpinner": vm.SourceX += amount; break;
            case "SourceYSpinner": vm.SourceY += amount; break;
            case "SourceWidthSpinner": vm.SourceWidth += amount; break;
            case "SourceHeightSpinner": vm.SourceHeight += amount; break;
            case "TargetXSpinner": vm.TargetX += amount; break;
            case "TargetYSpinner": vm.TargetY += amount; break;
            case "TargetWidthSpinner": vm.TargetWidth += amount; break;
            case "TargetHeightSpinner": vm.TargetHeight += amount; break;
            case "BoundingWidthSpinner": vm.BoundingWidth += amount; break;
            case "BoundingHeightSpinner": vm.BoundingHeight += amount; break;
        }
    }

    private void OnSpinnerSpin(object? sender, SpinEventArgs e)
    {
        if (sender is ButtonSpinner spinner)
        {
            UpdateSpinnerValue(spinner.Name!, e.Direction == SpinDirection.Increase);
        }
    }

    private void OnSpinnerMouseWheel(object? sender, PointerWheelEventArgs e)
    {
        if (sender is ButtonSpinner spinner)
        {
            UpdateSpinnerValue(spinner.Name!, e.Delta.Y > 0);
            e.Handled = true;
        }
    }

    private void OnSliderAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Slider slider) return;

        string? propertyName = slider.Name switch
        {
            "SourceXSlider" => nameof(UndertaleTexturePageItemEditorViewModel.SourceX),
            "SourceYSlider" => nameof(UndertaleTexturePageItemEditorViewModel.SourceY),
            "SourceWidthSlider" => nameof(UndertaleTexturePageItemEditorViewModel.SourceWidth),
            "SourceHeightSlider" => nameof(UndertaleTexturePageItemEditorViewModel.SourceHeight),
            "TargetXSlider" => nameof(UndertaleTexturePageItemEditorViewModel.TargetX),
            "TargetYSlider" => nameof(UndertaleTexturePageItemEditorViewModel.TargetY),
            "TargetWidthSlider" => nameof(UndertaleTexturePageItemEditorViewModel.TargetWidth),
            "TargetHeightSlider" => nameof(UndertaleTexturePageItemEditorViewModel.TargetHeight),
            "BoundingWidthSlider" => nameof(UndertaleTexturePageItemEditorViewModel.BoundingWidth),
            "BoundingHeightSlider" => nameof(UndertaleTexturePageItemEditorViewModel.BoundingHeight),
            _ => null
        };

        if (propertyName is null) return;

        // Create a new TwoWay binding for the Value property
        var twoWayBinding = new Binding(propertyName, BindingMode.TwoWay);

        // Apply the new binding. This overwrites the OneWay binding from the XAML.
        slider.Bind(Slider.ValueProperty, twoWayBinding);

        // Detach the event handler so this code only runs once per control instance
        slider.AttachedToVisualTree -= OnSliderAttached;
    }
}