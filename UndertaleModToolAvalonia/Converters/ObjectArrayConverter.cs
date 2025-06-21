using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Converters
{
    public class ObjectArrayConverter : IMultiValueConverter
    {
        public static readonly ObjectArrayConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            return new List<object?>(values).ToArray();
        }
    }
}
