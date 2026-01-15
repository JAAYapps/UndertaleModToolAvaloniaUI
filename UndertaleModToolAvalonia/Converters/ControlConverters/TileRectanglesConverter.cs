using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Models.EditorModels;
using UndertaleModToolAvalonia.Utilities;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class TileRectanglesConverter : IMultiValueConverter
    {
        public static ConcurrentDictionary<Tuple<string, uint>, Bitmap> TileCache { get; set; } = new();
        private static CachedTileDataLoader loader = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is Layer.LayerTilesData tilesData)
            {
                UndertaleBackground tilesBG = tilesData.Background;

                if (tilesBG is null)
                    return null;

                if ((loader.Convert(new object[] { tilesData }, null, "cache", null) as string) == "Error")
                    return null;

                try
                {
                    HashSet<uint> usedIDs = new();
                    List<Tuple<uint, uint, uint>> tileList = new();
                    for (uint y = 0; y < tilesData.TilesY; y++)
                        for (uint x = 0; x < tilesData.TilesX; x++)
                        {
                            uint tileID = tilesData.TileData[y][x];
                            if (tileID != 0)
                                tileList.Add(new(tileID, x, y));

                            usedIDs.Add(tileID & 0x0FFFFFFF); // removed tile flag
                        }

                    // convert Bitmaps to ImageSources (only used IDs)
                    _ = Parallel.ForEach(usedIDs, (id) =>
                    {
                        Tuple<string, uint> tileKey = new(tilesBG.Texture.Name.Content, id);

                        Bitmap spriteSrc = CachedTileDataLoader.TileCache[tileKey];

                        TileCache.TryAdd(tileKey, spriteSrc);
                    });

                    var tileArr = new TileRectangle[tileList.Count];
                    uint w = tilesBG.GMS2TileWidth;
                    uint h = tilesBG.GMS2TileHeight;
                    uint maxID = tilesData.Background.GMS2TileIds.Select(x => x.ID).Max();
                    _ = Parallel.For(0, tileList.Count, (i) =>
                    {
                        var tile = tileList[i];
                        uint id = tile.Item1;
                        uint realID;
                        double scaleX = 1;
                        double scaleY = 1;
                        double angle = 0;

                        if (id > maxID)
                        {
                            realID = id & 0x0FFFFFFF; // remove tile flag
                            if (realID > maxID)
                            {
                                Debug.WriteLine("Tileset \"" + tilesData.Background.Name.Content + "\" doesn't contain tile ID " + realID);
                                return;
                            }

                            switch (id >> 28)
                            {
                                case 1:
                                    scaleX = -1;
                                    break;
                                case 2:
                                    scaleY = -1;
                                    break;
                                case 3:
                                    scaleX = scaleY = -1;
                                    break;
                                case 4:
                                    angle = 90;
                                    break;
                                case 5:
                                    angle = 270;
                                    scaleY = -1;
                                    break;
                                case 6:
                                    angle = 90;
                                    scaleY = -1;
                                    break;
                                case 7:
                                    angle = 270;
                                    break;

                                default:
                                    Debug.WriteLine("Tile of " + tilesData.ParentLayer.LayerName + " located at (" + tile.Item2 + ", " + tile.Item3 + ") has unknown flag.");
                                    break;
                            }
                        }
                        else
                            realID = id;

                        tileArr[i] = new(TileCache[new(tilesBG.Texture.Name.Content, realID)], tile.Item2 * w, tile.Item3 * h, w, h, scaleX, scaleY, angle);
                    });

                    return tileArr;
                }
                catch (Exception ex)
                {
                    _ = App.Current!.ShowError($"An error occurred while generating \"Rectangles\" for tile layer {tilesData.ParentLayer.LayerName}.\n\n{ex}");
                    return null;
                }
            }
            else
                return null;
        }
    }

    public class ParentGridHeightConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double h)
                return h - 22; // "TabController" has predefined height
            else
                return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ParticleSystemRectConverter : IValueConverter
    {
        private static Dictionary<UndertaleParticleSystem, Rect> partSystemsDict;

        public static void Initialize(IEnumerable<UndertaleParticleSystem> particleSystems)
        {
            partSystemsDict = new();

            foreach (var partSys in particleSystems)
            {
                if (partSys is null)
                    continue;

                _ = AddNewSystem(partSys);
            }
        }
        public static void ClearDict() => partSystemsDict = null;

        private static Rect AddNewSystem(UndertaleParticleSystem partSys)
        {
            Rect rect = new Rect();
            if (partSys.Emitters.Count == 0)
            {
                partSystemsDict[partSys] = rect;
                return rect;
            }

            rect = new();
            var emitters = partSys.Emitters.Select(x => x.Resource);

            float minX = emitters.Select(x => x.RegionX).Min();
            float maxX = emitters.Select(x => x.RegionX + x.RegionWidth).Max();
            rect = rect.WithWidth(Math.Abs(minX - maxX));

            float minY = emitters.Select(x => x.RegionY).Min();
            float maxY = emitters.Select(x => x.RegionY + x.RegionHeight).Max();
            rect = rect.WithHeight(Math.Abs(minY - maxY));

            rect = rect.WithX(emitters.Select(x => x.RegionX - x.RegionWidth * 0.5f).Min());
            rect = rect.WithY(emitters.Select(x => x.RegionY - x.RegionHeight * 0.5f).Min());

            partSystemsDict[partSys] = rect;

            return rect;
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not UndertaleParticleSystem partSys)
                return 0;
            if ((partSys.Emitters?.Count ?? 0) == 0)
                return 0;
            if (parameter is not string mode)
                return 0;

            Rect sysRect = new Rect();
            if (partSystemsDict is not null && !partSystemsDict.TryGetValue(partSys, out Rect sRect))
                sysRect = sRect = AddNewSystem(partSys);

            return mode switch
            {
                "width" => sysRect.Width,
                "height" => sysRect.Height,
                "x" => sysRect.X + 8,
                "y" => sysRect.Y + 8,
                _ => 0
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
