using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UndertaleModToolAvalonia
{
    public class SumRectConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(e => e == AvaloniaProperty.UnsetValue))
            {
                return null;
            }
            return new Rect((ushort)values[0] + (uint)values[1], (ushort)values[2] + (uint)values[3], (uint)values[4], (uint)values[5]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
