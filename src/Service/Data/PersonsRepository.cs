using Service.Model;

namespace Service.Data;

public interface IPersonRepository : IGenericRepository<Person>
{
    Task<IEnumerable<Person>> GetPersonByIdAsync(int id);
}

public class PersonRepository(AppDbContext context) : GenericRepository<Person>(context), IPersonRepository
{
    // Ordered in SQL so the list has a stable order: without ORDER BY, Postgres returns rows in
    // physical order, which changes after an UPDATE.
    public override async Task<IEnumerable<Person>> GetAllAsync() =>
        await _dbSet.OrderBy(p => p.Id).ToListAsync();

    public async Task<IEnumerable<Person>> GetPersonByIdAsync(int id)
    {
        return await _dbSet.Where(p => p.Id == id).ToListAsync();
    }
}
