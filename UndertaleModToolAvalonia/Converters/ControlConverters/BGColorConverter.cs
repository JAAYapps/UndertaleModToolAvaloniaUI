using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Converters.ControlConverters;

public class BGColorConverter : IMultiValueConverter
{
    private static readonly ColorConverter colorConv = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3)
            return Brushes.Black;
        
        if (values[0] is not UndertaleRoom.RoomEntryFlags flags) 
            return Brushes.Black;
        
        bool isGMS2 = flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) || flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13);

        object? rawColorObj = isGMS2 ? values[1] : values[2];
        
        Color color = Colors.Black;
        if (rawColorObj is uint uVal)
            color = Color.FromUInt32(uVal); 
        
        

        return null;
    }
}
