using Service.BAL.Embeddings;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class DocumentsController(
    ILogger<DocumentsController> logger,
    IEmbeddingService embeddingService) : ControllerBase
{
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
