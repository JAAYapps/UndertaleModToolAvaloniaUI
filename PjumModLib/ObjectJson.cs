using System.Text.Json;
using System.Text.Json.Serialization;

namespace PjumModLib;

public sealed class ObjectJson
{
    [JsonPropertyName("sprite")]     public string? Sprite { get; set; }
    [JsonPropertyName("parent")]     public string? Parent { get; set; }
    [JsonPropertyName("maskSprite")] public string? MaskSprite { get; set; }

    [JsonPropertyName("visible")]    public bool? Visible { get; set; }
    [JsonPropertyName("solid")]      public bool? Solid { get; set; }
    [JsonPropertyName("persistent")] public bool? Persistent { get; set; }
    [JsonPropertyName("depth")]      public int? Depth { get; set; }

    [JsonPropertyName("events")]     public List<ObjectEventJson> Events { get; set; } = new();
    [JsonPropertyName("physics")]    public PhysicsJson? Physics { get; set; }
}

public sealed class ObjectEventJson
{
    [JsonPropertyName("type")]    public string? Type { get; set; }     // "Create", "Step", "Collision", ...
    [JsonPropertyName("subtype")] public JsonElement Subtype { get; set; } // omitted = 0; a number, or a name ("EndStep", "obj_other")
    [JsonPropertyName("code")]    public string? Code { get; set; }     // code entry to run; default gml_Object_<obj>_<type>_<subtype>
}

public sealed class PhysicsJson
{
    [JsonPropertyName("uses")]           public bool? Uses { get; set; }
    [JsonPropertyName("sensor")]         public bool? Sensor { get; set; }
    [JsonPropertyName("shape")]          public string? Shape { get; set; }   // "Circle" | "Box" | "Custom"
    [JsonPropertyName("density")]        public float? Density { get; set; }
    [JsonPropertyName("restitution")]    public float? Restitution { get; set; }
    [JsonPropertyName("group")]          public uint? Group { get; set; }
    [JsonPropertyName("linearDamping")]  public float? LinearDamping { get; set; }
    [JsonPropertyName("angularDamping")] public float? AngularDamping { get; set; }
    [JsonPropertyName("friction")]       public float? Friction { get; set; }
    [JsonPropertyName("awake")]          public bool? Awake { get; set; }
    [JsonPropertyName("kinematic")]      public bool? Kinematic { get; set; }
}
