using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UndertaleModToolAvalonia.Converters
{
    public class IsNullConverter : IValueConverter
    {
        public static readonly IsNullConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool invert = parameter is string par && par == "Invert";
            return value is null ^ invert;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
