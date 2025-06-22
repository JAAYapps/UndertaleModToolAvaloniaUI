using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia;

namespace UndertaleModToolAvalonia.Converters
{
    public class IndentStyleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DecompilerSettings.IndentStyleKind kind)
            {
                return null;
            }
            return kind switch
            {
                DecompilerSettings.IndentStyleKind.FourSpaces => "4 spaces",
                DecompilerSettings.IndentStyleKind.TwoSpaces => "2 spaces",
                DecompilerSettings.IndentStyleKind.Tabs => "Tabs",
                _ => throw new Exception("Unknown indent style kind")
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
