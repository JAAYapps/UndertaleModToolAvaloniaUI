﻿using System;
using System.Globalization;
using Avalonia.Data.Converters;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Converters
{
    //[ValueConversion(typeof(uint), typeof(string))]
    public class EventNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string par && par == "EventType")
                return Enum.Parse(typeof(EventType), (string)value);

            uint val = System.Convert.ToUInt32(value);
            return ((EventType)val).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (uint)(EventType)Enum.Parse(typeof(EventType), (string)value);
        }
    }
}
