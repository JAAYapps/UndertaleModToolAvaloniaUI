using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace UndertaleModToolAvalonia.Controls;

public partial class FlagsBox : UserControl
{
    public static readonly StyledProperty<object> ValueProperty =
        AvaloniaProperty.Register<FlagsBox, object>(
            nameof(Value),
            defaultBindingMode: BindingMode.TwoWay);

    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public ICommand ToggleFlagCommand { get; }

    public FlagsBox()
    {
        InitializeComponent();

        ToggleFlagCommand = new RelayCommand<object>(flagValue =>
        {
            if (Value is not Enum currentValue || flagValue is not Enum flag) return;

            // Get the underlying numeric type of the enum (e.g., int, uint, long)
            var underlyingType = Enum.GetUnderlyingType(currentValue.GetType());

            // Perform a bitwise XOR operation to toggle the flag
            // We need to convert to ulong for a universal bitwise operation
            ulong currentNumericValue = Convert.ToUInt64(currentValue);
            ulong flagNumericValue = Convert.ToUInt64(flag);

            ulong result = currentNumericValue ^ flagNumericValue;

            // Convert the result back to the original enum type and update the Value
            Value = Enum.ToObject(currentValue.GetType(), result);
        });
    }
}