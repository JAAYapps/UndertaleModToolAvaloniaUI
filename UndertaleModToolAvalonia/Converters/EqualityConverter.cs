using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia.Converters
{
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                return values;

            if (values.Count < 2)
                return false;

            bool invert = parameter is string par && par == "invert";
            return (values[0] == values[1]) ^ invert;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
