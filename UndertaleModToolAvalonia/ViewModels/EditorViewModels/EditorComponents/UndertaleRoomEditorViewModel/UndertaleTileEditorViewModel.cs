using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;
using UndertaleModToolAvalonia.Converters;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Models.EditorModels;
using UndertaleModToolAvalonia.Utilities;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel
{
    public partial class TileEditorSettings : ObservableObject
    {
        public static TileEditorSettings instance { get; set; } = new();

        [ObservableProperty]
        public bool brushTiling = true;

        [ObservableProperty]
        public bool roomPreviewBool = true;

        [ObservableProperty]
        public bool showGridBool = true;
    }

    public partial class UndertaleTileEditorViewModel : ViewModelBase, IInitializable<Layer>
    {
        public TileEditorSettings settings { get; set; } = TileEditorSettings.instance;

        public bool Modified { get; set; } = false;

        public Layer EditingLayer { get; set; }

        public WriteableBitmap TilesBitmap { get; set; }

        public double EditWidth { get; set; }
        public double EditHeight { get; set; }
        public double PaletteWidth { get; set; }
        public double PaletteHeight { get; set; }

        public List<Dictionary<Tuple<int, int>, uint>> UndoStack { get; set; } = new();
        public List<Dictionary<Tuple<int, int>, uint>> RedoStack { get; set; } = new();
        public bool UndoEnabled { get; set; } = false;
        public bool RedoEnabled { get; set; } = false;

        private const uint TILE_FLIP_H = 0b00010000000000000000000000000000;
        private const uint TILE_FLIP_V = 0b00100000000000000000000000000000;
        private const uint TILE_ROTATE = 0b01000000000000000000000000000000;
        private const uint TILE_INDEX = 0x7ffff;
        private const uint TILE_FLAGS = ~TILE_INDEX;
        // flags shifted 28 bits to the right
        private static Dictionary<uint, uint> ROTATION_CW = new Dictionary<uint, uint>{
            {0b000, 0b100},
            {0b100, 0b011},
            {0b011, 0b111},
            {0b111, 0b000},

            {0b110, 0b001},
            {0b010, 0b110},
            {0b101, 0b010},
            {0b001, 0b101},
        };
        private static Dictionary<uint, uint> ROTATION_CCW = new Dictionary<uint, uint>{
            {0b100, 0b000},
            {0b011, 0b100},
            {0b111, 0b011},
            {0b000, 0b111},

            {0b001, 0b110},
            {0b110, 0b010},
            {0b010, 0b101},
            {0b101, 0b001},
        };

        private uint[][] OldTileData { get; set; }
        public Layer.LayerTilesData TilesData { get; set; }
        public Layer.LayerTilesData PaletteTilesData { get; set; }
        public uint PaletteColumns
        {
            get { return PaletteTilesData.TilesX; }
            set
            {
                PaletteTilesData.TilesX = Math.Max(value, 1);
                SetPaletteColumns(PaletteTilesData.Background, PaletteTilesData.TilesX);
                PopulatePalette();
            }
        }
        public double PaletteCursorX { get; set; }
        public double PaletteCursorY { get; set; }
        public double PaletteCursorWidth { get; set; }
        public double PaletteCursorHeight { get; set; }
        public bool PaletteCursorVisibility { get; set; }

        public Layer.LayerTilesData BrushTilesData { get; set; }
        private bool BrushEmpty { get; set; } = true;
        public double BrushWidth { get; set; }
        public double BrushHeight { get; set; }
        public double BrushPreviewX { get; set; } = 0;
        public double BrushPreviewY { get; set; } = 0;
        public bool BrushPreviewVisibility { get; set; }
        public bool BrushOutlineVisibility { get; set; }
        public bool BrushPickVisibility { get; set; }
        public long RefreshBrush { get; set; } = 0;


        public RenderTargetBitmap RoomPreview { get; set; }
        public float RoomPrevOffsetX { get; set; }
        public float RoomPrevOffsetY { get; set; }
        public Point ScrollViewStart { get; set; }
        public Point DrawingStart { get; set; }
        public Point LastMousePos { get; set; }

        private bool apply { get; set; } = false;

        [ObservableProperty]
        private Layer.LayerTilesData focusedTilesData;

        public enum PaintAction
        {
            None,
            Draw,
            Erase,
            Pick,
            DragPick,
            Drag,

        }

        [ObservableProperty]
        private PaintAction painting = PaintAction.None;

        private static CachedTileDataLoader loader = new();
        private byte[] emptyTile { get; set; }
        private Dictionary<uint, byte[]> TileCache { get; set; }

        public Rect GridRect { get; set; }
        public Point GridPoint1 { get; set; }
        public Point GridPoint2 { get; set; }

        public string StatusText { get; set; } = "";

        private static List<(WeakReference<UndertaleBackground>, uint)> PaletteColumnsMap { get; set; } = new();
        public static uint GetPaletteColumns(UndertaleBackground background)
        {
            // Look through entire list, clearing out old weak references
            uint paletteColumns = background.GMS2TileColumns;
            for (int i = PaletteColumnsMap.Count - 1; i >= 0; i--)
            {
                (WeakReference<UndertaleBackground> reference, uint thisColumns) = PaletteColumnsMap[i];
                if (reference.TryGetTarget(out UndertaleBackground thisBg))
                {
                    if (thisBg == background)
                    {
                        paletteColumns = thisColumns;
                    }
                }
                else
                {
                    // Clear out old weak reference
                    PaletteColumnsMap.RemoveAt(i);
                }
            }
            return paletteColumns;
        }
        public static void SetPaletteColumns(UndertaleBackground background, uint value)
        {
            // Look through entire list, clearing out old weak references, and possibly set the palette columns value of this background
            bool added = false;
            for (int i = PaletteColumnsMap.Count - 1; i >= 0; i--)
            {
                (WeakReference<UndertaleBackground> reference, uint _) = PaletteColumnsMap[i];
                if (reference.TryGetTarget(out UndertaleBackground thisBg))
                {
                    if (thisBg == background)
                    {
                        // Set the palette columns
                        PaletteColumnsMap[i] = (reference, value);
                        added = true;
                    }
                }
                else
                {
                    // Clear out old weak reference
                    PaletteColumnsMap.RemoveAt(i);
                }
            }
            if (added) return;
            // Add new entry
            PaletteColumnsMap.Add((new WeakReference<UndertaleBackground>(background), value));
        }

        [ObservableProperty]
        private bool isMouseOver = false;

        public async Task<bool> InitializeAsync(Layer layer)
        {
            EditingLayer = layer;

            RoomPrevOffsetX = -EditingLayer.XOffset;
            RoomPrevOffsetY = -EditingLayer.YOffset;

            OldTileData = CloneTileData(EditingLayer.TilesData.TileData);
            TilesData = EditingLayer.TilesData;
            TileCache = new();

            BrushTilesData = new Layer.LayerTilesData();
            BrushTilesData.TileData = new uint[][] { new uint[] { 0 } };
            BrushTilesData.Background = TilesData.Background;
            BrushTilesData.TilesX = 1;
            BrushTilesData.TilesY = 1;
            UpdateBrush(false);

            PaletteTilesData = new Layer.LayerTilesData();
            PaletteTilesData.TileData = new uint[][] { new uint[] { 0 } };
            PaletteTilesData.Background = TilesData.Background;
            PaletteColumns = GetPaletteColumns(PaletteTilesData.Background);

            EditWidth = Convert.ToDouble((long)TilesData.TilesX * (long)TilesData.Background.GMS2TileWidth);
            EditHeight = Convert.ToDouble((long)TilesData.TilesY * (long)TilesData.Background.GMS2TileHeight);

            emptyTile = (byte[])Array.CreateInstance(
                typeof(byte), TilesData.Background.GMS2TileWidth * TilesData.Background.GMS2TileHeight * 4
            );
            Array.Fill<byte>(emptyTile, 0);

            GridRect = new(0, 0, TilesData.Background.GMS2TileWidth, TilesData.Background.GMS2TileHeight);
            GridPoint1 = new(TilesData.Background.GMS2TileWidth, 0);
            GridPoint2 = new(0, TilesData.Background.GMS2TileHeight);

            CachedTileDataLoader.Reset();
            TilesBitmap = new(new PixelSize((int)EditWidth, (int)EditHeight), new Vector(96, 96), PixelFormats.Bgra8888, null);
            DrawTilemap(TilesData, TilesBitmap);

            return true;
        }

        #region Brush and tile palette
        private void PopulatePalette()
        {
            PaletteTilesData.TilesY = (uint)Convert.ToInt32(
                Math.Ceiling(
                    (double)PaletteTilesData.Background.GMS2TileCount /
                    PaletteTilesData.TilesX
                )
            );

            PaletteWidth = Convert.ToDouble((long)PaletteTilesData.TilesX * (long)PaletteTilesData.Background.GMS2TileWidth);
            PaletteHeight = Convert.ToDouble((long)PaletteTilesData.TilesY * (long)PaletteTilesData.Background.GMS2TileHeight);

            int i = 0;
            int itemsPerTile = (int)PaletteTilesData.Background.GMS2ItemsPerTileCount;
            int count = (int)PaletteTilesData.Background.GMS2TileCount * itemsPerTile;
            for (int y = 0; y < PaletteTilesData.TilesY; y++)
            {
                for (int x = 0; x < PaletteTilesData.TilesX; x++)
                {
                    if (i >= count)
                        PaletteTilesData.TileData[y][x] = 0;
                    else
                        PaletteTilesData.TileData[y][x] = PaletteTilesData.Background.GMS2TileIds[i].ID;
                    i += itemsPerTile;
                }
            }

            FindPaletteCursor();
        }

        public void UpdateBrush(bool isImage)
        {
            if (Painting == PaintAction.DragPick)
            {
                BrushWidth = Convert.ToDouble(PaletteTilesData.Background.GMS2TileWidth);
                BrushHeight = Convert.ToDouble(PaletteTilesData.Background.GMS2TileHeight);
            }
            else
            {
                BrushWidth = Convert.ToDouble(
                    (long)BrushTilesData.TilesX * (long)BrushTilesData.Background.GMS2TileWidth
                );
                BrushHeight = Convert.ToDouble(
                    (long)BrushTilesData.TilesY * (long)BrushTilesData.Background.GMS2TileHeight
                );
            }
            BrushEmpty = true;
            for (int y = 0; y < BrushTilesData.TilesY; y++)
            {
                for (int x = 0; x < BrushTilesData.TilesX; x++)
                {
                    if ((BrushTilesData.TileData[y][x] & TILE_INDEX) != 0)
                    {
                        BrushEmpty = false;
                        break;
                    }
                }
                if (!BrushEmpty)
                    break;
            }
            UpdateBrushVisibility(isImage);
        }
        public void UpdateBrushVisibility(bool isImage)
        {
            bool over = IsMouseOver;
            BrushPreviewVisibility = (Painting == PaintAction.None && over);
            BrushOutlineVisibility =
                ((BrushEmpty && (Painting == PaintAction.None || Painting == PaintAction.Draw)) ||
                Painting == PaintAction.Erase) && over;
            BrushPickVisibility =
                ((Painting == PaintAction.Pick || (Painting == PaintAction.DragPick &&
                    PositionToTile(LastMousePos, FocusedTilesData, out _, out _)
                )) && isImage);
        }
        #endregion

        #region Tile painting and picking
        // Places the current brush onto a tilemap.
        // ox and oy specify the origin point of multi-tile brushes.
        public void PaintTile(int x, int y, int ox, int oy, Layer.LayerTilesData tilesData, bool erase = false)
        {
            int maxX = (int)Math.Min(x + BrushTilesData.TilesX, tilesData.TilesX);
            int maxY = (int)Math.Min(y + BrushTilesData.TilesY, tilesData.TilesY);
            for (int ty = (int)Math.Max(y, 0); ty < maxY; ty++)
            {
                for (int tx = (int)Math.Max(x, 0); tx < maxX; tx++)
                {
                    if (erase)
                        SetTile(tx, ty, tilesData, 0);
                    else
                        SetBrushTile(tilesData, tx, ty, ox, oy);
                }
            }
        }
        public void PaintLine(Layer.LayerTilesData tilesData, Point pos1, Point pos2, Point start, bool erase = false)
        {
            PositionToTile(pos1, tilesData, out int x1, out int y1);
            PositionToTile(pos2, tilesData, out int x2, out int y2);
            PositionToTile(start, tilesData, out int ox, out int oy);

            Line(tilesData, x1, y1, x2, y2, ox, oy, erase);
        }

        private void SetTile(int x, int y, Layer.LayerTilesData tilesData, uint tileID)
        {
            Modified = true;
            if (tilesData.TileData[y][x] != tileID)
            {
                Tuple<int, int> key = new(x, y);
                UndoStack[UndoStack.Count - 1][key] = tilesData.TileData[y][x];

                tilesData.TileData[y][x] = tileID;
                DrawTile(
                    tilesData.Background, tileID,
                    TilesBitmap, x, y
                );
            }
        }

        // Places one tile of the current brush.
        // ox and oy specify the origin point of multi-tile brushes.
        private void SetBrushTile(Layer.LayerTilesData tilesData, int x, int y, int ox, int oy)
        {
            int tx = mod(x - ox, (int)BrushTilesData.TilesX);
            int ty = mod(y - oy, (int)BrushTilesData.TilesY);
            uint tile = BrushTilesData.TileData[ty][tx];
            if ((tile & TILE_INDEX) != 0 || BrushEmpty)
                SetTile(x, y, tilesData, tile);
        }

        public void Fill(Layer.LayerTilesData tilesData, int x, int y, bool global, bool erase = false)
        {
            uint[][] data = tilesData.TileData;
            uint replace = data[y][x];

            if (global)
            {
                for (int fy = 0; fy < tilesData.TilesY; fy++)
                {
                    for (int fx = 0; fx < tilesData.TilesX; fx++)
                    {
                        if (data[fy][fx] == replace)
                        {
                            if (erase)
                                SetTile(fx, fy, tilesData, 0);
                            else
                                SetBrushTile(tilesData, fx, fy, x, y);
                        }
                    }
                }
                return;
            }

            Stack<Tuple<int, int>> stack = new();
            stack.Push(new(x, y));
            HashSet<Tuple<int, int>> handled = new();
            while (stack.Count > 0)
            {
                Tuple<int, int> tuple = stack.Pop();
                if (handled.Contains(tuple))
                    continue;
                handled.Add(tuple);
                int fx = tuple.Item1;
                int fy = tuple.Item2;
                if (data[fy][fx] == replace)
                {
                    if (erase)
                        SetTile(fx, fy, tilesData, 0);
                    else
                        SetBrushTile(tilesData, fx, fy, x, y);
                    // if this fill just did nothing
                    // (fixes infinite loops)
                    if (data[fy][fx] == replace)
                        continue;
                    if (fx > 0) stack.Push(new(fx - 1, fy));
                    if (fy > 0) stack.Push(new(fx, fy - 1));
                    if (fx < (tilesData.TilesX - 1)) stack.Push(new(fx + 1, fy));
                    if (fy < (tilesData.TilesY - 1)) stack.Push(new(fx, fy + 1));
                }
            }
        }

        private void Line(Layer.LayerTilesData tilesData, int x1, int y1, int x2, int y2, int ox, int oy, bool erase = false)
        {
            int dx = Math.Abs(x2 - x1);
            int sx = x1 < x2 ? 1 : -1;
            int dy = -Math.Abs(y2 - y1);
            int sy = y1 < y2 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                PaintTile(x1, y1, settings.BrushTiling ? ox : x1, settings.BrushTiling ? oy : y1, tilesData, erase);

                if (x1 == x2 && y1 == y2)
                    break;

                int e2 = 2 * error;
                if (e2 >= dy)
                {
                    if (x1 == x2)
                        break;
                    error += dy;
                    x1 += sx;
                }
                if (e2 <= dx)
                {
                    if (y1 == y2)
                        break;
                    error += dx;
                    y1 += sy;
                }
            }
        }

        public void Pick(Point pos, Point drawingStart, Layer.LayerTilesData tilesData)
        {
            bool boundsA = PositionToTile(drawingStart, tilesData, out int x1, out int y1);
            bool boundsB = PositionToTile(pos, tilesData, out int x2, out int y2);
            if (!boundsA && !boundsB) return;
            x1 = Math.Clamp(x1, 0, (int)tilesData.TilesX - 1);
            y1 = Math.Clamp(y1, 0, (int)tilesData.TilesY - 1);
            x2 = Math.Clamp(x2, 0, (int)tilesData.TilesX - 1);
            y2 = Math.Clamp(y2, 0, (int)tilesData.TilesY - 1);
            if (x2 < x1)
            {
                (x1, x2) = (x2, x1);
            }
            if (y2 < y1)
            {
                (y1, y2) = (y2, y1);
            }

            BrushTilesData.TilesX = (uint)(Math.Abs(x2 - x1) + 1);
            BrushTilesData.TilesY = (uint)(Math.Abs(y2 - y1) + 1);

            for (int y = 0; y < BrushTilesData.TilesY; y++)
            {
                for (int x = 0; x < BrushTilesData.TilesX; x++)
                {
                    BrushTilesData.TileData[y][x] = tilesData.TileData[y1 + y][x1 + x];
                }
            }

            UpdateBrush(true);

            if (tilesData == PaletteTilesData)
            {
                MovePaletteCursor(x1, y1);
                ResizePaletteCursor();
                PaletteCursorVisibility = true;
            }
        }

        public void FindPaletteCursor()
        {
            if (BrushTilesData.TilesX > 1 || BrushTilesData.TilesY > 1)
            {
                PaletteCursorVisibility = false;
                return;
            }
            PaletteCursorVisibility = true;

            uint brushTile = BrushTilesData.TileData[0][0] & TILE_INDEX;
            int index = PaletteTilesData.Background.GMS2TileIds.FindIndex(
                id => id.ID == brushTile
            );
            if (index == -1)
                index = 0;
            MovePaletteCursor((int)(index / PaletteTilesData.Background.GMS2ItemsPerTileCount));
            ResizePaletteCursor();
            // TODO if (PaletteCursor is not null)
            //    PaletteCursor.BringIntoView();
        }

        private void MovePaletteCursor(int index)
        {
            MovePaletteCursor((index % (int)PaletteTilesData.TilesX), (index / (int)PaletteTilesData.TilesX));
        }

        private void MovePaletteCursor(int x, int y)
        {
            PaletteCursorX = x * (int)PaletteTilesData.Background.GMS2TileWidth;
            PaletteCursorY = y * (int)PaletteTilesData.Background.GMS2TileHeight;
        }

        private void ResizePaletteCursor()
        {
            PaletteCursorWidth = BrushTilesData.TilesX * (int)PaletteTilesData.Background.GMS2TileWidth;
            PaletteCursorHeight = BrushTilesData.TilesY * (int)PaletteTilesData.Background.GMS2TileHeight;
        }
        #endregion

        #region Tile drawing
        private void DrawTilemap(Layer.LayerTilesData tilesData, WriteableBitmap wBitmap)
        {
            if ((loader.Convert(new object[] { tilesData }, null, "cache", null) as string) == "Error")
                return;

            for (int y = 0; y < tilesData.TilesY; y++)
            {
                for (int x = 0; x < tilesData.TilesX; x++)
                {
                    DrawTile(
                        tilesData.Background, tilesData.TileData[y][x],
                        wBitmap, x, y
                    );
                }
            }
        }

        // assumes a bgra32 writeablebitmap
        private void DrawTile(UndertaleBackground tileset, uint tile, WriteableBitmap wBitmap, int x, int y)
        {
            uint tileID = tile & TILE_INDEX;
            if (tileID == 0)
            {
                ClearToWBitmap(
                    wBitmap, (int)(x * tileset.GMS2TileWidth), (int)(y * tileset.GMS2TileHeight),
                    (int)tileset.GMS2TileWidth, (int)tileset.GMS2TileHeight
                );
                return;
            }

            Bitmap tileBMP = CachedTileDataLoader.TileCache[new(tileset.Texture.Name.Content, tileID)];

            if ((tile & TILE_FLAGS) == 0)
            {
                //if (TileCache.TryGetValue(tileID, out byte[] tileBytes))
                //{
                //    wBitmap.CopyPixels(
                //        new PixelRect((int)(x * tileset.GMS2TileWidth), (int)(y * tileset.GMS2TileHeight), (int)tileset.GMS2TileWidth, (int)tileset.GMS2TileHeight),
                //        tileBytes, tileBytes.Length,
                //        (int)tileset.GMS2TileWidth * 4
                //    );
                //    return;
                //}
                DrawBitmapToWBitmap(
                    tileBMP, wBitmap,
                    (int)(x * tileset.GMS2TileWidth), (int)(y * tileset.GMS2TileHeight),
                    tileID
                );
                return;
            }

            Bitmap transformedBmp = tileBMP;

            switch (tile >> 28)
            {
                case 1:
                    transformedBmp = transformedBmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    break;
                case 2:
                    transformedBmp = transformedBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    break;
                case 3:
                    transformedBmp = transformedBmp.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                    break;
                case 4:
                    transformedBmp = transformedBmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case 5:
                    // axes flipped since flip/mirror is done before rotation
                    transformedBmp = transformedBmp.RotateFlip(RotateFlipType.Rotate90FlipY);
                    break;
                case 6:
                    transformedBmp = transformedBmp.RotateFlip(RotateFlipType.Rotate90FlipX);
                    break;
                case 7:
                    transformedBmp = transformedBmp.RotateFlip(RotateFlipType.Rotate90FlipXY);
                    break;
                default:
                    throw new InvalidDataException($"{tile & TILE_FLAGS} is not a valid tile flag value.");
            }

            DrawBitmapToWBitmap(
                transformedBmp, wBitmap,
                (int)(x * tileset.GMS2TileWidth), (int)(y * tileset.GMS2TileHeight)
            );
        }
        private void DrawBitmapToWBitmap(Bitmap bitmap, WriteableBitmap wBitmap, int x, int y, uint? cache = null)
        {
            if (cache is uint cacheId && TileCache.TryGetValue(cacheId, out byte[] cachedBytes))
            {
                CopyBytesToWBitmap(wBitmap,
                    new PixelRect(x, y, (int)bitmap.Size.Width, (int)bitmap.Size.Height),
                    cachedBytes);
                return;
            }

            using var ms = new MemoryStream();
            bitmap.Save(ms);
            ms.Position = 0;
            using var skBitmap = SKBitmap.Decode(ms);

            if (skBitmap == null) return;

            int width = skBitmap.Width;
            int height = skBitmap.Height;
            byte[] arr = new byte[width * height * 4];

            int i = 0;
            for (int by = 0; by < height; by++)
            {
                for (int bx = 0; bx < width; bx++)
                {
                    SKColor color = skBitmap.GetPixel(bx, by);
                    arr[i++] = color.Blue;
                    arr[i++] = color.Green;
                    arr[i++] = color.Red;
                    arr[i++] = color.Alpha;
                }
            }

            if (cache is uint cacheID)
            {
                TileCache.TryAdd(cacheID, arr);
            }

            CopyBytesToWBitmap(wBitmap, new PixelRect(x, y, width, height), arr);
        }
        private void ClearToWBitmap(WriteableBitmap wBitmap, int x, int y, int width, int height)
        {
            CopyBytesToWBitmap(wBitmap, new PixelRect(x, y, width, height), emptyTile);
        }

        private void CopyBytesToWBitmap(WriteableBitmap wBitmap, PixelRect rect, byte[] data)
        {
            using (ILockedFramebuffer framebuffer = wBitmap.Lock())
            {
                nint destPtr = framebuffer.Address + (rect.Y * framebuffer.RowBytes) + (rect.X * 4);
                int destStride = framebuffer.RowBytes;

                int sourceStride = rect.Width * 4;

                if (sourceStride == destStride)
                {
                    Marshal.Copy(data, 0, destPtr, data.Length);
                }
                else
                {
                    for (int row = 0; row < rect.Height; row++)
                    {
                        nint rowPtr = destPtr + (row * destStride);
                        int sourceOffset = row * sourceStride;
                        Marshal.Copy(data, sourceOffset, rowPtr, sourceStride);
                    }
                }
            }
        }

        private void EndDrawing()
        {
            if (Painting == PaintAction.None)
                return;
            Painting = PaintAction.None;
            FocusedTilesData = null;
            UpdateBrush(false);
        }
        public void EndDrawing(Point position)
        {
            if (Painting == PaintAction.None)
                return;
            if (Painting == PaintAction.Pick)
            {
                PositionToTile(position, TilesData, out int mapX, out int mapY);
                BrushPreviewX = Convert.ToDouble((long)mapX * (long)TilesData.Background.GMS2TileWidth);
                BrushPreviewY = Convert.ToDouble((long)mapY * (long)TilesData.Background.GMS2TileHeight);
                if (FocusedTilesData != PaletteTilesData)
                {
                    FindPaletteCursor();
                }
                RefreshBrush++;
            }
            EndDrawing();
        }
        #endregion

        #region Commands
        [RelayCommand]
        private void Mirror()
        {
            for (int y = 0; y < BrushTilesData.TilesY; y++)
            {
                Array.Reverse(BrushTilesData.TileData[y]);
                for (int x = 0; x < BrushTilesData.TilesX; x++)
                {
                    if ((BrushTilesData.TileData[y][x] & TILE_ROTATE) != 0)
                        BrushTilesData.TileData[y][x] ^= TILE_FLIP_V;
                    else
                        BrushTilesData.TileData[y][x] ^= TILE_FLIP_H;
                }
            }
            RefreshBrush++;
        }

        [RelayCommand]
        private void Flip()
        {
            Array.Reverse(BrushTilesData.TileData);
            for (int y = 0; y < BrushTilesData.TilesY; y++)
            {
                for (int x = 0; x < BrushTilesData.TilesX; x++)
                {
                    if ((BrushTilesData.TileData[y][x] & TILE_ROTATE) != 0)
                        BrushTilesData.TileData[y][x] ^= TILE_FLIP_H;
                    else
                        BrushTilesData.TileData[y][x] ^= TILE_FLIP_V;
                }
            }
            RefreshBrush++;
        }

        [RelayCommand]
        private void RotateCW()
        {
            uint[][] oldTileData = CloneTileData(BrushTilesData.TileData);
            uint _tilesX = BrushTilesData.TilesX;
            uint _tilesY = BrushTilesData.TilesY;
            BrushTilesData.TilesX = _tilesY;
            BrushTilesData.TilesY = _tilesX;
            for (int y = 0; y < _tilesY; y++)
            {
                for (int x = 0; x < _tilesX; x++)
                {
                    uint tile = oldTileData[y][x];
                    uint flags = ROTATION_CW[(uint)(tile >> 28)] << 28;
                    BrushTilesData.TileData[x][_tilesY - y - 1] = (uint)((tile & TILE_INDEX) | flags);
                }
            }
            UpdateBrush(false);
            RefreshBrush++;
        }

        [RelayCommand]
        private void RotateCCW()
        {
            uint[][] oldTileData = CloneTileData(BrushTilesData.TileData);
            uint _tilesX = BrushTilesData.TilesX;
            uint _tilesY = BrushTilesData.TilesY;
            BrushTilesData.TilesX = _tilesY;
            BrushTilesData.TilesY = _tilesX;
            for (int y = 0; y < _tilesY; y++)
            {
                for (int x = 0; x < _tilesX; x++)
                {
                    uint tile = oldTileData[y][x];
                    uint flags = ROTATION_CCW[(uint)(tile >> 28)] << 28;
                    BrushTilesData.TileData[_tilesX - x - 1][y] = (uint)((tile & TILE_INDEX) | flags);
                }
            }
            UpdateBrush(false);
            RefreshBrush++;
        }

        [RelayCommand]
        private void ToggleGrid()
        {
            settings.ShowGridBool = !settings.ShowGridBool;
        }

        [RelayCommand]
        private void ToggleBrushTiling()
        {
            settings.BrushTiling = !settings.BrushTiling;
        }

        [RelayCommand]
        private void TogglePreview()
        {
            settings.RoomPreviewBool = !settings.RoomPreviewBool;
        }

        [RelayCommand]
        private void Undo()
        {
            if (UndoStack.Count == 0)
                return;
            EndDrawing();
            int index = UndoStack.Count - 1;
            var undoData = UndoStack[index];
            ApplyUndo(undoData);
            UndoStack.RemoveAt(index);
            RedoStack.Add(undoData);
            UndoEnabled = UndoStack.Count > 0;
            RedoEnabled = true;

        }

        [RelayCommand]
        private void Redo()
        {
            if (RedoStack.Count == 0)
                return;
            EndDrawing();
            int index = RedoStack.Count - 1;
            var undoData = RedoStack[index];
            ApplyUndo(undoData);
            RedoStack.RemoveAt(index);
            UndoStack.Add(undoData);
            UndoEnabled = true;
            RedoEnabled = RedoStack.Count > 0;
        }

        [RelayCommand]
        private void RecordUndo()
        {
            if (UndoStack.Count >= 100)
                UndoStack.RemoveAt(1);
            UndoStack.Add(new());
            RedoStack.Clear();
            UndoEnabled = true;
            RedoEnabled = false;
        }
        // Applies some undo data, and also "swaps" it
        // to instead redo.
        private void ApplyUndo(Dictionary<Tuple<int, int>, uint> data)
        {
            foreach (KeyValuePair<Tuple<int, int>, uint> kvp in data)
            {
                int x = kvp.Key.Item1;
                int y = kvp.Key.Item2;
                uint tile = kvp.Value;
                uint oldTile = TilesData.TileData[y][x];
                TilesData.TileData[y][x] = tile;
                DrawTile(
                    TilesData.Background, tile,
                    TilesBitmap, x, y
                );
                data[kvp.Key] = oldTile;
            }
        }
        #endregion

        #region Utilities
        private int mod(int left, int right)
        {
            int remainder = left % right;
            return remainder < 0 ? remainder + right : remainder;
        }

        private uint[][] CloneTileData(uint[][] tileData)
        {
            uint[][] newTileData = (uint[][])tileData.Clone();
            for (int i = 0; i < tileData.Length; i++)
                newTileData[i] = (uint[])tileData[i].Clone();
            return newTileData;
        }

        public bool PositionToTile(Point p, Layer.LayerTilesData tilesData, out int x, out int y)
        {
            x = Convert.ToInt32(Math.Floor(p.X / tilesData.Background.GMS2TileWidth));
            y = Convert.ToInt32(Math.Floor(p.Y / tilesData.Background.GMS2TileHeight));

            return TileInBounds(x, y, tilesData);
        }

        private bool TileInBounds(int x, int y, Layer.LayerTilesData tilesData)
        {
            if (x < 0 || y < 0) return false;
            if (x >= tilesData.TilesX || y >= tilesData.TilesY) return false;
            return true;
        }
        #endregion
    }
}
