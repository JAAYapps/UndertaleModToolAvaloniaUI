namespace UndertaleModToolAvalonia.Messages
{
    /// <summary>
    /// Asks the editor to run "Find all references" on an asset.
    /// </summary>
    public class FindReferencesMessage
    {
        public object Asset { get; }

        public FindReferencesMessage(object asset)
        {
            Asset = asset;
        }
    }
}
