using CommunityToolkit.Mvvm.Messaging.Messages;

namespace UndertaleModToolAvalonia.Messages
{
    /// <summary>
    /// Asks every open code editor to compile what was typed into it.
    /// Each one answers <see langword="true"/> if its code is compiled (or it had no changes),
    /// and <see langword="false"/> if the code did not compile.
    /// </summary>
    public class SaveCodeChangesMessage : AsyncCollectionRequestMessage<bool>;
}
