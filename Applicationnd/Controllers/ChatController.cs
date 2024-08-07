using Applicationnd.Dto;
using Applicationnd.Service.Implementations;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Applicationnd.Service.Interfaces;

namespace Applicationnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly RedisService _redisService;
        private readonly IChatService _openAiService;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Общая блокировка для управления потоками

        public ChatController(RedisService redisService, IChatService openAiService)
        {
            _redisService = redisService;
            _openAiService = openAiService;
        }

        [HttpPost("new-message")]
        public async Task<IActionResult> PostNewMessage([FromBody] NewMessageRequest request)
        {
            try
            {
                // Записываем сообщение в Redis
                await _redisService.SetMessagesAsync(request.chat_id, new List<string> { request.message });

                // Проверяем существует ли поток
                var thread = await _openAiService.CreateThread(request.chat_id, request.message);
                    if (string.IsNullOrEmpty(thread))
                    {
                        var errorResponse = new { code = 500, isTTM = true };
                        return StatusCode(500, errorResponse);
                    }
                    
                return Ok(new { isTTM = 0 });
            }
            catch (Exception e)
            {
                var errorResponse = new { code = 500, isTTM = 1, message = e.Message }; // Добавляем сообщение об ошибке
                return StatusCode(500, errorResponse);
            }
        }

        [HttpPost("generate-message")]
        public async Task<IActionResult> GenerateMessage([FromBody] GenerateMessageRequest request)
        {
            await _semaphore.WaitAsync(); // Блокируем процесс

            try
            {
                // Получаем поток для текущего чата
                var threadId = await _redisService.GetThreadIdAsync(request.chat_id);
                if (threadId == null)
                {
                    return NotFound(new { isTTM = 1, message = "Поток не найден" });
                }

                // Получаем последнее сообщение
                var messageList = await _redisService.GetMessagesAsync(request.chat_id);
                if (messageList.Count == 0)
                {
                    return BadRequest(new { isTTM = 1, message = "Нет сообщений для генерации" });
                }

                var lastMessage = messageList.First(); // Последнее сообщение пользователя

                var generatedResponse = await _openAiService.GenerateResponseAsync(lastMessage,request.chat_id);

                string qtid = $"{request.chat_id}:{request.message_id}";
                await _redisService.SetResponce(qtid,generatedResponse);
                return Ok(new { qtid, isTTM = 0});
            }
            finally
            {
                _semaphore.Release(); // Освобождаем блокировку
            }
        }

        [HttpPost("get-answer")]
        public async Task<IActionResult> GetAnswer([FromBody] GetAnswerRequest request)
        {
            await _semaphore.WaitAsync(); // Блокируем процесс

            try
            {
                // Получаем сообщение из Redis по qtid
                var responseMessage = await _redisService.GetResponce(request.qtid);
                if (string.IsNullOrEmpty( responseMessage))
                {
                    return StatusCode(204, new { isTTM = 1 }); // Ответ еще не готов
                }

                return Ok(new { message = responseMessage, isTTM = 0 });
            }
            finally
            {
                _semaphore.Release(); // Освобождаем блокировку
            }
        }
    }
}