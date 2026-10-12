using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.ComponentModel;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Converters;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Controls
{
    public class RoomBackgroundDisplay : Control
    {
        private static readonly UndertaleCachedImageLoader loader = new();

        public static readonly StyledProperty<object?> BackgroundObjectProperty =
            AvaloniaProperty.Register<RoomBackgroundDisplay, object?>(nameof(BackgroundObject));

        public object? BackgroundObject
        {
            get => GetValue(BackgroundObjectProperty);
            set => SetValue(BackgroundObjectProperty, value);
        }

        public Rect DrawnBounds { get; private set; }

        static RoomBackgroundDisplay()
        {
            AffectsRender<RoomBackgroundDisplay>(BackgroundObjectProperty);
            AffectsMeasure<RoomBackgroundDisplay>(BackgroundObjectProperty);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property != BackgroundObjectProperty)
                return;

            Subscribe(change.OldValue, false);
            Subscribe(change.NewValue, true);
        }

        private void Subscribe(object? obj, bool add)
        {
            INotifyPropertyChanged?[] sources = obj switch
            {
                Background bg => new INotifyPropertyChanged?[] { bg, bg.ParentRoom },
                Layer.LayerBackgroundData bgData => new INotifyPropertyChanged?[] { bgData, bgData.ParentLayer, bgData.ParentLayer?.ParentRoom },
                _ => Array.Empty<INotifyPropertyChanged?>()
            };

            foreach (INotifyPropertyChanged? source in sources)
            {
                if (source is null)
                    continue;

                if (add)
                    source.PropertyChanged += Source_PropertyChanged;
                else
                    source.PropertyChanged -= Source_PropertyChanged;
            }
        }

        private void Source_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            InvalidateMeasure();
            InvalidateVisual();
        }

        private UndertaleRoom? GetRoom()
        {
            return BackgroundObject switch
            {
                Background bg => bg.ParentRoom,
                Layer.LayerBackgroundData bgData => bgData.ParentLayer?.ParentRoom,
                _ => null
            };
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UndertaleRoom? room = GetRoom();
            if (room is null)
                return default;

            return new Size(room.Width, room.Height);
        }

        public override void Render(DrawingContext context)
        {
            DrawnBounds = default;

            UndertaleTexturePageItem? texture;
            bool tiledH, tiledV, stretch, isLayer;
            double scaleX, scaleY, xOffset, yOffset, left, top;

            if (BackgroundObject is Background bg)
            {
                texture = bg.BackgroundDefinition?.Texture;
                if (texture is null || bg.ParentRoom is null)
                    return;

                bg.UpdateStretch();
                isLayer = false;
                tiledH = bg.TiledHorizontally;
                tiledV = bg.TiledVertically;
                stretch = bg.Stretch;
                scaleX = bg.CalcScaleX;
                scaleY = bg.CalcScaleY;
                xOffset = left = bg.XOffset;
                yOffset = top = bg.YOffset;
            }
            else if (BackgroundObject is Layer.LayerBackgroundData bgData)
            {
                if (bgData.Sprite is null || bgData.Sprite.Textures.Count == 0 || bgData.ParentLayer?.ParentRoom is null)
                    return;

                texture = bgData.Sprite.Textures[0].Texture;
                if (texture is null)
                    return;

                bgData.UpdateScale();
                isLayer = true;
                tiledH = bgData.TiledHorizontally;
                tiledV = bgData.TiledVertically;
                stretch = bgData.Stretch;
                scaleX = bgData.CalcScaleX;
                scaleY = bgData.CalcScaleY;
                xOffset = bgData.ParentLayer.XOffset;
                yOffset = bgData.ParentLayer.YOffset;
                left = bgData.XOffset;
                top = bgData.YOffset;
            }
            else
                return;

            UndertaleRoom? room = GetRoom();
            double texW = texture.SourceWidth;
            double texH = texture.SourceHeight;
            if (room is null || texW <= 0 || texH <= 0 || scaleX == 0 || scaleY == 0)
                return;
            if (loader.Convert(texture, typeof(IImage), null, System.Globalization.CultureInfo.InvariantCulture) is not Bitmap bitmap)
                return;

            double offsetH = xOffset / scaleX;
            double offsetV = yOffset / scaleY;
            if (isLayer)
            {
                left /= scaleX;
                top /= scaleY;
            }

            double width, height, phaseX, phaseY;
            if (tiledH)
            {
                left = 0;
                width = stretch ? texW : room.Width;
                phaseX = stretch ? offsetH : xOffset;
            }
            else
            {
                width = stretch ? texW - offsetH : texW;
                phaseX = left;
            }
            if (tiledV)
            {
                top = 0;
                height = stretch ? texH : room.Height;
                phaseY = stretch ? offsetV : yOffset;
            }
            else
            {
                height = stretch ? texH - offsetV : texH;
                phaseY = top;
            }
            if (width <= 0 || height <= 0)
                return;

            double startX = tiledH ? phaseX - Math.Ceiling((phaseX - left) / texW) * texW : phaseX;
            double startY = tiledV ? phaseY - Math.Ceiling((phaseY - top) / texH) * texH : phaseY;

            DrawnBounds = new Rect(left * scaleX, top * scaleY, width * scaleX, height * scaleY);

            using (context.PushTransform(Matrix.CreateScale(scaleX, scaleY)))
            using (context.PushClip(new Rect(left, top, width, height)))
            {
                for (double x = startX; x < left + width; x += texW)
                {
                    for (double y = startY; y < top + height; y += texH)
                    {
                        context.DrawImage(bitmap, new Rect(x, y, texW, texH));

                        if (!tiledV)
                            break;
                    }

                    if (!tiledH)
                        break;
                }
            }
        }
    }
}
