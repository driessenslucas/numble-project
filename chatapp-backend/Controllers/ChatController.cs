using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ChatApp.Models;

namespace ChatApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;
        private readonly ICosmosDbService _cosmosDbService;

        public ChatController(IOpenAIService openAIService, ICosmosDbService cosmosDbService)
        {
            _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
            _cosmosDbService = cosmosDbService ?? throw new ArgumentNullException(nameof(cosmosDbService));
        }

        [HttpPost("chat")]
        public async Task<IActionResult> GetChatResponse([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.UserMessage))
            {
                return BadRequest("User message is required.");
            }

            try
            {
                var userId = User.Identity?.Name;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var response = await _openAIService.GetChatResponseAsync(request.UserMessage);

                var chatHistory = new ChatHistory
                {
                    UserId = userId,
                    Message = request.UserMessage
                };

                await _cosmosDbService.SaveChatAsync(chatHistory);

                return Ok(new { response });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory()
        {
            try
            {
                var userId = User.Identity?.Name;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var history = await _cosmosDbService.GetChatHistoryAsync(userId);

                return Ok(history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("save-session")]
        public async Task<IActionResult> SaveSession([FromBody] ChatSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.UserId))
            {
                return BadRequest("Session or UserId is required.");
            }

            try
            {
                await _cosmosDbService.SaveSessionAsync(session);
                return Ok("Session saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving session: {ex.Message}");
                return StatusCode(500, "An error occurred while saving the session.");
            }
        }

        [HttpGet("get-sessions")]
        public async Task<IActionResult> GetSessions()
        {
            try
            {
                var userId = User.Identity?.Name; // Assumes B2C provides a unique identifier

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var sessions = await _cosmosDbService.GetSessionsForUserAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving sessions: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving sessions.");
            }
        }

    }
}
