namespace Application.Dtos.ChatDto;

public class Tool
{
    public string Type { get; set; }
    public List<Content> Content { get; set; }  // Add Content property to the Tool class
}