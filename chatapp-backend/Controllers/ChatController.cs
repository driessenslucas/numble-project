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

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest("User ID is required.");
                }

                Console.WriteLine($"Processing chat request for user: {request.UserId}");
                Console.WriteLine($"User message: {request.UserMessage}");

                var response = await _openAIService.GetChatResponseAsync(request.UserMessage);
                Console.WriteLine($"OpenAI Response: {response}");

                ChatSession chatSession;

                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    // Retrieve existing session
                    chatSession = await _cosmosDbService.GetSessionAsync(request.UserId, request.SessionId);
                    if (chatSession == null)
                    {
                        return NotFound("Session not found.");
                    }

                    // Append new message to existing session
                    chatSession.Messages.Add(new ChatMessage
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        Text = request.UserMessage,
                        IsUserMessage = true,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    });

                    chatSession.Messages.Add(new ChatMessage
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        Text = response,
                        IsUserMessage = false,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    });
                }
                else
                {
                    // Create new session
                    chatSession = new ChatSession
                    {
                        sessionId = Guid.NewGuid().ToString(),
                        UserId = request.UserId,
                        SessionName = "Default Session",
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage
                            {
                                MessageId = Guid.NewGuid().ToString(),
                                Text = request.UserMessage,
                                IsUserMessage = true,
                                Timestamp = DateTime.UtcNow.ToString("o")
                            },
                            new ChatMessage
                            {
                                MessageId = Guid.NewGuid().ToString(),
                                Text = response,
                                IsUserMessage = false,
                                Timestamp = DateTime.UtcNow.ToString("o")
                            }
                        },
                        LastUpdated = DateTime.UtcNow.ToString("o")
                    };
                }

                Console.WriteLine("Created/Updated ChatSession object:");
                Console.WriteLine($"  SessionId: {chatSession.sessionId}");
                Console.WriteLine($"  UserId: {chatSession.UserId}");
                Console.WriteLine($"  Messages Count: {chatSession.Messages.Count}");
                Console.WriteLine($"  LastUpdated: {chatSession.LastUpdated}");

                try
                {
                    await _cosmosDbService.SaveSessionAsync(chatSession);
                    Console.WriteLine("Successfully saved session to Cosmos DB");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to save to Cosmos DB: {ex.GetType().Name} - {ex.Message}");
                    Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }

                return Ok(new { response, sessionId = chatSession.sessionId });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in Chat endpoint: {ex.GetType().Name} - {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory([FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required.");
                }

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
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest("User ID and Session ID are required.");
                }

                var session = await _cosmosDbService.GetSessionAsync(userId, sessionId);
                if (session == null)
                {
                    return NotFound("Session not found.");
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving session: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the session.");
            }
        }
    }
}
