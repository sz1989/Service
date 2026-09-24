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

    // Cosine distance from pgvector's <=> operator ranges 0 (identical) to 2 (opposite).
    // Above this, the nearest match is still too unrelated to the question to be useful
    // context, so we skip the LLM call rather than let it hallucinate off a bad match.
    private const double RelevanceThreshold = 0.6;

    public async Task<string> Ask(string question)
    {
        var embeddings = await embeddingGenerator.GenerateAsync([question]);
        var queryVector = new Vector(embeddings.First().Vector.ToArray());

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        // <=> is pgvector's cosine distance operator; smaller means more similar.
        command.CommandText = """
            SELECT text, embedding <=> @embedding AS distance
            FROM document_embeddings
            ORDER BY distance
            LIMIT @topK
            """;

        command.Parameters.AddWithValue("embedding", queryVector);
        command.Parameters.AddWithValue("topK", TopK);

        var context = new List<string>();
        var bestDistance = double.MaxValue;

        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                context.Add(reader.GetString(0));
                bestDistance = Math.Min(bestDistance, reader.GetDouble(1));
            }
        }

        if (bestDistance > RelevanceThreshold)
        {
            return "I don't have any relevant documents to answer that question.";
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
