using System.Text.Json.Serialization;

namespace PjumModLib;

public sealed class PathJson
{
    [JsonPropertyName("smooth")]   public bool Smooth { get; set; }
    [JsonPropertyName("closed")]   public bool Closed { get; set; }
    [JsonPropertyName("precision")] public uint Precision { get; set; } = 4;
    [JsonPropertyName("points")]   public List<PathPointJson> Points { get; set; } = new();
}

public sealed class PathPointJson
{
    [JsonPropertyName("x")]     public float X { get; set; }
    [JsonPropertyName("y")]     public float Y { get; set; }
    [JsonPropertyName("speed")] public float Speed { get; set; } = 100f;
}
