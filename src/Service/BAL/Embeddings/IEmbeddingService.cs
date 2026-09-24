namespace Service.BAL.Embeddings;

public interface IEmbeddingService
{
    Task IngestAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentMatch>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}

public record DocumentMatch(string Id, string Text, double Distance);
