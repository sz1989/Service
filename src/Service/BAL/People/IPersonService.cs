using Service.Model;

namespace Service.BAL.People;

public interface IPersonService
{
    Task<IEnumerable<Person>> GetPersonByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Person>> GetAllPersonsAsync(CancellationToken cancellationToken = default);
    Task QueuePersonRefreshAsync(int id);
}
