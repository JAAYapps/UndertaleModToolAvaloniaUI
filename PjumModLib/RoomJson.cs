using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;

namespace PjumModLib;

public sealed class RoomJson
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("add")] public List<RoomInstanceJson> Add { get; set; } = new();
    [JsonPropertyName("change")] public List<RoomInstanceJson> Change { get; set; } = new();
    [JsonPropertyName("remove")] public List<uint> Remove { get; set; } = new();
    [JsonPropertyName("settings")] public RoomSettingsJson? Settings { get; set; }
    [JsonPropertyName("backgrounds")] public List<RoomBackgroundJson>? Backgrounds { get; set; }
    [JsonPropertyName("views")] public List<RoomViewJson>? Views { get; set; }
    [JsonPropertyName("tiles")] public RoomTilesJson? Tiles { get; set; }
}

public sealed class RoomSettingsJson
{
    [JsonPropertyName("width")] public uint? Width { get; set; }
    [JsonPropertyName("height")] public uint? Height { get; set; }
    [JsonPropertyName("speed")] public uint? Speed { get; set; }
    [JsonPropertyName("persistent")] public bool? Persistent { get; set; }
    [JsonPropertyName("backgroundColor")] public uint? BackgroundColor { get; set; }
    [JsonPropertyName("drawBackgroundColor")] public bool? DrawBackgroundColor { get; set; }
    [JsonPropertyName("caption")] public string? Caption { get; set; }
}

public sealed class RoomBackgroundJson
{
    [JsonPropertyName("slot")] public int Slot { get; set; }
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("background")] public string? Background { get; set; }
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
    [JsonPropertyName("tileX")] public bool TileX { get; set; }
    [JsonPropertyName("tileY")] public bool TileY { get; set; }
    [JsonPropertyName("speedX")] public int SpeedX { get; set; }
    [JsonPropertyName("speedY")] public int SpeedY { get; set; }
    [JsonPropertyName("foreground")] public bool Foreground { get; set; }
    [JsonPropertyName("stretch")] public bool Stretch { get; set; }
}

public sealed class RoomViewJson
{
    [JsonPropertyName("slot")] public int Slot { get; set; }
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("viewX")] public int ViewX { get; set; }
    [JsonPropertyName("viewY")] public int ViewY { get; set; }
    [JsonPropertyName("viewWidth")] public int ViewWidth { get; set; }
    [JsonPropertyName("viewHeight")] public int ViewHeight { get; set; }
    [JsonPropertyName("portX")] public int PortX { get; set; }
    [JsonPropertyName("portY")] public int PortY { get; set; }
    [JsonPropertyName("portWidth")] public int PortWidth { get; set; }
    [JsonPropertyName("portHeight")] public int PortHeight { get; set; }
    [JsonPropertyName("borderX")] public uint BorderX { get; set; }
    [JsonPropertyName("borderY")] public uint BorderY { get; set; }
    [JsonPropertyName("speedX")] public int SpeedX { get; set; }
    [JsonPropertyName("speedY")] public int SpeedY { get; set; }
    [JsonPropertyName("follow")] public string? Follow { get; set; }
}

public sealed class RoomTilesJson
{
    [JsonPropertyName("add")] public List<RoomTileJson> Add { get; set; } = new();
    [JsonPropertyName("change")] public List<RoomTileJson> Change { get; set; } = new();
    [JsonPropertyName("remove")] public List<uint> Remove { get; set; } = new();
}

public sealed class RoomTileJson
{
    [JsonPropertyName("id")] public uint? Id { get; set; }
    [JsonPropertyName("background")] public string? Background { get; set; }
    [JsonPropertyName("x")] public int? X { get; set; }
    [JsonPropertyName("y")] public int? Y { get; set; }
    [JsonPropertyName("sourceX")] public int? SourceX { get; set; }
    [JsonPropertyName("sourceY")] public int? SourceY { get; set; }
    [JsonPropertyName("width")] public uint? Width { get; set; }
    [JsonPropertyName("height")] public uint? Height { get; set; }
    [JsonPropertyName("depth")] public int? Depth { get; set; }
    [JsonPropertyName("scaleX")] public float? ScaleX { get; set; }
    [JsonPropertyName("scaleY")] public float? ScaleY { get; set; }
    [JsonPropertyName("color")] public uint? Color { get; set; }
}

public sealed class RoomInstanceJson
{
    [JsonPropertyName("id")] public uint? Id { get; set; }
    [JsonPropertyName("object")] public string? Object { get; set; }
    [JsonPropertyName("x")] public int? X { get; set; }
    [JsonPropertyName("y")] public int? Y { get; set; }
    [JsonPropertyName("scaleX")] public float? ScaleX { get; set; }
    [JsonPropertyName("scaleY")] public float? ScaleY { get; set; }
    [JsonPropertyName("rotation")] public float? Rotation { get; set; }
    [JsonPropertyName("color")] public uint? Color { get; set; }
    [JsonPropertyName("imageIndex")] public int? ImageIndex { get; set; }
    [JsonPropertyName("imageSpeed")] public float? ImageSpeed { get; set; }
    [JsonPropertyName("layer")] public string? Layer { get; set; }
}

public static class RoomLayers
{
    public static UndertaleRoom.Layer? LayerOf(UndertaleRoom room, UndertaleRoom.GameObject instance) =>
        InstanceLayers(room).FirstOrDefault(l => l.InstancesData.Instances.Contains(instance));

    public static IEnumerable<UndertaleRoom.Layer> InstanceLayers(UndertaleRoom room) =>
        (room.Layers ?? []).Where(l => l.LayerType == UndertaleRoom.LayerType.Instances && l.InstancesData is not null);
}
