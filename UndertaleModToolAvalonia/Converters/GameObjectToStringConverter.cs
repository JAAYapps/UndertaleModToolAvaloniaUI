using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Converters
{
    public sealed class GameObjectToStringConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is not GameObject gameObject)
            {
                return "(null)";
            }
            return gameObject.ToString();
        }
    }
}
