using Service.Model;

namespace Service.Data;

public interface IPersonRepository : IGenericRepository<Person>
{
    Task<IEnumerable<Person>> GetPersonByIdAsync(int id);
}

public class PersonRepository : GenericRepository<Person>, IPersonRepository
{
    public PersonRepository(AppDbContext context) : base(context) { }
    
    public async Task<IEnumerable<Person>> GetPersonByIdAsync(int id)
    {
        return await _dbSet.Where(p => p.Id == id).ToListAsync();
    }
}
