using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia.Services.TextureCacheService
{
    /// <summary>
    /// Manages loading and caching GMImage textures as Avalonia Bitmaps.
    /// </summary>
    public class TextureCacheService : ITextureCacheService
    {
        //private readonly Dictionary<GMImage, WeakReference<Bitmap>> _bitmapCache = new();
        //private readonly object _cacheLock = new();

        ///// <summary>
        ///// Gets a cached or new Avalonia Bitmap for a given GMImage.
        ///// </summary>
        //public Bitmap GetBitmapForImage(GMImage image)
        //{
        //    lock (_cacheLock)
        //    {
        //        if (_bitmapCache.TryGetValue(image, out var weakRef) && weakRef.TryGetTarget(out var cachedBitmap))
        //        {
        //            return cachedBitmap;
        //        }

        //        byte[] pixelData = image.ConvertToRawBgra().ToSpan().ToArray();

        //        using var ms = new MemoryStream(pixelData);
        //        var writeableBitmap = new WriteableBitmap(
        //            new Avalonia.PixelSize(image.Width, image.Height),
        //            new Avalonia.Vector(96, 96), // DPI
        //            PixelFormat.Bgra8888,
        //            AlphaFormat.Premul);

        //        using (var lockedBuffer = writeableBitmap.Lock())
        //        {
        //            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, lockedBuffer.Address, pixelData.Length);
        //        }

        //        Bitmap finalBitmap;
        //        using (var saveStream = new MemoryStream())
        //        {
        //            writeableBitmap.Save(saveStream);
        //            saveStream.Position = 0;
        //            finalBitmap = new Bitmap(saveStream);
        //        }

        //        _bitmapCache[image] = new WeakReference<Bitmap>(finalBitmap);
        //        return finalBitmap;
        //    }
        //}

        /// <summary>
        /// Returns an Avalonia Bitmap instance for the given GMImage.
        /// This is the Avalonia equivalent of the GetBitmapSourceForImage method.
        /// </summary>
        public Bitmap GetBitmapForImage(GMImage image)
        {
            lock (_bitmapSourceLookupLock)
            {
                // This caching logic remains the same, just the type changes
                Bitmap foundBitmap = null;
                for (int i = _bitmapSourceLookup.Count - 1; i >= 0; i--)
                {
                    (GMImage imageKey, WeakReference<Bitmap> referenceVal) = _bitmapSourceLookup[i];
                    if (!referenceVal.TryGetTarget(out Bitmap bitmap))
                    {
                        _bitmapSourceLookup.RemoveAt(i);
                    }
                    else if (imageKey == image)
                    {
                        foundBitmap = bitmap;
                    }
                }

                if (foundBitmap is not null)
                {
                    return foundBitmap;
                }

                // Convert the GMImage to raw BGRA data and load it into an Avalonia Bitmap
                byte[] pixelData = image.ConvertToRawBgra().ToSpan().ToArray();
                using (var stream = new MemoryStream(pixelData))
                {
                    // Bitmap.DecodeToWidth/DecodeToHeight can be used for performance if needed
                    var avaloniaBitmap = new WriteableBitmap(
                                new Avalonia.PixelSize(image.Width, image.Height),
                                new Avalonia.Vector(96, 96), // DPI
                                PixelFormat.Bgra8888,
                                AlphaFormat.Unpremul);

                    using (var lockedBuffer = avaloniaBitmap.Lock())
                    {
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, lockedBuffer.Address, pixelData.Length);
                    }
                    Console.WriteLine();
                    _bitmapSourceLookup.Add((image, new WeakReference<Bitmap>(avaloniaBitmap)));
                    Console.WriteLine("Added image " + _bitmapSourceLookup.Count);
                    return avaloniaBitmap;
                }
            }
        }

        // You will need to change the type of your cache field
        private readonly List<(GMImage, WeakReference<Bitmap>)> _bitmapSourceLookup = new();
        private readonly object _bitmapSourceLookupLock = new();
    }
}
