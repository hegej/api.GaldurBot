using Microsoft.AspNetCore.Mvc;
using api.GaldurBot;

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
            if (request == null || string.IsNullOrWhiteSpace(request.UserInput))
            {
                return BadRequest("Invalid input");
            }

            try
            {
                var response = await _botServices.ChatWithOpenAIAsync(request.UserInput);
                return Ok(new { Message = response });
            }
            catch (System.Exception)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }

    public class ChatRequest
    {
        public string UserInput { get; set; }
    }
}
