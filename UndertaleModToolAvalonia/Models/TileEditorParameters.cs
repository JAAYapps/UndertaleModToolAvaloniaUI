using Avalonia.Media.Imaging;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Models
{
    public record TileEditorParameters(Layer Layer, RenderTargetBitmap? RoomPreview);
}
