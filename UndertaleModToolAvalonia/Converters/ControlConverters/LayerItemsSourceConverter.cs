using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class LayerItemsSourceConverter : IValueConverter
    {
        private static readonly MultiCollectionBinding multiCollection = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Layer layer)
            {
                return layer.LayerType switch
                {
                    LayerType.Assets => multiCollection.Convert(new object?[] { layer.AssetsData.LegacyTiles, layer.AssetsData.Sprites, layer.AssetsData.ParticleSystems },
                                                                targetType, null, culture),
                    LayerType.Background => new List<Layer.LayerBackgroundData>() { layer.BackgroundData },
                    LayerType.Instances => layer.InstancesData.Instances,
                    LayerType.Tiles => new List<Layer.LayerTilesData>() { layer.TilesData },
                    _ => null
                };
            }
            else
                return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
