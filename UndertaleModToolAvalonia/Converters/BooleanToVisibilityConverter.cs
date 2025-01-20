using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia.Converters
{
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public bool trueValue { get; set; }
        public bool falseValue { get; set; }
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolean && boolean) ? trueValue : falseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
