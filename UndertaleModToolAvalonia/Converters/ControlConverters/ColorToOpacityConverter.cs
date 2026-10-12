using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class ColorToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not uint color)
                return 1.0;

            return (color >> 24) / 255.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
