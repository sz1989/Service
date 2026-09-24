using Service.BAL.Chat;
using Service.BAL.Embeddings;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class ChatController(
    ILogger<ChatController> logger,
    IChatService chatService,
    IEmbeddingService embeddingService) : ControllerBase
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

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<DocumentMatch>>> Search(
        [FromQuery] string query,
        [FromQuery] int topK    ,
        CancellationToken cancellationToken)
    {
        // url -> /Chat/search?query=How does dependency injection work?&topK=5
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("query is required.");
        }

        var effectiveTopK = topK <= 0 ? 5 : topK;

        logger.LogInformation("Chat search: {Query} (topK={TopK})", query, effectiveTopK);
        var matches = await embeddingService.SearchAsync(query, effectiveTopK, cancellationToken);

        return Ok(matches);
    }
}
