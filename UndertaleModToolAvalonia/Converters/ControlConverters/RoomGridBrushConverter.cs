using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class RoomGridBrushConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3 || values[0] is not double width || values[1] is not double height || values[2] is not double thickness)
                return null;
            if (width <= 0 || height <= 0 || thickness <= 0)
                return null;

            Rect rect = new(0, 0, width, height);
            return new DrawingBrush()
            {
                TileMode = TileMode.Tile,
                SourceRect = new RelativeRect(rect, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(rect, RelativeUnit.Absolute),
                Drawing = new GeometryDrawing()
                {
                    Geometry = new RectangleGeometry(rect),
                    Pen = new Pen(Brushes.Gray, thickness)
                }
            };
        }
    }
}
