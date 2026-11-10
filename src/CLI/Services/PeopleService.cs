namespace CLI.Services;

internal sealed class PeopleService(NpgsqlDataSource dataSource, ILogger<PeopleService> logger) : IPeopleService
{
    private const string _sql = "SELECT id, name, date_of_birth, salary FROM person ORDER BY id LIMIT @limit";

    public async Task<IReadOnlyList<PersonSummary>> ListAsync(int limit, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            await using var command = dataSource.CreateCommand(_sql);
            command.Parameters.AddWithValue("limit", limit);
            await using var reader = await command.ExecuteReaderAsync(ct);

            var people = new List<PersonSummary>();
            while (await reader.ReadAsync(ct))
            {
                people.Add(new PersonSummary(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetFieldValue<DateOnly>(2),
                    reader.GetDecimal(3)));
            }

            logger.LogInformation("Read {Count} people", people.Count);
            return people;
        }
        catch (NpgsqlException ex)
        {
            throw new PeopleServiceException("Could not read people from the database.", ex);
        }
    }
}
