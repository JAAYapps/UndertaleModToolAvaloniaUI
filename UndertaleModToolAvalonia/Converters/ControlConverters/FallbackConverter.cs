using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class FallbackConverter : IMultiValueConverter
    {
        public static readonly FallbackConverter Instance = new();

        // Easier to check my remove command parameter in my UndertaleObjectReference if I don't assign it any value.
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // Return the first value in the list that is not null.
            return values.FirstOrDefault(v => v is not null);
        }
    }
}
