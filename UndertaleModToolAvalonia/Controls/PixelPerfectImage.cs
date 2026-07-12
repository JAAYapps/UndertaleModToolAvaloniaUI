using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Controls
{
    // Original source was for WPF. This code is a derivative for Avalonia by recreating the Image Control
    // source - https://stackoverflow.com/a/4801434/12136394
    public class PixelPerfectImage : Control
    {
        // Reimplemented the properties needed from the Image control

        public static readonly StyledProperty<IImage?> SourceProperty =
            AvaloniaProperty.Register<PixelPerfectImage, IImage?>(nameof(Source));

        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<PixelPerfectImage, Stretch>(nameof(Stretch), Stretch.Uniform);

        public static readonly StyledProperty<Layer.LayerTilesData?> LayerTilesDataProperty =
            AvaloniaProperty.Register<PixelPerfectImage, Layer.LayerTilesData?>(nameof(LayerTilesData));

        public static readonly StyledProperty<bool> CheckTransparencyProperty =
            AvaloniaProperty.Register<PixelPerfectImage, bool>(nameof(CheckTransparency));

        [Content]
        public IImage? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public Layer.LayerTilesData? LayerTilesData
        {
            get => GetValue(LayerTilesDataProperty);
            set => SetValue(LayerTilesDataProperty, value);
        }

        public bool CheckTransparency
        {
            get => GetValue(CheckTransparencyProperty);
            set => SetValue(CheckTransparencyProperty, value);
        }

        static PixelPerfectImage()
        {
            AffectsRender<PixelPerfectImage>(SourceProperty, StretchProperty);
            AffectsMeasure<PixelPerfectImage>(SourceProperty, StretchProperty);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (Source is not Bitmap sourceBitmap) return;

            var point = e.GetPosition(this);

            // If the point is outside the bounds, do nothing.
            if (!new Rect(this.Bounds.Size).Contains(point)) return;

            // Send data to Skia Sharp
            using var memoryStream = new MemoryStream();
            sourceBitmap.Save(memoryStream);
            memoryStream.Position = 0;

            using var skBitmap = SKBitmap.Decode(memoryStream);

            if (skBitmap == null) return;

            var pixelSize = skBitmap.Info.Size;
            int x = (int)(point.X / Bounds.Width * pixelSize.Width);
            int y = (int)(point.Y / Bounds.Height * pixelSize.Height);

            if (x < 0 || x >= pixelSize.Width || y < 0 || y >= pixelSize.Height) return;

            bool isHit = true;

            if (CheckTransparency)
            {
                SKColor color = skBitmap.GetPixel(x, y);

                if (color.Alpha == 0)
                    isHit = false;
            }
            else
            {
                if (LayerTilesData == null)
                {
                    isHit = false;
                }
                else
                {
                    int x1 = x / (int)LayerTilesData.Background.GMS2TileWidth;
                    int y1 = y / (int)LayerTilesData.Background.GMS2TileHeight;

                    if (x1 < 0 || x1 > LayerTilesData.TilesX - 1 ||
                        y1 < 0 || y1 > LayerTilesData.TilesY - 1 ||
                        LayerTilesData.TileData[y1][x1] == 0)
                        isHit = false;
                }
            }

            if (isHit)
                e.Handled = true;
        }

        public override void Render(DrawingContext context)
        {
            if (Source != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                Rect viewPort = new Rect(Bounds.Size);
                Size sourceSize = Source.Size;

                Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize);
                Size scaledSize = sourceSize * scale;
                Rect destRect = viewPort
                    .CenterRect(new Rect(scaledSize))
                    .Intersect(viewPort);

                Rect sourceRect = new Rect(sourceSize)
                    .CenterRect(new Rect(destRect.Size / scale));

                context.DrawImage(Source, sourceRect, destRect);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Source == null) return new Size();

            return Stretch.CalculateSize(availableSize, Source.Size);
        }
    }
}
