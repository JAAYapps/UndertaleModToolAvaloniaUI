﻿using Avalonia;
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
    public class RectConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            bool ignore = parameter is string par && par == "returnEmptyOnNull";

            if (values.Any(e => e == AvaloniaProperty.UnsetValue))
            {
                if (ignore)
                    return new Rect(0, 0, 0, 0);
                else
                    return null;
            }

            return new Rect((ushort)values[0], (ushort)values[1], (ushort)values[2], (ushort)values[3]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
