using Avalonia.Data.Converters;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class ColorTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                uint val = System.Convert.ToUInt32(value);
                bool hasAlpha = bool.Parse((string)parameter);
                return "#" + (hasAlpha ? val.ToString("X8") : val.ToString("X8")[2..]);
            }
            catch (Exception ex)
            {
                return new ValidationResult(ex.Message);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string val = (string)value;
                bool hasAlpha = bool.Parse((string)parameter);

                if (val[0] != '#')
                    return new ValidationResult("Invalid color string");

                val = val[1..];
                if (val.Length != (hasAlpha ? 8 : 6))
                    return new ValidationResult("Invalid color string");

                if (!hasAlpha)
                    val = "FF" + val; // add alpha (255)

                return System.Convert.ToUInt32(val, 16);
            }
            catch (Exception ex)
            {
                return new ValidationResult(ex.Message);
            }
        }
    }
}
