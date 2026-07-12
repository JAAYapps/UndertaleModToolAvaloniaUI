using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Converters.ControlConverters;
using UndertaleModToolAvalonia.Models.EditorModels;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;
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
            Console.WriteLine("value " + value.GetType());
            bool isTile = false;
            bool cacheEnabled = true;
            bool generate = false;
            
            string? par;
            List<Tuple<uint, uint, uint, uint>>? tileRectList = null;
            if (parameter is string)
            {
                par = parameter as string;

                isTile = par?.Contains("tile") ?? false;
                cacheEnabled = !par?.Contains("nocache") ?? true;
                generate = par?.Contains("generate") ?? false;
            }
            else if (parameter is List<Tuple<uint, uint, uint, uint>>)
            {
                generate = true;
                tileRectList = parameter as List<Tuple<uint, uint, uint, uint>>;
            }
            
            Tile? tile = null;
            if (isTile)
                tile = value as Tile;

            UndertaleTexturePageItem? texture = isTile ? tile?.Tpag : value as UndertaleTexturePageItem;
            if (texture is null || texture.TexturePage is null)
                return null;

            string? texName = texture.Name?.Content;
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

            return spriteBitmap;
        }


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

    // UndertaleCachedImageLoader wrappers
    public class CachedTileImageLoader : IMultiValueConverter
    {
        private static UndertaleCachedImageLoader loader = new();
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is null) // tile
                return null;

            if ((uint)values[1] == 0 || (uint)values[2] == 0) // width, height
                return null;

            return loader.Convert(values[0], null, "tile", null);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class CachedImageLoaderWithIndex : IMultiValueConverter
    {
        private static UndertaleCachedImageLoader loader = new();
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(x => x is null))
                return null;

            IList<UndertaleSprite.TextureEntry> textures = values[0] as IList<UndertaleSprite.TextureEntry>;
            if (textures is null)
                return null;

            int index = -1;
            if (values[1] is int indexInt)
                index = indexInt;
            else if (values[1] is float indexFloat)
                index = (int)indexFloat;

            if (index > textures.Count - 1 || index < 0)
                return null;
            else
                return loader.Convert(textures[index].Texture, null, null, null);
        }
    }

    public class CachedTileDataLoader : IMultiValueConverter
    {
        // Tile text. page, tile ID - tile pixel data
        public static ConcurrentDictionary<Tuple<string, uint>, Bitmap> TileCache { get; set; } = new();
        private static readonly ConcurrentDictionary<string, Bitmap> tilePageCache = new();

        public static void Reset()
        {
            foreach (Bitmap bmp in TileCache.Values)
                bmp.Dispose();
            foreach (Bitmap bmp in tilePageCache.Values)
                bmp.Dispose();

            TileCache.Clear();
            tilePageCache.Clear();
            TileRectanglesConverter.TileCache.Clear();
        }

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(x => x is null))
                return null;

            bool cache = parameter is string par && par == "cache";

            Layer.LayerTilesData tilesData = values[0] as Layer.LayerTilesData;
            UndertaleBackground tilesBG = tilesData?.Background;

            if (tilesBG is null)
                return null;

            try
            {
                string texName = tilesBG.Texture?.Name?.Content;
                if (texName is null or "PageItem Unknown Index")
                {
                    texName = (AppConstants.Data!.TexturePageItems.IndexOf(tilesBG.Texture) + 1).ToString();
                    if (texName == "0")
                        return null;
                }

                Bitmap tilePageBMP;
                if (tilePageCache.ContainsKey(texName))
                {
                    tilePageBMP = tilePageCache[texName];
                }
                else
                {
                    tilePageBMP = UndertaleCachedImageLoader.CreateSpriteBitmap(new(tilesBG.Texture.SourceX,
                                                                                    tilesBG.Texture.SourceY,
                                                                                    tilesBG.Texture.SourceWidth,
                                                                                    tilesBG.Texture.SourceHeight), tilesBG.Texture);

                    tilePageCache[texName] = tilePageBMP;
                }
                
                WriteableBitmap data = BitmapInfo.BitmapToWritableBitmap(tilePageBMP);//tilePageBMP.LockBits(new Rectangle(0, 0, tilePageBMP.Width, tilePageBMP.Height), ImageLockMode.ReadOnly, tilePageBMP.PixelFormat);
                int depth = BitmapInfo.GetDepth(tilePageBMP); //Image.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[(int)(BitmapInfo.GetStride(tilePageBMP) * tilePageBMP.Size.Height)];
                Marshal.Copy(data.Lock().Address, buffer, 0, buffer.Length);
                //tilePageBMP.UnlockBits(data);

                int w = (int)tilesBG.GMS2TileWidth;
                int h = (int)tilesBG.GMS2TileHeight;
                int outX = (int)tilesBG.GMS2OutputBorderX;
                int outY = (int)tilesBG.GMS2OutputBorderY;
                int tileRows = (int)Math.Ceiling(tilesBG.GMS2TileCount / (double)tilesBG.GMS2TileColumns);
                PixelFormat? format = tilePageBMP.Format;

                bool outOfBounds = false;
                _ = Parallel.For(0, tileRows, (y) =>
                {
                    int y1 = ((y + 1) * outY) + (y * (h + outY));

                    for (int x = 0; x < tilesBG.GMS2TileColumns; x++)
                    {
                        int x1 = ((x + 1) * outX) + (x * (w + outX));

                        if (x1 + w > data.Size.Width || y1 + h > data.Size.Height)
                        {
                            outOfBounds = true;
                            return;
                        }

                        int bufferResLen = w * h * depth;
                        byte[] bufferRes = ArrayPool<byte>.Shared.Rent(bufferResLen);

                        if (!(x == 0 && y == 0))
                        {
                            for (int i = 0; i < h; i++)
                            {
                                for (int j = 0; j < w * depth; j += depth)
                                {
                                    int origIndex = (y1 * BitmapInfo.GetStride(data)) + (i * BitmapInfo.GetStride(data)) + (x1 * depth) + j;
                                    int croppedIndex = (i * w * depth) + j;

                                    Buffer.BlockCopy(buffer, origIndex, bufferRes, croppedIndex, depth);
                                }
                            }
                        }

                        WriteableBitmap tileBMP = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), format);
                        Marshal.Copy(bufferRes, 0, tileBMP.Lock().Address, bufferResLen);
                        ArrayPool<byte>.Shared.Return(bufferRes);

                        TileCache.TryAdd(new(texName, (uint)((tilesBG.GMS2TileColumns * y) + x)), tileBMP);
                    }
                });

                if (outOfBounds)
                {
                    _ = App.Current!.ShowError($"Tileset of \"{tilesData.ParentLayer.LayerName.Content}\" tile layer has wrong parameters (tile size, output border, etc.).\n" +
                                          "It can't be displayed.");
                    return "Error";
                }

                return cache ? null : CreateLayerSource(in tilesData, in tilesBG, in w, in h);
            }
            catch (Exception ex)
            {
                _ = App.Current!.ShowError($"An error occurred while rendering tile layer \"{tilesData.ParentLayer.LayerName.Content}\".\n\n{ex}");
                return "Error";
            }
        }

        public Bitmap CreateLayerSource(in Layer.LayerTilesData tilesData, in UndertaleBackground tilesBG, in int w, in int h)
        {
            int finalWidth = w * (int)tilesData.TilesX;
            int finalHeight = h * (int)tilesData.TilesY;

            // 1. Create a SkiaSharp canvas to draw on. This replaces `new Bitmap` and `Graphics.FromImage`.
            var info = new SKImageInfo(finalWidth, finalHeight);
            using var surface = SKSurface.Create(info);
            using var canvas = surface.Canvas;

            // This logic remains the same.
            uint maxID = tilesData.Background.GMS2TileIds.Select(x => x.ID).Max();

            for (int y = 0; y < tilesData.TilesY; y++)
            {
                for (int x = 0; x < tilesData.TilesX; x++)
                {
                    uint id = tilesData.TileData[y][x];
                    if (id == 0)
                        continue;

                    Bitmap? tileBitmap; // This will be our Avalonia Bitmap tile.

                    if (id > maxID)
                    {
                        uint realID = id & 0x0FFFFFFF;
                        if (realID > maxID)
                        {
                            Debug.WriteLine($"Tileset \"{tilesData.Background.Name.Content}\" doesn't contain tile ID {realID}");
                            continue;
                        }

                        // IMPORTANT: Assume TileCache now stores Avalonia Bitmaps.
                        // You cannot clone an Avalonia Bitmap, but the RotateFlip method creates a new instance anyway.
                        var originalTile = TileCache[new(tilesBG.Texture.Name.Content, realID)];

                        // 2. Use the new RotateFlip extension method. It returns a NEW bitmap.
                        RotateFlipType flipType = (id >> 28) switch
                        {
                            1 => RotateFlipType.RotateNoneFlipX,
                            2 => RotateFlipType.RotateNoneFlipY,
                            3 => RotateFlipType.RotateNoneFlipXY,
                            4 => RotateFlipType.Rotate90FlipNone,
                            5 => RotateFlipType.Rotate90FlipY,
                            6 => RotateFlipType.Rotate90FlipX,
                            7 => RotateFlipType.Rotate90FlipXY,
                            _ => RotateFlipType.RotateNoneFlipNone
                        };

                        if (flipType != RotateFlipType.RotateNoneFlipNone)
                        {
                            tileBitmap = originalTile.RotateFlip(flipType);
                        }
                        else
                        {
                            Debug.WriteLine($"Tile of {tilesData.ParentLayer.LayerName} at ({x}, {y}) has unknown flag.");
                            tileBitmap = originalTile; // Use the original if the flag is unknown
                        }
                    }
                    else
                    {
                        // Assume TileCache now stores Avalonia Bitmaps.
                        tileBitmap = TileCache[new(tilesBG.Texture.Name.Content, id)];
                    }

                    // 3. Draw the tile onto the SkiaSharp canvas.
                    if (tileBitmap != null)
                    {
                        // Convert the Avalonia tile Bitmap to an SKBitmap to draw it.
                        using var ms = new MemoryStream();
                        tileBitmap.Save(ms);
                        ms.Position = 0;
                        using var skTile = SKBitmap.Decode(ms);

                        // Draw the tile at the correct position. This replaces `g.DrawImageUnscaled`.
                        canvas.DrawBitmap(skTile, x * w, y * h);
                    }
                }
            }

            // 4. Convert the final SkiaSharp surface back to an Avalonia Bitmap.
            // This replaces all the HBitmap/Interop code.
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();

            return new Bitmap(stream);
        }

        //public Bitmap CreateLayerSource(in Layer.LayerTilesData tilesData, in UndertaleBackground tilesBG, in int w, in int h)
        //{
        //    WriteableBitmap layerBMP = new WriteableBitmap(new PixelSize(w * (int)tilesData.TilesX, h * (int)tilesData.TilesY), new Vector(96, 96));
        //    uint maxID = tilesData.Background.GMS2TileIds.Select(x => x.ID).Max();

        //    //using Graphics g = Graphics.FromImage(layerBMP);
        //    for (int y = 0; y < tilesData.TilesY; y++)
        //    {
        //        for (int x = 0; x < tilesData.TilesX; x++)
        //        {
        //            uint id = tilesData.TileData[y][x];
        //            if (id == 0)
        //                continue;

        //            if (id > maxID)
        //            {
        //                uint realID = id & 0x0FFFFFFF; // remove tile flag
        //                if (realID > maxID)
        //                {
        //                    Debug.WriteLine("Tileset \"" + tilesData.Background.Name.Content + "\" doesn't contain tile ID " + realID);
        //                    continue;
        //                }

        //                Bitmap resBMP = (Bitmap)TileCache[new(tilesBG.Texture.Name.Content, realID)].Clone();

        //                switch (id >> 28)
        //                {
        //                    case 1:
        //                        resBMP.RotateFlip(RotateFlipType.RotateNoneFlipX);
        //                        break;
        //                    case 2:
        //                        resBMP.RotateFlip(RotateFlipType.RotateNoneFlipY);
        //                        break;
        //                    case 3:
        //                        resBMP.RotateFlip(RotateFlipType.RotateNoneFlipXY);
        //                        break;
        //                    case 4:
        //                        resBMP.RotateFlip(RotateFlipType.Rotate90FlipNone);
        //                        break;
        //                    case 5:
        //                        // axes flipped since flip/mirror is done before rotation
        //                        resBMP.RotateFlip(RotateFlipType.Rotate90FlipY);
        //                        break;
        //                    case 6:
        //                        resBMP.RotateFlip(RotateFlipType.Rotate90FlipX);
        //                        break;
        //                    case 7:
        //                        resBMP.RotateFlip(RotateFlipType.Rotate90FlipXY);
        //                        break;

        //                    default:
        //                        Debug.WriteLine("Tile of " + tilesData.ParentLayer.LayerName + " located at (" + x + ", " + y + ") has unknown flag.");
        //                        break;
        //                }

        //                g.DrawImageUnscaled(resBMP, x * w, y * h);

        //                resBMP.Dispose();
        //            }
        //            else
        //                g.DrawImageUnscaled(TileCache[new(tilesBG.Texture.Name.Content, id)], x * w, y * h);
        //        }
        //    }

        //    Bitmap spriteSrc = new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Unpremul, layerBMP.Lock().Address, new PixelSize((int)layerBMP.Size.Width, (int)layerBMP.Size.Height), new Vector(96, 96), BitmapInfo.GetStride(layerBMP));
        //    layerBMP.Dispose();

        //    return spriteSrc;
        //}
    }
}
