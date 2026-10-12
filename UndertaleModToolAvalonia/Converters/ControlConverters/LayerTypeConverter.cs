using Avalonia.Data.Converters;
using System;
using System.Globalization;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class LayerTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool includeBGLayer = parameter is string par && par == "Canvas";
            return !(value is LayerType.Instances || (includeBGLayer && value is LayerType.Background));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
