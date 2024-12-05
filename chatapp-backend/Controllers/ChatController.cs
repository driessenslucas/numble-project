using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ChatApp.Models;
using ChatApp.Services;
using System.Linq;
using System.Collections.Generic;
using Internal;

namespace ChatApp.Controllers
{
    
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

        private IActionResult ValidateUserAuthentication(string requestUserId)
        {
            // Retrieve the list of authenticated users
            var authenticatedUsers = HttpContext.Items["UserIds"] as List<string>;

            // Validate the requested user ID
            if (authenticatedUsers != null && authenticatedUsers.Contains(requestUserId))
            {
                Console.WriteLine($"User {requestUserId} is authenticated.");
                return Ok();
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try 
            {
                ValidateUserAuthentication(request.UserId);

                var response = string.IsNullOrEmpty(request.SessionId)
                    ? await _openAIService.GetChatResponseAsync(request.UserMessage)
                    : await _openAIService.GetChatWithHistoryResponseAsync(
                        request.UserMessage, 
                        await _cosmosDbService.GetSessionAsync(request.UserId, request.SessionId)
                    );

                var chatSession = await CreateOrUpdateChatSession(request, response);
                await _cosmosDbService.SaveSessionAsync(chatSession);

                return Ok(new { response, sessionId = chatSession.sessionId });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in Chat endpoint: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        private async Task<ChatSession> CreateOrUpdateChatSession(ChatRequest request, string response)
        {
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                var session = await _cosmosDbService.GetSessionAsync(request.UserId, request.SessionId);
                if (session == null)
                    throw new Exception("Session not found.");

                session.Messages.Add(CreateChatMessage(request.UserMessage, true));
                session.Messages.Add(CreateChatMessage(response, false));
                return session;
            }

            return new ChatSession
            {
                sessionId = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                SessionName = "Default Session",
                Messages = new List<ChatMessage>
                {
                    CreateChatMessage(request.UserMessage, true),
                    CreateChatMessage(response, false)
                },
                LastUpdated = DateTime.UtcNow.ToString("o")
            };
        }

        private ChatMessage CreateChatMessage(string text, bool isUserMessage)
        {
            return new ChatMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                Text = text,
                IsUserMessage = isUserMessage,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required.");

            ValidateUserAuthentication(userId);

            try 
            {
                var sessions = await _cosmosDbService.GetSessionsForUserAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving chat history: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving chat history.");
            }
        }

        [HttpGet("sessions/{userId}/{sessionId}")]
        public async Task<IActionResult> GetSession(string userId, string sessionId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
                return BadRequest("User ID and Session ID are required.");

            ValidateUserAuthentication(userId);

            try 
            {
                var session = await _cosmosDbService.GetSessionAsync(userId, sessionId);
                return session == null 
                    ? NotFound("Session not found.")
                    : Ok(session);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving session: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the session.");
            }
        }

        [HttpDelete("sessions/{userId}/{sessionId}")]
        public async Task<IActionResult> DeleteSession(string userId, string sessionId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
                return BadRequest("User ID and Session ID are required.");

            ValidateUserAuthentication(userId);

            try 
            {
                await _cosmosDbService.DeleteSessionAsync(userId, sessionId);
                return Ok("Session deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting session: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the session.");
            }
        }


    }
}
     