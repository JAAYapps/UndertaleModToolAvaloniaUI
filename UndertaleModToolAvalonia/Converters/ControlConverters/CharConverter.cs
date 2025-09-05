using Avalonia.Data.Converters;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class CharConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            ushort charNum;
            try
            {
                // Handles both ushort and short inputs safely
                charNum = System.Convert.ToUInt16(value);
            }
            catch
            {
                return "(error)";
            }

            if (charNum == 0)
                return string.Empty; // Return an empty string for null/0 character

            return System.Convert.ToChar(charNum).ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string charStr || string.IsNullOrEmpty(charStr))
            {
                return (ushort)0;
            }

            // A string can be more than one character, but we only care about the first one.
            uint charCode = charStr[0];

            if (charCode > ushort.MaxValue)
            {
                // Instead of showing a message box, return a validation result.
                // The UI binding system can use this to show an error state (e.g., a red border).
                return new ValidationResult("Character code cannot be greater than 65535.");
            }

            return (ushort)charCode;
        }
    }
}
