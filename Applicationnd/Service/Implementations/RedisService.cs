using StackExchange.Redis;

public class RedisService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<string> GetThreadIdAsync(string chatId)
    {
        var db = _redis.GetDatabase();
        return await db.StringGetAsync($"thread_id:{chatId}");
    }

    public async Task SetThreadIdAsync(string chatId , string threadId)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"thread_id:{chatId}", threadId);
    }
    public async Task SetMessagesAsync(string chatId, List<string> messages)
    {
        var db = _redis.GetDatabase();
        var messagesKey = $"messages:{chatId}";
        // Здесь можно также использовать List или другую структуру данных по желанию
        await db.SetAddAsync(messagesKey, messages.Select(m => (RedisValue)m).ToArray());
    }

    public async Task<List<string>> GetMessagesAsync(string chatId)
    {
        var db = _redis.GetDatabase();
        var messagesKey = $"messages:{chatId}";
        var redisValues = await db.SetMembersAsync(messagesKey);
        return redisValues.Select(rv => rv.ToString()).ToList();
    }
    public async Task SetLastMessageIdAsync(string chatid, string messageId)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"last_message_id:{chatid}", messageId);
    }
    

    public async Task<string> GetLastMessageIdAsync(string chatid)
    {
        var db = _redis.GetDatabase();
        return await db.StringGetAsync($"last_message_id:{chatid}");
    }

    public async Task SetResponce(string qtid, string generatedResponse)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"Responce_qtid:{qtid}", generatedResponse);
    }
    public async Task<string> GetResponce(string qtid)
    {
        var db = _redis.GetDatabase();
        return await db.StringGetAsync($"Responce_qtid:{qtid}");
    }
}