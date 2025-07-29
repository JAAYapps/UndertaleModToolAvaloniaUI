using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Converters
{
    internal class UndertaleCachedImageLoader : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, Bitmap> imageCache = new();
        private static readonly ConcurrentDictionary<Tuple<string, Tuple<uint, uint, uint, uint>>, Bitmap> tileCache = new();
        // private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        private static bool _reuseTileBuffer;
        public static bool ReuseTileBuffer
        {
            get => _reuseTileBuffer;
            set
            {
                sharedTileBuffer = value ? ArrayPool<byte>.Create() : null;

                _reuseTileBuffer = value;
            }
        }
        private static ArrayPool<byte> sharedTileBuffer;
        private static int currBufferSize = 1048576;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            bool isTile = false;
            bool cacheEnabled = true;
            bool generate = false;

            string par;
            List<Tuple<uint, uint, uint, uint>> tileRectList = null;
            if (parameter is string)
            {
                par = parameter as string;

                isTile = par.Contains("tile");
                cacheEnabled = !par.Contains("nocache");
                generate = par.Contains("generate");
            }
            else if (parameter is List<Tuple<uint, uint, uint, uint>>)
            {
                generate = true;
                tileRectList = parameter as List<Tuple<uint, uint, uint, uint>>;
            }

            Tile tile = null;
            if (isTile)
                tile = value as Tile;

            UndertaleTexturePageItem texture = isTile ? tile.Tpag : value as UndertaleTexturePageItem;
            if (texture is null || texture.TexturePage is null)
                return null;

            string texName = texture.Name?.Content;
            if (texName is null || texName == "PageItem Unknown Index")
            {
                if (generate)
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        return (AppConstants.Data!.TexturePageItems.IndexOf(texture) + 1).ToString();
                    });
                else
                    texName = (AppConstants.Data!.TexturePageItems.IndexOf(texture) + 1).ToString();

                if (texName == "0")
                    return null;
            }

            if (texture.SourceWidth == 0 || texture.SourceHeight == 0)
                return null;

            if (tileRectList is not null)
            {
                Rect rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);
                ProcessTileSet(texName, CreateSpriteBitmap(rect, in texture), tileRectList, texture.TargetX, texture.TargetY);

                return null;
            }

            Bitmap spriteSrc;
            if (isTile)
            {
                if (tileCache.TryGetValue(new(texName, new((uint)tile.SourceX, (uint)tile.SourceY, tile.Width, tile.Height)), out spriteSrc))
                    return spriteSrc;
            }

            if (!imageCache.ContainsKey(texName) || !cacheEnabled)
            {
                Rect rect;

                // how many pixels are out of bounds of tile texture page
                int diffW = 0;
                int diffH = 0;

                if (isTile)
                {
                    int actualTileSourceX = tile.SourceX - texture.TargetX;
                    int actualTileSourceY = tile.SourceY - texture.TargetY;
                    diffW = (int)(actualTileSourceX + tile.Width - texture.SourceWidth);
                    diffH = (int)(actualTileSourceY + tile.Height - texture.SourceHeight);
                    rect = new((int)(texture.SourceX + actualTileSourceX), (int)(texture.SourceY + actualTileSourceY), (int)tile.Width, (int)tile.Height);
                }
                else
                    rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);

                spriteSrc = CreateSpriteSource(in rect, in texture, diffW, diffH);

                if (cacheEnabled)
                {
                    if (isTile)
                        tileCache.TryAdd(new(texName, new((uint)tile.SourceX, (uint)tile.SourceY, tile.Width, tile.Height)), spriteSrc);
                    else
                        imageCache.TryAdd(texName, spriteSrc);
                }

                if (generate)
                    return null;
                else
                    return spriteSrc;
            }

            return imageCache[texName];
        }

        public static void Reset()
        {
            imageCache.Clear();
            tileCache.Clear();
            ReuseTileBuffer = false;
            currBufferSize = 1048576;
        }

        /// <summary>
        /// Copies a rectangular region from a source image to a destination RenderTargetBitmap.
        /// </summary>
        /// <param name="sourceImage">The image to copy from (equivalent to your bitmapSource).</param>
        /// <param name="targetBitmap">The bitmap to draw onto. Must be a RenderTargetBitmap.</param>
        /// <param name="sourceRect">The rectangular area to copy from the sourceImage.</param>
        /// <param name="targetOffset">The top-left position to draw at in the targetBitmap.</param>
        public static void CopyImageRegion(IImage sourceImage, RenderTargetBitmap targetBitmap, Rect sourceRect, Point targetOffset)
        {
            // A DrawingContext is used to render graphics onto a target.
            // The 'using' block ensures the context is properly disposed, and the drawing
            // operations are applied.
            using (var context = targetBitmap.CreateDrawingContext())
            {
                // Define the destination rectangle on the target bitmap.
                var destinationRect = new Rect(targetOffset.X, targetOffset.Y, sourceRect.Width, sourceRect.Height);

                // Draw the specified portion of the source image onto the target.
                context.DrawImage(sourceImage, sourceRect, destinationRect);
            }
        }

        public static Bitmap CreateSpriteBitmap(Rect rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        {
            GMImage image = texture.TexturePage.TextureData.Image;
            if (App.Current!.Services.GetService(typeof(ITextureCacheService)) is not ITextureCacheService textureCacheService)
            {
                _ = App.Current!.ShowError("The Texture Service failed to be created.");
                return new WriteableBitmap(new PixelSize(), new Vector(96.0f, 96.0f), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Unpremul);
            }

            Bitmap bitmapSource = textureCacheService.GetBitmapForImage(image);

            // Clamp width/height in bounds (diffW/diffH represent how out of bounds they are)
            rect = rect.WithWidth(rect.Width - ((diffW > 0) ? diffW : 0));
            rect = rect.WithHeight(rect.Height - ((diffH > 0) ? diffH : 0));

            // Clamp X/Y in bounds
            int offsetX = 0, offsetY = 0;
            if (rect.X < texture.SourceX)
            {
                offsetX = texture.SourceX - (int)rect.X;
                rect = rect.WithWidth(rect.Width - offsetX);
                rect = rect.WithX(texture.SourceX);
            }
            if (rect.Y < texture.SourceY)
            {
                offsetY = texture.SourceY - (int)rect.Y;
                rect = rect.WithHeight(rect.Height - offsetY);
                rect = rect.WithY(texture.SourceY);
            }

            RenderTargetBitmap spriteBitmap = new RenderTargetBitmap(new PixelSize((int)rect.Width, (int)rect.Height), new Vector(96.0f, 96.0f));
            
            //WriteableBitmap spriteBitmap = new WriteableBitmap(new PixelSize((int)rect.Width, (int)rect.Height), new Vector(96.0f, 96.0f));

            // Abort if rect is out of bounds of the texture item
            if (rect.X >= (texture.SourceX + texture.SourceWidth) || rect.Y >= (texture.SourceY + texture.SourceHeight))
                return spriteBitmap;
            if (rect.Width <= 0 || rect.Height <= 0)
                return spriteBitmap;

            // Abort if rect is out of bounds of bitmap source (e.g., texture was not loaded)
            if (rect.X < 0 || rect.X >= bitmapSource.Size.Width || rect.Y < 0 || rect.Y >= bitmapSource.Size.Height ||
                (rect.X + rect.Width) > bitmapSource.Size.Width || (rect.Y + rect.Height) > bitmapSource.Size.Height)
            {
                return spriteBitmap;
            }

            CopyImageRegion(bitmapSource, spriteBitmap, rect, new Point(offsetX, offsetY));

            //int depth = BitmapInfo.GetDepth(img);
            //int bufferLen = BitmapInfo.GetStride(img) * (int)img.Size.Height;
            //int stride = BitmapInfo.GetStride(img);
            //int bufferResLen = (int)temp.Width * (int)temp.Height * depth;
            //byte[] bufferRes = new byte[bufferLen];

            //WriteableBitmap spriteBMP = new WriteableBitmap(new PixelSize((int)temp.Width, (int)temp.Height), new Vector(96.0f, 96.0f), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Unpremul);


            //using var dataNew = spriteBMP.Lock();
            //Marshal.Copy(bufferRes, 0, dataNew.Address, bufferResLen);
            //ArrayPool<byte>.Shared.Return(bufferRes);

            //nint bmpPtr = spriteBMP.Lock().Address;
            //Bitmap spriteSrc = new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Unpremul, bmpPtr, new PixelSize(), new Vector(96.0, 96.0), stride);
            //spriteBMP.Dispose();

            //using Bitmap img = new Bitmap("D:\\gitsorts\\AAY resources\\AAYBicon.png");

            //Bitmap g = new Bitmap(img, new PixelRect(x, y, (int)100, (int)temp.Height));

            return spriteBitmap;
        }

        /// <summary>
        /// Creates a sprite bitmap by copying a rectangular region from a source texture page.
        /// </summary>
        /// <param name="rect">The desired region to extract.</param>
        /// <param name="texture">The texture page item containing the source texture.</param>
        /// <param name="diffW">Value to subtract from the width, for out-of-bounds adjustment.</param>
        /// <param name="diffH">Value to subtract from the height, for out-of-bounds adjustment.</param>
        /// <returns>An Avalonia Bitmap containing the sprite.</returns>
        //public static Bitmap CreateSpriteBitmap(PixelRect rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        //{
        //    // This helper method must be implemented to convert your custom GMImage
        //    // into an Avalonia WriteableBitmap. See implementation example below.
        //    WriteableBitmap sourceBitmap = GetWriteableBitmapForImage(texture.TexturePage.TextureData.Image);

        //    // Create the destination bitmap with the original requested dimensions.
        //    // Use Math.Max to prevent zero-sized bitmaps which can cause exceptions.
        //    var spriteBitmap = new WriteableBitmap(
        //        new PixelSize(Math.Max(1, rect.Width), Math.Max(1, rect.Height)),
        //        new Vector(96, 96), // Standard DPI
        //        PixelFormat.Bgra8888, // Assumes 32bpp RGBA format
        //        AlphaFormat.Premul);

        //    // If source texture failed to load, return the blank spriteBitmap.
        //    if (sourceBitmap == null)
        //        return spriteBitmap;

        //    // --- Start of bounds checking and clamping logic ---
        //    var clampedRect = rect;

        //    // Clamp width/height based on diffW/diffH
        //    clampedRect = clampedRect.WithWidth(clampedRect.Width - (diffW > 0 ? diffW : 0));
        //    clampedRect = clampedRect.WithHeight(clampedRect.Height - (diffH > 0 ? diffH : 0));

        //    // Clamp X/Y, calculating the offset for drawing into the destination bitmap
        //    int offsetX = 0, offsetY = 0;
        //    if (clampedRect.X < texture.SourceX)
        //    {
        //        offsetX = texture.SourceX - clampedRect.X;
        //        clampedRect = clampedRect.WithWidth(clampedRect.Width - offsetX)
        //                                 .WithX(texture.SourceX);
        //    }
        //    if (clampedRect.Y < texture.SourceY)
        //    {
        //        offsetY = texture.SourceY - clampedRect.Y;
        //        clampedRect = clampedRect.WithHeight(clampedRect.Height - offsetY)
        //                                 .WithY(texture.SourceY);
        //    }

        //    // Abort if the clamped rectangle is invalid or outside the texture item's defined area
        //    if (clampedRect.Width <= 0 || clampedRect.Height <= 0 ||
        //        clampedRect.X >= (texture.SourceX + texture.SourceWidth) ||
        //        clampedRect.Y >= (texture.SourceY + texture.SourceHeight))
        //    {
        //        return spriteBitmap;
        //    }

        //    // Abort if the clamped rectangle is outside the source bitmap's bounds
        //    var sourceBounds = new PixelRect(0, 0, sourceBitmap.PixelSize.Width, sourceBitmap.PixelSize.Height);
        //    if (!sourceBounds.Contains(clampedRect.TopLeft) || !sourceBounds.Contains(clampedRect.BottomRight - new PixelPoint(1, 1)))
        //    {
        //        return spriteBitmap;
        //    }
        //    // --- End of bounds checking ---

        //    // Copy pixel data from the source to the destination bitmap.
        //    // This requires enabling "unsafe" code in your project's .csproj file.
        //    unsafe
        //    {
        //        using (var sourceLock = sourceBitmap.Lock())
        //        using (var destLock = spriteBitmap.Lock())
        //        {
        //            var bpp = sourceLock.Format.Value.BitsPerPixel / 8; // Bytes per pixel

        //            for (int y = 0; y < clampedRect.Height; y++)
        //            {
        //                // Pointer to the beginning of the source row
        //                byte* pSrc = (byte*)sourceLock.Address +
        //                             ((clampedRect.Y + y) * sourceLock.RowBytes) +
        //                             (clampedRect.X * bpp);

        //                // Pointer to the beginning of the destination row
        //                byte* pDest = (byte*)destLock.Address +
        //                              ((offsetY + y) * destLock.RowBytes) +
        //                              (offsetX * bpp);

        //                // Copy the row data
        //                Buffer.MemoryCopy(pSrc, pDest, destLock.RowBytes, clampedRect.Width * bpp);
        //            }
        //        }
        //    }

        //    return spriteBitmap;
        //}

        // Add these using statements at the top of your file

        // NOTE: You must enable unsafe code in your .csproj file for this to compile:
        // <PropertyGroup>
        //   <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        // </PropertyGroup>
        //public static unsafe Bitmap CreateSpriteBitmap(PixelRect rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        //{
        //    // Create the destination bitmap with the initial, un-clamped size.
        //    // This will be the canvas we draw the sprite onto.
        //    var spriteBitmap = new WriteableBitmap(
        //        new PixelSize(rect.Width, rect.Height),
        //        new Vector(96, 96), // Standard DPI
        //        PixelFormat.Bgra8888, // Common pixel format for Avalonia
        //        AlphaFormat.Premul);

        //    // --- Clamping and Bounds Checking Logic (largely the same) ---

        //    // Clamp width/height in bounds
        //    rect = rect.WithWidth(rect.Width - (diffW > 0 ? diffW : 0));
        //    rect = rect.WithHeight(rect.Height - (diffH > 0 ? diffH : 0));

        //    // Clamp X/Y in bounds and calculate the offset for drawing
        //    int offsetX = 0, offsetY = 0;
        //    if (rect.X < texture.SourceX)
        //    {
        //        offsetX = texture.SourceX - rect.X;
        //        rect = rect.WithWidth(rect.Width - offsetX);
        //        rect = rect.WithX(texture.SourceX);
        //    }
        //    if (rect.Y < texture.SourceY)
        //    {
        //        offsetY = texture.SourceY - rect.Y;
        //        rect = rect.WithHeight(rect.Height - offsetY);
        //        rect = rect.WithY(texture.SourceY);
        //    }

        //    // Abort if rect is out of bounds of the texture item or has no size
        //    if (rect.X >= (texture.SourceX + texture.SourceWidth) || rect.Y >= (texture.SourceY + texture.SourceHeight) || rect.Width <= 0 || rect.Height <= 0)
        //        return spriteBitmap;

        //    // --- Pixel Copying Logic (Avalonia-specific) ---

        //    // Get the raw pixel data and dimensions from the source texture page
        //    GMImage sourceImage = texture.TexturePage.TextureData.Image;
        //    byte[] sourceData = sourceImage.ConvertToRawBgra().ToSpan().ToArray();
        //    int sourceWidth = sourceImage.Width;
        //    int sourceHeight = sourceImage.Height;
        //    int bytesPerPixel = 4; // BGRA8888

        //    // Final safety check against the full source texture dimensions
        //    if (rect.Right > sourceWidth || rect.Bottom > sourceHeight)
        //    {
        //        // This can happen if the texture page wasn't loaded correctly.
        //        return spriteBitmap;
        //    }

        //    // Lock the destination bitmap's buffer to write pixel data
        //    using (var frameBuffer = spriteBitmap.Lock())
        //    {
        //        byte* destPtr = (byte*)frameBuffer.Address;
        //        int destStride = frameBuffer.RowBytes;

        //        fixed (byte* sourcePtrStart = sourceData)
        //        {
        //            // Perform a row-by-row copy from the source byte array to the destination framebuffer
        //            for (int y = 0; y < rect.Height; y++)
        //            {
        //                // Calculate the starting position in the source texture data
        //                byte* sourceRowPtr = sourcePtrStart + ((rect.Y + y) * sourceWidth + rect.X) * bytesPerPixel;

        //                // Calculate the starting position in the destination bitmap data
        //                byte* destRowPtr = destPtr + ((offsetY + y) * destStride) + (offsetX * bytesPerPixel);

        //                // Copy the pixel data for the entire row
        //                Buffer.MemoryCopy(sourceRowPtr, destRowPtr, rect.Width * bytesPerPixel, rect.Width * bytesPerPixel);
        //            }
        //        }
        //    }

        //    return spriteBitmap;
        //}

        /// <summary>
        /// Returns an Avalonia Bitmap instance for the given GMImage.
        /// This is the Avalonia equivalent of the GetBitmapSourceForImage method.
        /// </summary>
        //public Bitmap GetBitmapForImage(GMImage image)
        //{
        //    lock (_bitmapSourceLookupLock)
        //    {
        //        // This caching logic remains the same, just the type changes
        //        Bitmap foundBitmap = null;
        //        for (int i = _bitmapSourceLookup.Count - 1; i >= 0; i--)
        //        {
        //            (GMImage imageKey, WeakReference<Bitmap> referenceVal) = _bitmapSourceLookup[i];
        //            if (!referenceVal.TryGetTarget(out Bitmap bitmap))
        //            {
        //                _bitmapSourceLookup.RemoveAt(i);
        //            }
        //            else if (imageKey == image)
        //            {
        //                foundBitmap = bitmap;
        //            }
        //        }

        //        if (foundBitmap is not null)
        //        {
        //            return foundBitmap;
        //        }

        //        // Convert the GMImage to raw BGRA data and load it into an Avalonia Bitmap
        //        byte[] pixelData = image.ConvertToRawBgra().ToSpan().ToArray();
        //        using (var stream = new MemoryStream(pixelData))
        //        {
        //            // Bitmap.DecodeToWidth/DecodeToHeight can be used for performance if needed
        //            var avaloniaBitmap = new Bitmap(stream);
        //            _bitmapSourceLookup.Add((image, new WeakReference<Bitmap>(avaloniaBitmap)));
        //            return avaloniaBitmap;
        //        }
        //    }
        //}

        // You will need to change the type of your cache field
        //private readonly List<(GMImage, WeakReference<Bitmap>)> _bitmapSourceLookup = new();
        //private readonly object _bitmapSourceLookupLock = new();


        // Left over function from old tool.
        private Bitmap CreateSpriteSource(in Rect rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        {
            Bitmap spriteBMP = CreateSpriteBitmap(rect, in texture, diffW, diffH);

            Bitmap spriteSrc = spriteBMP;

            return spriteSrc;
        }

        private void ProcessTileSet(string textureName, Bitmap bmp, List<Tuple<uint, uint, uint, uint>> tileRectList, int targetX, int targetY)
        {
            var data = BitmapInfo.BitmapToWritableBitmap(bmp);

            int depth = BitmapInfo.GetDepth(bmp);

            int bufferLen = BitmapInfo.GetStride(bmp) * (int)bmp.Size.Height;
            byte[] buffer;
            if (ReuseTileBuffer)
            {
                if (bufferLen > currBufferSize)
                {
                    currBufferSize = bufferLen;
                    sharedTileBuffer = ArrayPool<byte>.Create(currBufferSize, 17); // 17 is default value
                }

                buffer = sharedTileBuffer.Rent(bufferLen);
            }
            else
                buffer = new byte[bufferLen];

            Marshal.Copy(data.Lock().Address, buffer, 0, bufferLen);

            _ = Parallel.ForEach(tileRectList, (tileRect) =>
            {
                int origX = (int)tileRect.Item1;
                int origY = (int)tileRect.Item2;
                int x = origX - targetX;
                int y = origY - targetY;
                int w = (int)tileRect.Item3;
                int h = (int)tileRect.Item4;

                if (w == 0 || h == 0)
                    return;

                // Sometimes, tile size can be bigger than texture size
                // (for example, BG tile of "room_torielroom")
                // Also, it can be out of texture bounds
                // (for example, tile 10055649 of "room_fire_core_topright")
                // (both examples are from Undertale)
                // This algorithm doesn't support that, so this tile will be processed by "CreateSpriteSource()"
                if (w > data.Size.Width || h > data.Size.Height || x < 0 || y < 0 || x + w > data.Size.Width || y + h > data.Size.Height)
                    return;

                int stride = BitmapInfo.GetStride(data);
                int bufferResLen = w * h * depth;
                byte[] bufferRes = ArrayPool<byte>.Shared.Rent(bufferResLen); // may return bigger array than requested

                // Source - https://stackoverflow.com/a/9691388/12136394
                // There was faster solution, but it uses "unsafe" code
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w * depth; j += depth)
                    {
                        int origIndex = y * stride + i * stride + x * depth + j;
                        int croppedIndex = i * w * depth + j;

                        Buffer.BlockCopy(buffer, origIndex, bufferRes, croppedIndex, depth);
                    }
                }

                //Bitmap tileBMP = new(w, h);
                //BitmapData dataNew = tileBMP.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, data.PixelFormat);
                var tileBMP = new WriteableBitmap(new PixelSize(w, h), new Vector(96.0f, 96.0f), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
                using var dataNew = tileBMP.Lock();
                Marshal.Copy(bufferRes, 0, dataNew.Address, bufferResLen);
                ArrayPool<byte>.Shared.Return(bufferRes);

                nint bmpPtr = tileBMP.Lock().Address;
                Bitmap spriteSrc = new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Unpremul, bmpPtr, PixelSize.Empty, new Vector(96.0, 96.0), stride);
                tileBMP.Dispose();

                Tuple<string, Tuple<uint, uint, uint, uint>> tileKey = new(textureName, new((uint)origX, (uint)origY, (uint)w, (uint)h));
                tileCache.TryAdd(tileKey, spriteSrc);
            });

            bmp.Dispose();

            if (ReuseTileBuffer)
                sharedTileBuffer.Return(buffer);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
