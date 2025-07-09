using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Runtime.Serialization;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Controls;

public partial class UndertaleTexturePageItemDisplay : UserControl
{
    public static readonly StyledProperty<bool?> DisplayBorderProperty =
    AvaloniaProperty.Register<UndertaleTexturePageItemDisplay, bool?>(
        nameof(DisplayBorder), defaultValue: true, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public bool DisplayBorder
    {
        get { return (bool)GetValue(DisplayBorderProperty); }
        set { SetValue(DisplayBorderProperty, value); }
    }

    public UndertaleTexturePageItemDisplay()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DisplayBorderProperty)
        {
            if (change.NewValue is not bool val)
                return;
            RenderAreaBorder.BorderThickness = new Thickness(val ? 1 : 0);
        }
    }
}