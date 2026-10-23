using System.Text.Json;
using System.Text.Json.Serialization;

namespace PjumModLib;

public sealed class FontJson
{
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("emSize")] public float? EmSize { get; set; }
    [JsonPropertyName("emSizeIsFloat")] public bool? EmSizeIsFloat { get; set; }
    [JsonPropertyName("bold")] public bool? Bold { get; set; }
    [JsonPropertyName("italic")] public bool? Italic { get; set; }
    [JsonPropertyName("rangeStart")] public ushort? RangeStart { get; set; }
    [JsonPropertyName("rangeEnd")] public uint? RangeEnd { get; set; }
    [JsonPropertyName("charset")] public byte? Charset { get; set; }
    [JsonPropertyName("antiAliasing")] public byte? AntiAliasing { get; set; }
    [JsonPropertyName("scaleX")] public float? ScaleX { get; set; }
    [JsonPropertyName("scaleY")] public float? ScaleY { get; set; }
    [JsonPropertyName("ascenderOffset")] public int? AscenderOffset { get; set; }
    [JsonPropertyName("ascender")] public uint? Ascender { get; set; }
    [JsonPropertyName("sdfSpread")] public uint? SdfSpread { get; set; }
    [JsonPropertyName("lineHeight")] public uint? LineHeight { get; set; }
    [JsonPropertyName("glyphs"), JsonConverter(typeof(OneLineGlyphs))] public List<FontGlyphJson>? Glyphs { get; set; }
}

public sealed class FontGlyphJson
{
    [JsonPropertyName("char")] public ushort Char { get; set; }
    [JsonPropertyName("x")] public ushort X { get; set; }
    [JsonPropertyName("y")] public ushort Y { get; set; }
    [JsonPropertyName("width")] public ushort Width { get; set; }
    [JsonPropertyName("height")] public ushort Height { get; set; }
    [JsonPropertyName("shift")] public short Shift { get; set; }
    [JsonPropertyName("offset")] public short Offset { get; set; }
    [JsonPropertyName("kerning")] public List<short[]>? Kerning { get; set; }
}

public sealed class OneLineGlyphs : JsonConverter<List<FontGlyphJson>>
{
    private static readonly JsonSerializerOptions Compact = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public override List<FontGlyphJson>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<List<FontGlyphJson>>(ref reader, options);

    public override void Write(Utf8JsonWriter writer, List<FontGlyphJson> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        string indent = OneLineRows.LineStart(writer);
        foreach (FontGlyphJson glyph in value)
            writer.WriteRawValue(indent + JsonSerializer.Serialize(glyph, Compact).Replace(",", ", ").Replace(":", ": "), true);
        writer.WriteEndArray();
    }
}
