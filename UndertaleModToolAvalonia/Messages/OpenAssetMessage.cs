namespace UndertaleModToolAvalonia.Messages
{
    /// <summary>
    /// Tells the editor to open an asset in a tab. The reference boxes use this
    /// since they don't have their own view model.
    /// </summary>
    public class OpenAssetMessage
    {
        public object Asset { get; }
        public bool InNewTab { get; }

        public OpenAssetMessage(object asset, bool inNewTab)
        {
            Asset = asset;
            InNewTab = inNewTab;
        }
    }
}
