using Microsoft.AspNetCore.Mvc;
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

        // Inject the OpenAI service into the controller
        public ChatController(IOpenAIService openAIService)
        {
            _openAIService = openAIService;
        }

        // POST endpoint to send a chat request to OpenAI
        [HttpPost("chat")]
        public async Task<IActionResult> GetChatResponse([FromBody] ChatRequest request)
        {
            // Ensure the request is not null or empty
            if (request == null || string.IsNullOrEmpty(request.UserMessage))
            {
                return BadRequest("User message is required.");
            }

            try
            {
                // Pass the user's message to the OpenAI service and get the response
                var response = await _openAIService.GetChatResponseAsync(request.UserMessage);

                // Return the response from OpenAI
                return Ok(new { response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
