using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UndertaleModToolAvalonia.Converters
{
    public class DoubleToUshortConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ushort us)
            {
                return (double)us;
            }
            return 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                // Safely round the double and clamp it to the valid ushort range
                var rounded = Math.Round(d);
                var clamped = Math.Clamp(rounded, ushort.MinValue, ushort.MaxValue);
                return (ushort)clamped;
            }
            return (ushort)0;
        }
    }
}
