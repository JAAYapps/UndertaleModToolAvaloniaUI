using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Converters
{
    public class IsNonNullObjectConverter : IValueConverter
    {
        public static readonly IsNonNullObjectConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // "NewItemPlaceholderName" is internal to Avalonia's DataGrid, 
            // checking the type name is a reliable way to identify it.
            return value is not null && value.GetType().Name != "NewItemPlaceholder";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
