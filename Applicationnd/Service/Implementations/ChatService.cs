using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Application.Dtos.ChatDto;
using Applicationnd.Dto;
using Applicationnd.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Applicationnd.Service.Implementations;

public class ChatService: IChatService
{
    private readonly string? _openAiApiKey;
    private readonly RedisService _redisService;
    private readonly string? _assistantkey;
    private readonly CancellationTokenSource _cancellationToken;
    
private const  string baseUrl = "https://api.openai.com/v1/";

    public ChatService(IConfiguration configuration,
        RedisService redisService)
    {
        _openAiApiKey = configuration["OpenAI:ApiKey"];
        _redisService = redisService;
        _assistantkey = configuration["OpenAi:assistkey"];
        _cancellationToken = new CancellationTokenSource();
    }



    public async Task<string> CreateThread(string requestChatId,string message)
    {
        var baseUrl = "https://api.openai.com/v1/";
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
        httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        string threadId = null;

        try
        {
            threadId = await _redisService.GetThreadIdAsync(requestChatId);

            if (string.IsNullOrEmpty(threadId))
            {
                var threadCreationRequestContent = new
                {
                    messages = new[] { new { role = "user", content = message } }
                };
                var threadCreationJsonRequest = JsonConvert.SerializeObject(threadCreationRequestContent);
                var threadCreationContent =
                    new StringContent(threadCreationJsonRequest, Encoding.UTF8, "application/json");
                var threadCreationResponse =
                    await httpClient.PostAsync($"{baseUrl}threads", threadCreationContent);
                threadCreationResponse.EnsureSuccessStatusCode();
                var threadCreationJsonResponse =
                    await threadCreationResponse.Content.ReadAsStringAsync();
                var threadCreationResponseObject =
                    JsonConvert.DeserializeObject<ThreadResponse>(threadCreationJsonResponse);
                threadId = threadCreationResponseObject?.Id ??
                           throw new InvalidOperationException(
                               "Не удалось получить идентификатор потока из ответа.");
                Console.WriteLine($"Поток успешно создан с идентификатором: {threadId}");
                await _redisService.SetThreadIdAsync(requestChatId, threadId);
            }
            else
            {
                var threadUpdateRequestContent = new
                {
                    metadata = new Dictionary<string, string>
                    {
                        { "modified", "true" },
                        { "user_message", message }
                    }
                };
                var threadUpdateJsonRequest = JsonConvert.SerializeObject(threadUpdateRequestContent);
                var threadUpdateContent =
                    new StringContent(threadUpdateJsonRequest, Encoding.UTF8, "application/json");
                var threadUpdateResponse = await httpClient.PostAsync($"{baseUrl}threads/{threadId}",
                    threadUpdateContent);
                threadUpdateResponse.EnsureSuccessStatusCode();
                var threadUpdateJsonResponse =
                    await threadUpdateResponse.Content.ReadAsStringAsync();
                var threadUpdateResponseObject =
                    JsonConvert.DeserializeObject<ThreadResponse>(threadUpdateJsonResponse);
                threadId = threadUpdateResponseObject?.Id ??
                           throw new InvalidOperationException(
                               "Не удалось получить идентификатор потока из ответа.");
                Console.WriteLine($"Поток успешно обновлен: {threadId}");
                await _redisService.SetThreadIdAsync(requestChatId, threadId);

            }

            return threadId;
        }
        catch (Exception ex)
        {
           return null;
        }
    }

    public async Task<string> GenerateResponseAsync(string lastMessage,string chatid)
    {
        try
        {
            string? threadId = await _redisService.GetThreadIdAsync(chatid);
            if (string.IsNullOrEmpty(threadId))
                throw new InvalidOperationException("Не удалось получить идентификатор потока.");
            var createRunRequestContent = new {
                assistant_id = _assistantkey,
                additional_messages = new[] { new { role = "user", content = lastMessage } },
            };
            var createRunJsonRequest = JsonConvert.SerializeObject(createRunRequestContent);
            using var httpClient = new HttpClient();
            var createRunContent = new StringContent(createRunJsonRequest, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Clear(); // Очищаем старые заголовки
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
            var createRunResponse = await httpClient.PostAsync($"{baseUrl}threads/{threadId}/runs", createRunContent);
            createRunResponse.EnsureSuccessStatusCode();
            var createRunJsonResponse = await createRunResponse.Content.ReadAsStringAsync();
            var createRunResponseObject = JsonConvert.DeserializeObject<RunResponse>(createRunJsonResponse);
            var runId = createRunResponseObject?.Id ?? throw new InvalidOperationException("Не удалось получить идентификатор выполнения из ответа.");
            Console.WriteLine($"Идентификатор выполнения успешно получен: {runId}");


            var runStatus =
                await WaitForRunCompletionAsync(httpClient, baseUrl, _assistantkey, threadId, runId)
                    .ConfigureAwait(false);
            var messages = await GetMessagesFromThreadAsync(chatid, threadId).ConfigureAwait(false);
            // Обработка сообщений
            return messages;
        }
        catch (Exception e)
        {
            return null;
        }
        
    }
    private async Task<RunResponse> WaitForRunCompletionAsync(HttpClient httpClient, string baseUrl, string assistantId,
        string threadId, string runId)
    {
        var statusUrl = $"{baseUrl}threads/{threadId}/runs/{runId}";
        RunResponse runResponseObject;

        while (true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            request.Headers.Add("OpenAI-Beta", "assistants=v1");

            var statusResponse = await httpClient.SendAsync(request).ConfigureAwait(false);

            if (statusResponse.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Error 404: Resource not found.");
                throw new HttpRequestException("Resource not found. Please check the URL or IDs.");
            }

            statusResponse.EnsureSuccessStatusCode();

            var statusJsonResponse =
                await statusResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            runResponseObject = JsonConvert.DeserializeObject<RunResponse>(statusJsonResponse);

            if (runResponseObject.Status == "completed")
                break;

            await Task.Delay(2000).ConfigureAwait(false);
        }

        return runResponseObject;
    }
    public async Task<string> GetMessagesFromThreadAsync(string chatid, string threadId) {
        var baseUrl = "https://api.openai.com/v1/";
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
        httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

        try {
            var url = $"{baseUrl}threads/{threadId}/messages";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonResponse);

            var rootObject = JsonConvert.DeserializeObject<RootObject>(jsonResponse);
            if (rootObject != null && rootObject.Data != null) {
                var lastAssistantMessage = rootObject.Data
                    .Where(m => m.Role == "assistant")
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                if (lastAssistantMessage != null && lastAssistantMessage.Content != null) {
                    var newLastMessageId = lastAssistantMessage.Id;
                    var lastMessageId = await _redisService.GetLastMessageIdAsync(chatid);

                    if (newLastMessageId != lastMessageId) {
                        await _redisService.SetLastMessageIdAsync(chatid, newLastMessageId);
                        var messageContent = lastAssistantMessage.Content
                            .FirstOrDefault(c => c.Text != null && !string.IsNullOrEmpty(c.Text.Value))?.Text.Value;
                        return messageContent;
                    }
                }
            }
            return null;
        } catch (Exception ex) {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
            throw;
        }
    }
}
