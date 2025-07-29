using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class TextureLoadedWrapperConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(v => v == AvaloniaProperty.UnsetValue))
            {
                // Return collapsed until values are known
                return false;
            }

            bool textureLoaded, textureExternal;
            try
            {
                textureLoaded = (bool)values[0];
                textureExternal = (bool)values[1];
            }
            catch
            {
                return false;
            }

            return (textureLoaded || !textureExternal) ? false : true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
