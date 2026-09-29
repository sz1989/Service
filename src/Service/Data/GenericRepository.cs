namespace Service.Data;

using Microsoft.EntityFrameworkCore;
using Service.Data;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    // EF Core tracks changes automatically; Update marks the state as Modified
    public void Update(T entity) => _dbSet.Update(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);

    public async Task SaveAsync() => await _context.SaveChangesAsync();
}

//The Traditional Way (Tracked Update)
// using (var context = new MyDbContext())
// {
//     // 1. Retrieve the record by its Id
//     var employee = await context.Employees.FindAsync(1);

//     if (employee != null)
//     {
//         // 2. Modify the properties
//         employee.Salary = 65000;
//         employee.Position = "Senior Developer";

//         // 3. Persist changes to the database
//         await context.SaveChangesAsync(); 
//     }
// }

// The Modern, Fast Way (ExecuteUpdate)
// using (var context = new MyDbContext())
// {
//     // Updates the record directly in the database using a single SQL query
//     await context.Employees
//         .Where(e => e.Id == 1)
//         .ExecuteUpdateAsync(setters => setters
//             .SetProperty(e => e.Salary, 65000)
//             .SetProperty(e => e.Position, "Senior Developer")
//         );
// }

// Updating a Disconnected Entity (Attach / Update)
// using (var context = new MyDbContext())
// {
//     // Passing a disconnected entity model back to the context
//     context.Employees.Update(updatedEmployee);
    
//     // Or mark it as modified explicitly:
//     // context.Entry(updatedEmployee).State = EntityState.Modified;

//     await context.SaveChangesAsync();
// }