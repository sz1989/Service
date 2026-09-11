using Service.BAL.Chat;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class ChatController(
    ILogger<ChatController> logger,
    IChatService chatService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<string>> Ask([FromQuery] string question)
    {
        // url -> /Chat?question=Why is the sky blue?
        if (string.IsNullOrWhiteSpace(question))
        {
            return BadRequest("question is required.");
        }

        logger.LogInformation("Chat request: {Question}", question);
        var response = await chatService.AskAsync(question);

        return Ok(response);
    }
}
