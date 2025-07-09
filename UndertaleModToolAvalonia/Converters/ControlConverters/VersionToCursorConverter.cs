using Avalonia.Data.Converters;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class VersionToCursorConverter : IValueConverter
    {
        // We will reuse your existing converter's logic
        private static readonly IsVersionAtLeastConverter versionChecker = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Check the version using the logic from your other converter
            bool isAtLeast = (bool)versionChecker.Convert(value, targetType, parameter, culture);

            // Return the correct cursor based on the result
            return isAtLeast ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
