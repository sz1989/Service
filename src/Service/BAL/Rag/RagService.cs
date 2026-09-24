using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;

namespace Service.BAL.Rag;

public class RagService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IChatClient chatClient,
    NpgsqlDataSource dataSource) : IRagService
{
    private const int TopK = 5;

    public async Task<string> Ask(string question)
    {
        var embeddings = await embeddingGenerator.GenerateAsync([question]);
        var queryVector = new Vector(embeddings.First().Vector.ToArray());

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        // <=> is pgvector's cosine distance operator; smaller means more similar.
        command.CommandText = """
            SELECT text
            FROM document_embeddings
            ORDER BY embedding <=> @embedding
            LIMIT @topK
            """;

        command.Parameters.AddWithValue("embedding", queryVector);
        command.Parameters.AddWithValue("topK", TopK);

        var context = new List<string>();

        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                context.Add(reader.GetString(0));
            }
        }

        var prompt = $"""
            Answer the question using only the context below. If the context doesn't
            contain the answer, say so.

            Context:
            {string.Join("\n\n", context)}

            Question: {question}
            """;

        var response = await chatClient.GetResponseAsync(prompt);

        return response.Text;
    }
}
