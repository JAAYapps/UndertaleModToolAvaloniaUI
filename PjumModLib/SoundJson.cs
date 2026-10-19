using System.Text.Json.Serialization;

namespace PjumModLib;

public sealed class SoundJson
{
    [JsonPropertyName("type")]         public string? Type { get; set; } = null;
    [JsonPropertyName("embedded")]     public bool Embedded { get; set; } = true;
    [JsonPropertyName("decodeOnLoad")] public bool DecodeOnLoad { get; set; }
    [JsonPropertyName("volume")]       public float Volume { get; set; } = 1f;
    [JsonPropertyName("pitch")]        public float Pitch { get; set; } = 1f;
    [JsonPropertyName("effects")]      public uint Effects { get; set; }
}
