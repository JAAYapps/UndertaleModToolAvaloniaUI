using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Controls;

public partial class FlagsBox : UserControl
{
    public static readonly StyledProperty<Enum?> ValueProperty =
        AvaloniaProperty.Register<FlagsBox, Enum?>(
            nameof(Value),
            defaultBindingMode: BindingMode.TwoWay);

    public Enum? Value
    {
        get
        {
            Console.WriteLine("Getting type: " + Value.GetType());
            return GetValue(ValueProperty);
        }
        set
        {
            Console.WriteLine("Setting type: " + Value.GetType());
            SetValue(ValueProperty, value);
        }
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
            Value = (Enum?)Enum.ToObject(currentValue.GetType(), result);
        });
    }
}