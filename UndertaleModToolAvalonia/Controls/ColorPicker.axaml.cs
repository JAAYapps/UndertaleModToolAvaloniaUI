using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using UndertaleModToolAvalonia.Converters.ControlConverters;

namespace UndertaleModToolAvalonia.Controls;

public partial class ColorPicker : UserControl
{
    public static readonly StyledProperty<uint?> ColorProperty =
        AvaloniaProperty.Register<ColorPicker, uint?>(
        nameof(Color), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool?> HasAlphaProperty =
        AvaloniaProperty.Register<ColorPicker, bool?>(
        nameof(Color), defaultValue: true, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public uint Color
    {
        get => (uint)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
    public bool HasAlpha
    {
        get => (bool)GetValue(HasAlphaProperty);
        set => SetValue(HasAlphaProperty, value); // we can't put here any other logic
    }

    public ColorPicker()
    {
        InitializeComponent();

        Binding binding = new("Color")
        {
            Converter = new ColorTextConverter(),
            ConverterParameter = HasAlpha.ToString(), // HasAlpha
            RelativeSource = new RelativeSource() { AncestorType = typeof(ColorPicker) },
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        ColorText.Bind(TextBox.TextProperty, binding);

        ColorText.MaxLength = HasAlpha ? 9 : 7;
        ToolTip.SetTip(ColorText, $"#{(HasAlpha ? "AA" : "")}BBGGRR");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HasAlphaProperty)
        {
            var newValue = change.GetNewValue<bool?>();
            OnHasAlphaChanged(newValue);
        }
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        OnHasAlphaChanged(HasAlpha);
    }

    private void OnHasAlphaChanged(bool? newValue)
    {
        bool? hasAlpha = newValue;
        ColorPicker colorPicker = this;

        Binding binding = new("Color")
        {
            Converter = new ColorTextConverter(),
            ConverterParameter = hasAlpha.ToString(), // HasAlpha
            RelativeSource = new RelativeSource() { AncestorType = typeof(ColorPicker) },
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        colorPicker.ColorText.Bind(TextBox.TextProperty, binding);

        colorPicker.ColorText.MaxLength = hasAlpha == true ? 9 : 7;
        ToolTip.SetTip(colorPicker.ColorText, $"#{(hasAlpha == true ? "AA" : "")}BBGGRR");
    }
}