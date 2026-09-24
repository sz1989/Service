using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;

namespace Service.BAL.Embeddings;

public class EmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    NpgsqlDataSource dataSource) : IEmbeddingService
{
    public async Task IngestAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();

        // Ollama embeds the whole batch in one call; order of the result matches the input order.
        var embeddings = await embeddingGenerator.GenerateAsync(textList, cancellationToken: cancellationToken);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        foreach (var (text, embedding) in textList.Zip(embeddings))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO document_embeddings (id, text, embedding)
                VALUES (@id, @text, @embedding)
                """;

            command.Parameters.AddWithValue("id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("text", text);
            command.Parameters.AddWithValue("embedding", new Vector(embedding.Vector.ToArray()));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<DocumentMatch>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var embeddings = await embeddingGenerator.GenerateAsync([query], cancellationToken: cancellationToken);
        var queryVector = new Vector(embeddings.First().Vector.ToArray());

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        // <=> is pgvector's cosine distance operator; smaller means more similar.
        command.CommandText = """
            SELECT id, text, embedding <=> @embedding AS distance
            FROM document_embeddings
            ORDER BY distance
            LIMIT @topK
            """;

        command.Parameters.AddWithValue("embedding", queryVector);
        command.Parameters.AddWithValue("topK", topK);

        var matches = new List<DocumentMatch>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            matches.Add(new DocumentMatch(reader.GetString(0), reader.GetString(1), reader.GetDouble(2)));
        }

        return matches;
    }
}
