using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Converters
{
    //[ValueConversion(typeof(uint), typeof(UndertaleGameObject))]
    public class GameObjectByIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint val = System.Convert.ToUInt32(value);
            UndertaleGameObject returnObj = null;
            if (val < AppConstants.Data.GameObjects.Count)
            {
                returnObj = AppConstants.Data.GameObjects[(int)val];
                return returnObj;
            }
            else
            {
                return returnObj;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (uint)AppConstants.Data.GameObjects.IndexOf((UndertaleGameObject)value);
        }
    }
}
