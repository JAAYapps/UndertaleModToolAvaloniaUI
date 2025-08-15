using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Runtime.Serialization;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Controls;

public partial class UndertaleTexturePageItemDisplay : UserControl
{
    public static readonly StyledProperty<bool?> DisplayBorderProperty =
    AvaloniaProperty.Register<UndertaleTexturePageItemDisplay, bool?>(
        nameof(DisplayBorder), defaultValue: true, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleTexturePageItem?> TextureItemProperty =
        AvaloniaProperty.Register<UndertaleTexturePageItemDisplay, UndertaleTexturePageItem?>(nameof(TextureItem));

    public static readonly StyledProperty<EditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<UndertaleTexturePageItemDisplay, EditorViewModel?>(nameof(Editor));

    public static readonly StyledProperty<ITextureCacheService?> TextureCacheServiceProperty =
        AvaloniaProperty.Register<UndertaleTexturePageItemDisplay, ITextureCacheService?>(nameof(TextureCacheService));

    public static readonly StyledProperty<IFileService?> FileServiceProperty =
        AvaloniaProperty.Register<UndertaleTexturePageItemDisplay, IFileService?>(nameof(FileService));

    public UndertaleTexturePageItem? TextureItem
    {
        get => GetValue(TextureItemProperty);
        set => SetValue(TextureItemProperty, value);
    }
    public EditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }
    public ITextureCacheService? TextureCacheService
    {
        get => GetValue(TextureCacheServiceProperty);
        set => SetValue(TextureCacheServiceProperty, value);
    }
    public IFileService? FileService
    {
        get => GetValue(FileServiceProperty);
        set => SetValue(FileServiceProperty, value);
    }

    public bool? DisplayBorder
    {
        get { return GetValue(DisplayBorderProperty); }
        set { SetValue(DisplayBorderProperty, value); }
    }

    public UndertaleTexturePageItemDisplay()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextureItemProperty ||
        change.Property == EditorProperty ||
        change.Property == TextureCacheServiceProperty ||
        change.Property == FileServiceProperty)
        {
            TryUpdateDataContext();
        }
        if (change.Property == DisplayBorderProperty)
        {
            if (change.NewValue is not bool val)
                return;
            RenderAreaBorder.BorderThickness = new Thickness(val ? 1 : 0);
        }
    }

    private void TryUpdateDataContext()
    {
        var currentItem = this.TextureItem;

        if (this.DataContext is UndertaleTexturePageItemEditorViewModel vm && vm.TexturePageItem == currentItem)
            return;

        if (currentItem == null)
        {
            this.DataContext = null;
            return;
        }

        if (Editor != null && TextureCacheService != null && FileService != null)
        {
            string title = currentItem.TexturePage.Name.Content;

            this.DataContext = new UndertaleTexturePageItemEditorViewModel(
                title,
                currentItem,
                this.Editor,
                this.TextureCacheService,
                this.FileService
            );
        }
    }
}