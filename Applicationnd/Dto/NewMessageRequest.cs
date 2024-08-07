namespace Applicationnd.Dto;

public class NewMessageRequest
{
    public string chat_id { get; set; }
    public string message_id { get; set; }
    public string role { get; set; }
    public string message { get; set; }
}