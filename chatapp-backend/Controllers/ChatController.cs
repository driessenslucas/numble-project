using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ChatApp.Models;
using ChatApp.Services;

namespace ChatApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;

        public ChatController(IOpenAIService openAIService)
        {
            _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
        }

        [HttpPost("chat")]
        public async Task<IActionResult> GetChatResponse([FromBody] ChatRequest request)
        {
            // Log the incoming request
            Console.WriteLine("Received chat request");

            if (request == null || string.IsNullOrEmpty(request.UserMessage))
            {
                return BadRequest("User message is required.");
            }

            try
            {
                // Log before calling the service
                Console.WriteLine($"Processing message: {request.UserMessage}");

                var response = await _openAIService.GetChatResponseAsync(request.UserMessage);

                // Log successful response
                Console.WriteLine("Successfully processed chat request");

                return Ok(new { response });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error occurred while processing chat request: {ex.Message}");

                // Return generic 500 error without exposing the detailed exception
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}