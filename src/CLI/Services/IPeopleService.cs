namespace CLI.Services;

internal interface IPeopleService
{
    /// <summary>Lists up to <paramref name="limit"/> people ordered by id; throws <see cref="PeopleServiceException"/> if the database can't be read.</summary>
    Task<IReadOnlyList<PersonSummary>> ListAsync(int limit, CancellationToken ct);
}
