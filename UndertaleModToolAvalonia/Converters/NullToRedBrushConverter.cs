using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Converters
{
    public class NullToRedBrushConverter : IValueConverter
    {
        public static readonly NullToRedBrushConverter Instance = new();

        // Define the brushes once for efficiency
        private static readonly IBrush NormalBrush = new SolidColorBrush(Colors.Gray);
        private static readonly IBrush ErrorBrush = new SolidColorBrush(Colors.Red);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is null ? ErrorBrush : NormalBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
