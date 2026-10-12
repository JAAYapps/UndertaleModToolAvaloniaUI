using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class RoomObjectTransformConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 0)
                return null;

            Matrix matrix = values[0] switch
            {
                GameObject obj => Matrix.CreateTranslation(obj.SpriteXOffset, obj.SpriteYOffset)
                                  * Matrix.CreateScale(obj.ScaleX, obj.ScaleY)
                                  * Matrix.CreateRotation(Matrix.ToRadians(obj.OppositeRotation))
                                  * Matrix.CreateTranslation(obj.X, obj.Y),

                SpriteInstance spr => Matrix.CreateTranslation(spr.SpriteXOffset, spr.SpriteYOffset)
                                      * Matrix.CreateScale(spr.ScaleX, spr.ScaleY)
                                      * Matrix.CreateRotation(Matrix.ToRadians(spr.OppositeRotation))
                                      * Matrix.CreateTranslation(spr.X, spr.Y),

                Tile tile => Matrix.CreateScale(tile.ScaleX, tile.ScaleY)
                             * Matrix.CreateTranslation(tile.X, tile.Y),

                ParticleSystemInstance partSys => Matrix.CreateTranslation(-8, -8)
                                                  * Matrix.CreateTranslation(partSys.X, partSys.Y)
                                                  * Matrix.CreateScale(partSys.ScaleX, partSys.ScaleY)
                                                  * Matrix.CreateTranslation(-partSys.X, -partSys.Y)
                                                  * Matrix.CreateRotation(Matrix.ToRadians(partSys.OppositeRotation))
                                                  * Matrix.CreateTranslation(partSys.X, partSys.Y),

                Layer { LayerType: not (LayerType.Instances or LayerType.Background) } layer => Matrix.CreateTranslation(layer.XOffset, layer.YOffset),

                _ => Matrix.Identity
            };

            return new MatrixTransform(matrix);
        }
    }
}
