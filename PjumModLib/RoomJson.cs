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
