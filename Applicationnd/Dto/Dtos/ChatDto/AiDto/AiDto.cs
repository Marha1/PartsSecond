namespace Application.Dtos.ChatDto.AiDto;

public class MessagesResponse
{
    public string Object { get; set; } // будет содержать "list"
    public List<ChatDto.Message> Data { get; set; } // список сообщений
    public string FirstId { get; set; } // ID первого сообщения в списке
    public string LastId { get; set; } // ID последнего сообщения в списке
    public bool HasMore { get; set; } // есть ли больше сообщений
}

public class Message
{
    public string Id { get; set; } // уникальный идентификатор сообщения
    public string Object { get; set; } // тип объекта (например, "thread.message")
    public int CreatedAt { get; set; } // время создания сообщения
    public string AssistantId { get; set; } // идентификатор ассистента
    public string ThreadId { get; set; } // идентификатор потока
    public string RunId { get; set; } // идентификатор выполнения
    public string Role { get; set; } // роль отправителя (например, "assistant" или "user")
    public List<Content> Content { get; set; } // содержимое сообщения
    public List<string> Attachments { get; set; } // файлы, прикреплённые к сообщению
    public Dictionary<string, string> Metadata { get; set; } // дополнительные метаданные
}

public class Content
{
    public string Type { get; set; } // тип содержимого (например, "text")
    public Text Text { get; set; } // текстовое содержимое
}

public class Text
{
    public string Value { get; set; } // само сообщение
    public List<string> Annotations { get; set; } // аннотации, если есть
}