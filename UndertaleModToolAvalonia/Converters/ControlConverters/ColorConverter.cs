using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint val = System.Convert.ToUInt32(value);
            return Color.FromArgb((byte)((val >> 24) & 0xff), (byte)(val & 0xff), (byte)((val >> 8) & 0xff), (byte)((val >> 16) & 0xff));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color val = (Color)value;
            return (val.A << 24) | (val.B << 16) | (val.G << 8) | val.R;
        }
    }
}
