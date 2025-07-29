using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class EnumToValuesConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return Array.Empty<object>();
            }
            return Enum.GetValues(value.GetType());
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
