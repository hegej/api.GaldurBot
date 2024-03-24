using Microsoft.AspNetCore.Mvc;
using api.GaldurBot;
using api.GaldurBot.Models;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly BotServices _botServices;

        public BotController(BotServices botServices)
        {
            _botServices = botServices;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.userInput))
            {
                return BadRequest("Invalid input");
            }

            try
            {
                var response = await _botServices.ChatWithOpenAIAsync(request.sessionId, request.userInput, request.username);
                return Ok(new { Message = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
    
