namespace Application.Dtos.ChatDto;

public class Message
{
    public string Role { get; set; }
    public List<Content> Content { get; set; }
}