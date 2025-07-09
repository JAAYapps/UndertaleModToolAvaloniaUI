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
    // TODO Test this once any of the editors are migrated
    public class MaskImageConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(e => e == AvaloniaProperty.UnsetValue || e is null))
            {
                return null;
            }

            int width;
            if (values[0] is int w_int)
                width = w_int;
            else if (values[0] is uint w_uint)
                width = (int)w_uint;
            else
                return null;

            int height;
            if (values[1] is int h_int)
                height = h_int;
            else if (values[1] is uint h_uint)
                height = (int)h_uint;
            else
                return null;

            if (values[2] is not byte[] data)
                return null;

            int stride = (width + 7) / 8;
            if (data.Length != stride * height || width <= 0 || height <= 0)
            {
                return null;
            }

            // Avalonia's WriteableBitmap only has a format that understands (Bgra8888) and there is no BlackAndWhite because of SkiaSharp
            var bitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Opaque);

            // Must Lock the bitmap's memory to write the pixel data to it.
            using (var frameBuffer = bitmap.Lock())
            {
                // This pointer goes to the beginning of the bitmap's memory
                IntPtr pBackBuffer = frameBuffer.Address;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Goes through each bit inside the bitmap
                        int byteIndex = (y * stride) + (x / 8);
                        int bitIndex = x % 8;

                        // Check the bit
                        bool isBitSet = (data[byteIndex] & (1 << (7 - bitIndex))) != 0;

                        // Calculating the position in the destination bitmap's memory
                        // Each pixel takes 4 bytes (ARGB)
                        // Using the base address of the pBackBuffer pointer
                        // RowBytes is basiclly a line in the bitmap (Ex. 1920x1080, the line is the 1920 bytes or the pixels in the row)
                        // Add the x to the line we got, the 4 multiplication is to align by 4 bytes (the ARGB data.)
                        long pDest = pBackBuffer.ToInt64() + (y * frameBuffer.RowBytes) + (x * 4);

                        if (isBitSet)
                        {
                            // Setting to it's maximum value
                            Marshal.WriteInt32(new IntPtr(pDest), unchecked((int)0xFFFFFFFF));
                        }
                        else
                        {
                            // Setting the A byte to it's max and the rest is all 0
                            // Alpha needs to be visible
                            Marshal.WriteInt32(new IntPtr(pDest), unchecked((int)0xFF000000));
                        }
                    }
                }
            }

            return bitmap;
        }
    }
}
