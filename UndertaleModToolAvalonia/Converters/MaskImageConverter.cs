using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace UndertaleModToolAvalonia
{
    public class MaskImageConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(e => e == AvaloniaProperty.UnsetValue))
            {
                return null;
            }
            
            uint width = (uint)values[0];
            uint height = (uint)values[1];
            byte[] data = (byte[])values[2];
            if (data == null || data.Length != (width + 7) / 8 * height || width == 0 || height == 0)
                return null;
            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();
            Bitmap bitmap = new Bitmap(PixelFormat.Rgb565, AlphaFormat.Opaque, pointer, new PixelSize((int)width, (int)height), new Vector(96, 96), (int)((width + 7) / 8));
            pinnedArray.Free();
            return bitmap;
            // Make sure to fix!!!
            // return BitmapSource.Create((int)width, (int)height, 96, 96, PixelFormats.BlackWhite, null, data, (int)((width + 7) / 8));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
