using CommunityToolkit.Mvvm.Messaging.Messages;

namespace UndertaleModToolAvalonia.Messages
{
    public class SelectedCodeTextMessage : RequestMessage<(string? Text, bool InAssembly)>;
}