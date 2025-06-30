using Avalonia;
using Avalonia.Controls;
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
    internal class StringReferenceBackgroundConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2) return AvaloniaProperty.UnsetValue;

            var objectReference = values[0];
            var text = values[1] as string;

            var control = parameter as Control;
            control!.TryFindResource("NullBrush", out var nullBrush);
            control!.TryFindResource("EmptyBrush", out var emptyBrush);

            if (objectReference is null && string.IsNullOrEmpty(text))
            {
                return nullBrush;
            }

            if (string.IsNullOrEmpty(text))
            {
                return emptyBrush;
            }

            return Brushes.Transparent;
        }
    }
}
