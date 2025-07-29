using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class TimestampDateTimeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not ulong timestamp)
                return "(error)";
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds((long)timestamp);
            if (parameter is string par && par == "GMT")
                return "GMT+0: " + dateTimeOffset.UtcDateTime.ToString();
            else
                return dateTimeOffset.LocalDateTime.ToString();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string dateTimeStr)
                return new ValidationResult("The value is not a string.");
            if (!DateTime.TryParse(dateTimeStr, out DateTime dateTime))
                return new ValidationResult("Invalid date time format.");

            return (ulong)(new DateTimeOffset(dateTime).ToUnixTimeSeconds());
        }
    }
}
