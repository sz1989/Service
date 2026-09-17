namespace Service.BAL.Embeddings;

public interface IEmbeddingService
{
    Task IngestAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
