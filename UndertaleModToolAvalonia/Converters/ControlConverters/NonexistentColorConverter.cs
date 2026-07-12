using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace UndertaleModToolAvalonia.Converters.ControlConverters;

public class NonexistentColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
        {
            return Brushes.Gray;
        }
        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}