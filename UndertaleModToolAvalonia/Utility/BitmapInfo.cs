using Avalonia;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Utility
{
    public class BitmapInfo
    {
        private BitmapInfo() { }

        public static int GetDepth(Avalonia.Media.Imaging.Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                using (var image = SixLabors.ImageSharp.Image.Load(memoryStream))
                {
                    return image.PixelType.BitsPerPixel / 8;
                }
            }
        }

        public static Avalonia.Media.Imaging.WriteableBitmap BitmapToWritableBitmap(Avalonia.Media.Imaging.Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return Avalonia.Media.Imaging.WriteableBitmap.Decode(memoryStream);
            }
        }

        //public static Avalonia.Media.Imaging.WriteableBitmap ByteDataToBlackAndWhiteBitmap(byte[] data, int width, int height)
        //{
        //    using (var memoryStream = new MemoryStream(data))
        //    {
        //        memoryStream.Seek(0, SeekOrigin.Begin);
        //        // return 
        //        var wb = Avalonia.Media.Imaging.WriteableBitmap.Decode(memoryStream);
        //        using (MemoryStream ms = new MemoryStream(data))
        //        {
        //            new WriteableBitmap(new PixelSize((int)width, (int)height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgb565, Avalonia.Platform.AlphaFormat.Opaque);

        //        }
        //        return (, , PixelFormats.BlackWhite, null, data, (int)((width + 7) / 8));
        //    }
        //}

        public static int GetStride(Avalonia.Media.Imaging.Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var image = SixLabors.ImageSharp.Image.Load(memoryStream);
                return (image.PixelType.BitsPerPixel / 8) * image.Width;
            }
        }

        public static int GetStride(Avalonia.Media.Imaging.WriteableBitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var image = SixLabors.ImageSharp.Image.Load(memoryStream);
                return (image.PixelType.BitsPerPixel / 8) * image.Width;
            }
        }
    }
}
