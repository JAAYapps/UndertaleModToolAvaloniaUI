using Avalonia.Media.Imaging;
using System.IO;

namespace UndertaleModToolAvalonia.Utilities
{
    public class BitmapInfo
    {
        private BitmapInfo() { }
        
        private const int BytesPerPixel = 4;
 
        public static int GetBitsPerPixel(Bitmap bitmap) => BytesPerPixel * 8;
 
        public static int GetDepth(Bitmap bitmap) => BytesPerPixel;
 
        public static int GetStride(Bitmap bitmap) => BytesPerPixel * bitmap.PixelSize.Width;
 
        public static int GetStride(WriteableBitmap bitmap) => BytesPerPixel * bitmap.PixelSize.Width;
 
        public static WriteableBitmap BitmapToWritableBitmap(Bitmap bitmap)
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return WriteableBitmap.Decode(memoryStream);
        }
    }
}
