public class RootObject
{
    public string Object { get; set; }
    public List<MessageResponse> Data { get; set; }
}

public class MessageResponse
{
    public string Id { get; set; }
    public string Object { get; set; }
    public int CreatedAt { get; set; }
    public string AssistantId { get; set; }
    public string ThreadId { get; set; }
    public string RunId { get; set; }
    public string Role { get; set; }
    public List<Content> Content { get; set; }
    public List<string> FileIds { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class Content
{
    public string Type { get; set; }
    public Text Text { get; set; }
}

public class Text
{
    public string Value { get; set; }
    public List<Annotation> Annotations { get; set; }
}

public class Annotation
{
    public string Type { get; set; }
    public string Text { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public FileCitation FileCitation { get; set; }
}

public class FileCitation
{
    public string FileId { get; set; }
    public string Quote { get; set; }
}