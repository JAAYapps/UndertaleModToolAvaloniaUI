namespace UndertaleModToolAvalonia.Models.EditorModels;

public class Description
{
    public string Heading { get; private set; }
    public string Message { get; private set; }

    public Description(string heading, string description)
    {
        Heading = heading;
        Message = description;
    }
}