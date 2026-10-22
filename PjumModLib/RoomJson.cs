using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib;
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
    [JsonPropertyName("removeLayers")] public List<string>? RemoveLayers { get; set; }
    [JsonPropertyName("layers")] public List<RoomLayerJson>? Layers { get; set; }
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
    [JsonPropertyName("enableViews")] public bool? EnableViews { get; set; }
    [JsonPropertyName("clearViewBackground")] public bool? ClearViewBackground { get; set; }
    [JsonPropertyName("doNotClearDisplayBuffer")] public bool? DoNotClearDisplayBuffer { get; set; }
    [JsonPropertyName("creationCode")] public string? CreationCode { get; set; }
    [JsonPropertyName("physicsWorld")] public bool? PhysicsWorld { get; set; }
    [JsonPropertyName("physicsTop")] public uint? PhysicsTop { get; set; }
    [JsonPropertyName("physicsLeft")] public uint? PhysicsLeft { get; set; }
    [JsonPropertyName("physicsRight")] public uint? PhysicsRight { get; set; }
    [JsonPropertyName("physicsBottom")] public uint? PhysicsBottom { get; set; }
    [JsonPropertyName("gravityX")] public float? GravityX { get; set; }
    [JsonPropertyName("gravityY")] public float? GravityY { get; set; }
    [JsonPropertyName("metersPerPixel")] public float? MetersPerPixel { get; set; }
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
    [JsonPropertyName("sprite")] public string? Sprite { get; set; }
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
    [JsonPropertyName("after")] public uint? After { get; set; }
}

public sealed class RoomLayerJson
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("new")] public bool? New { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("depth")] public int? Depth { get; set; }
    [JsonPropertyName("visible")] public bool? Visible { get; set; }
    [JsonPropertyName("x")] public float? X { get; set; }
    [JsonPropertyName("y")] public float? Y { get; set; }
    [JsonPropertyName("hspeed")] public float? HSpeed { get; set; }
    [JsonPropertyName("vspeed")] public float? VSpeed { get; set; }
    [JsonPropertyName("grid")] public RoomGridJson? Grid { get; set; }
    [JsonPropertyName("background")] public RoomLayerBackgroundJson? Background { get; set; }
    [JsonPropertyName("tiles")] public RoomTilesJson? Tiles { get; set; }
    [JsonPropertyName("sprites")] public RoomSpritesJson? Sprites { get; set; }
}

public sealed class RoomGridJson
{
    [JsonPropertyName("tileset")] public string? Tileset { get; set; }
    [JsonPropertyName("rows"), JsonConverter(typeof(OneLineRows))] public uint[][]? Rows { get; set; }
    [JsonPropertyName("cells"), JsonConverter(typeof(OneLineRows))] public uint[][]? Cells { get; set; }
}

public sealed class OneLineRows : JsonConverter<uint[][]>
{
    public override uint[][]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<uint[][]>(ref reader, options);

    public override void Write(Utf8JsonWriter writer, uint[][] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        string indent = writer.Options.Indented
            ? writer.Options.NewLine + new string(writer.Options.IndentCharacter, writer.CurrentDepth * writer.Options.IndentSize)
            : "";
        foreach (uint[]? row in value)
            writer.WriteRawValue(indent + (row is null ? "null" : "[" + string.Join(", ", row) + "]"), true);
        writer.WriteEndArray();
    }
}

public sealed class RoomLayerBackgroundJson
{
    [JsonPropertyName("sprite")] public string? Sprite { get; set; }
    [JsonPropertyName("visible")] public bool? Visible { get; set; }
    [JsonPropertyName("foreground")] public bool? Foreground { get; set; }
    [JsonPropertyName("tileX")] public bool? TileX { get; set; }
    [JsonPropertyName("tileY")] public bool? TileY { get; set; }
    [JsonPropertyName("stretch")] public bool? Stretch { get; set; }
    [JsonPropertyName("color")] public uint? Color { get; set; }
    [JsonPropertyName("firstFrame")] public float? FirstFrame { get; set; }
    [JsonPropertyName("animationSpeed")] public float? AnimationSpeed { get; set; }
    [JsonPropertyName("animationSpeedType")] public uint? AnimationSpeedType { get; set; }
}

public sealed class RoomSpritesJson
{
    [JsonPropertyName("add")] public List<RoomSpriteJson> Add { get; set; } = new();
    [JsonPropertyName("change")] public List<RoomSpriteJson> Change { get; set; } = new();
    [JsonPropertyName("remove")] public List<string> Remove { get; set; } = new();
}

public sealed class RoomSpriteJson
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("sprite")] public string? Sprite { get; set; }
    [JsonPropertyName("x")] public int? X { get; set; }
    [JsonPropertyName("y")] public int? Y { get; set; }
    [JsonPropertyName("scaleX")] public float? ScaleX { get; set; }
    [JsonPropertyName("scaleY")] public float? ScaleY { get; set; }
    [JsonPropertyName("color")] public uint? Color { get; set; }
    [JsonPropertyName("animationSpeed")] public float? AnimationSpeed { get; set; }
    [JsonPropertyName("animationSpeedType")] public uint? AnimationSpeedType { get; set; }
    [JsonPropertyName("frameIndex")] public float? FrameIndex { get; set; }
    [JsonPropertyName("rotation")] public float? Rotation { get; set; }
    [JsonPropertyName("after")] public string? After { get; set; }
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
    [JsonPropertyName("creationCode")] public string? CreationCode { get; set; }
    [JsonPropertyName("preCreateCode")] public string? PreCreateCode { get; set; }
    [JsonPropertyName("after")] public uint? After { get; set; }
}

public static class RoomLayers
{
    public static readonly UndertaleRoom.LayerType[] CarriedTypes =
        [UndertaleRoom.LayerType.Instances, UndertaleRoom.LayerType.Tiles, UndertaleRoom.LayerType.Background, UndertaleRoom.LayerType.Assets, UndertaleRoom.LayerType.Path];

    public static UndertaleRoom EmptyRoom(UndertaleData data, Func<string, UndertaleString> makeString)
    {
        var room = new UndertaleRoom();
        if (!data.IsGameMaker2())
        {
            room.Caption = makeString("");
            return room;
        }
        room.Backgrounds.Clear();
        room.Right = 0;
        room.Bottom = 0;
        if (data.IsVersionAtLeast(2024, 13))
        {
            room.Flags |= UndertaleRoom.RoomEntryFlags.IsGM2024_13;
            room.InstanceCreationOrderIDs ??= new();
        }
        else
        {
            room.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
            if (data.IsVersionAtLeast(2, 3))
                room.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2_3;
        }
        return room;
    }

    public static UndertaleRoom.Layer? Named(UndertaleRoom room, string name) =>
        (room.Layers ?? []).FirstOrDefault(l => l.LayerName?.Content == name);

    public static UndertaleRoom.Layer? LayerOf(UndertaleRoom room, UndertaleRoom.GameObject instance) =>
        InstanceLayers(room).FirstOrDefault(l => l.InstancesData.Instances.Contains(instance));

    public static IEnumerable<UndertaleRoom.Layer> InstanceLayers(UndertaleRoom room) =>
        (room.Layers ?? []).Where(l => l.LayerType == UndertaleRoom.LayerType.Instances && l.InstancesData is not null);
}
