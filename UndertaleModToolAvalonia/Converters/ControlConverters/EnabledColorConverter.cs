using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Platform;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace UndertaleModToolAvalonia.Converters.ControlConverters;

public class EnabledColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnabled && !isEnabled)
        {
            return Brushes.Gray;
        }
            
        // Default
        return AvaloniaProperty.UnsetValue; 
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotSupportedException();
}