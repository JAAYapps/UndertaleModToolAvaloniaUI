namespace UndertaleModToolAvalonia.Models
{
    public record TextInputParameters(string Title, string Label, string DefaultText, bool IsMultiline = false, string SubmitText = "OK", string CancelText = "Cancel");
}
