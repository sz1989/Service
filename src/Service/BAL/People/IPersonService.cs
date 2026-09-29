using Service.Model;

namespace Service.BAL.People;

public interface IPersonService
{
    Task<IEnumerable<Person>> GetPersonByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Person>> GetAllPersonsAsync(CancellationToken ct = default);

    Task<Person> CreateANewAsync(Person newPerson);

    Task QueuePersonRefreshAsync(int id);

    /// <summary>Returns the updated person, or null if no person with that id exists.</summary>
    Task<Person?> UpdatePersonAsync(Person updatePerson, CancellationToken ct = default);

    /// <summary>Returns true if a person with that id existed and was deleted.</summary>
    Task<bool> DeletePersonAsync(int id, CancellationToken ct = default);
}
