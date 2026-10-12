using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia.Converters
{
    public class OptionValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string str)
                return null;

            switch (parameter)
            {
                case "boolean":
                    return str.ToLowerInvariant() == "true";

                case "number":
                    if (Double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                        return str;
                    return "0";

                default:
                    return str;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not string par)
                return BindingOperations.DoNothing;

            switch (par)
            {
                case "boolean":
                    if (value is not bool b)
                        return BindingOperations.DoNothing;
                    return (b ? "True" : "False");

                case "number":
                    if (value is string s && Double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                        return s;
                    return BindingOperations.DoNothing;

                default:
                    return BindingOperations.DoNothing;
            }
        }
    }
}
