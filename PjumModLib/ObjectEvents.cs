using UndertaleModLib;
using UndertaleModLib.Models;

namespace PjumModLib;

public static class ObjectEvents
{
    // gml_Object_<obj>_<type>_<subtype>, the name the code/ importer looks up.
    // GM 2.3+ names a collision event after the other object rather than its index.
    public static string DefaultCodeName(UndertaleData data, string obj, EventType type, uint subtype)
    {
        if (type == EventType.Collision && data.IsVersionAtLeast(2, 3)
            && subtype < data.GameObjects.Count
            && data.GameObjects[(int)subtype]?.Name?.Content is string other)
            return $"gml_Object_{obj}_Collision_{other}";

        return $"gml_Object_{obj}_{type}_{subtype}";
    }
}
