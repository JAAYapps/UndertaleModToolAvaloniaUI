using System;
using System.Data;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace UndertaleModToolAvalonia.Converters.ControlConverters;

public class NonexistentColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
        {
            return Brushes.Gray;
        }
        if (Application.Current!.TryGetResource("TreeViewItemForeground", Application.Current.ActualThemeVariant, out var brush))
            return brush;
        if (Application.Current.ActualThemeVariant == ThemeVariant.Dark)
        {
            return Brushes.White;
        }

        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}