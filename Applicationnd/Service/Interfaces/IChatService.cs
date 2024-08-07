namespace Applicationnd.Service.Interfaces;

public interface IChatService
{
      Task<string> CreateThread(string requestChatId, string message);
      Task<string> GenerateResponseAsync(string lastMessage, string chatid);
}