using Avalonia.Input;
using UndertaleModLib;

namespace UndertaleModToolAvalonia.Utilities
{
    /// <summary>
    /// Drag data for dragging an asset from the tree onto a reference box.
    /// Only works inside the app.
    /// </summary>
    public static class ResourceDragDrop
    {
        public static readonly DataFormat<UndertaleObject> Format =
            DataFormat.CreateInProcessFormat<UndertaleObject>(AppConstants.UndertaleObjectFormat);

        public static DataTransfer CreateTransfer(UndertaleObject resource)
        {
            var transfer = new DataTransfer();
            transfer.Add(DataTransferItem.Create(Format, resource));
            return transfer;
        }

        /// <summary>Gets the dragged asset, or null if it's not one.</summary>
        public static UndertaleObject? GetResource(DragEventArgs e)
        {
            return e.DataTransfer.TryGetValue(Format);
        }
    }
}
