using System.Text.Json.Serialization;

namespace PjumModLib;

public sealed class GameJson
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("fileName")] public string? FileName { get; set; }
}
