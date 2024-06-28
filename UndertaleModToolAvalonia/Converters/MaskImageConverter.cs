using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia
{
    public class MaskImageConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(e => e == AvaloniaProperty.UnsetValue))
            {
                return null;
            }
            
            uint width = (uint)values[0];
            uint height = (uint)values[1];
            byte[] data = (byte[])values[2];
            if (data == null || data.Length != (width + 7) / 8 * height || width == 0 || height == 0)
                return null;
            
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
