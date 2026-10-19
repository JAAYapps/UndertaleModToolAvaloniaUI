using System.Text.Json.Serialization;

namespace PjumModLib;

public sealed class SpriteJson
{
    [JsonPropertyName("width")] public uint? Width { get; set; }
    [JsonPropertyName("height")] public uint? Height { get; set; }
    [JsonPropertyName("originX")] public int? OriginX { get; set; }
    [JsonPropertyName("originY")] public int? OriginY { get; set; }
    [JsonPropertyName("margins")] public SpriteMarginsJson? Margins { get; set; }
    [JsonPropertyName("bboxMode")] public uint? BBoxMode { get; set; }
    [JsonPropertyName("collisionKind")] public string? CollisionKind { get; set; }
    [JsonPropertyName("transparent")] public bool? Transparent { get; set; }
    [JsonPropertyName("smooth")] public bool? Smooth { get; set; }
    [JsonPropertyName("preload")] public bool? Preload { get; set; }
    [JsonPropertyName("frames")] public int? Frames { get; set; }
    [JsonPropertyName("masks")] public int? Masks { get; set; }
}

public sealed class SpriteMarginsJson
{
    [JsonPropertyName("left")] public int Left { get; set; }
    [JsonPropertyName("top")] public int Top { get; set; }
    [JsonPropertyName("right")] public int Right { get; set; }
    [JsonPropertyName("bottom")] public int Bottom { get; set; }
}
