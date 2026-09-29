using Service.BAL.Chat;
using Service.BAL.Embeddings;
using Service.BAL.Rag;
using Service.Services;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class ChatController(
    ILogger<ChatController> logger,
    IChatService chatService,
    IRagService ragService,
    IEmbeddingService embeddingService,
    IChatRequestQueue chatRequestQueue,
    IChatRequestRegistry chatRequestRegistry) : ControllerBase
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

    [HttpPost("ask-async")]
    public async Task<IActionResult> AskAsync([FromBody] ChatAskRequest request)
    {
        // Fire-and-notify: the caller connects to /hubs/chat first to get a connectionId, then
        // posts here. We queue the question and return immediately instead of blocking on the
        // local LLM; ChatBackgroundService pushes "chatAnswer"/"chatError" to that connection
        // once ChatService.AskAsync finishes.
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("question is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ConnectionId))
        {
            return BadRequest("connectionId is required.");
        }

        var requestId = Guid.NewGuid();
        logger.LogInformation("Queuing async chat request {RequestId}: {Question}", requestId, request.Question);

        chatRequestRegistry.Register(requestId);
        await chatRequestQueue.QueueAsync(new ChatAskWorkItem(requestId, request.Question, request.ConnectionId));

        return Accepted(new { requestId });
    }

    [HttpPost("ask-async/{requestId:guid}/cancel")]
    public IActionResult CancelAsk(Guid requestId)
    {
        // Cancels a still-queued or in-progress AskAsync started via POST /Chat/ask-async.
        // Not found once the request has already finished (answered, errored, or cancelled).
        logger.LogInformation("Cancelling chat request {RequestId}", requestId);

        return chatRequestRegistry.TryCancel(requestId) ? NoContent() : NotFound();
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
