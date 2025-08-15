using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class IsGM2023Converter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not UndertaleData data)
                return false;

            return data.IsVersionAtLeast(2023, 1);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
