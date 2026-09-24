using Service.BAL.Chat;
using Service.BAL.Embeddings;
using Service.BAL.Rag;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class ChatController(
    ILogger<ChatController> logger,
    IChatService chatService,
    IRagService ragService,
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

    [HttpGet("rag")]
    public async Task<ActionResult<string>> AskRag([FromQuery] string question)
    {
        // url -> /Chat/rag?question=How does dependency injection work?
        if (string.IsNullOrWhiteSpace(question))
        {
            return BadRequest("question is required.");
        }

        logger.LogInformation("Chat RAG request: {Question}", question);
        var response = await ragService.Ask(question);

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

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] string[] texts, CancellationToken cancellationToken)
    {
        if (texts is null || texts.Length == 0)
        {
            return BadRequest("no text");
        }

        logger.LogInformation("Ingesting {Count} document(s) into document_embeddings", texts.Length);
        await embeddingService.IngestAsync(texts, cancellationToken);

        return Ok();
    }
}
