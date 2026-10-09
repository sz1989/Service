using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Service.Model;
using Service.Services;

namespace Service.BAL.People;

public class PersonService(
    ILogger<PersonService> logger,
    IPersonRepository personRepo,
    IBackgroundTaskQueue taskQueue,
    IDistributedCache cache,
    IRedisPublisher publisher,
    IServiceScopeFactory scopeFactory) : IPersonService
{
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2) // Renews if accessed within 2 minutes
    };

    private static string PersonCacheKey(int id) => $"person:{id}";

    public async Task<IEnumerable<Person>> GetPersonByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = PersonCacheKey(id);
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogInformation("Cache hit for person {Id}", id);
            return JsonSerializer.Deserialize<IEnumerable<Person>>(cached) ?? [];
        }

        logger.LogInformation("Cache miss for person {Id}, fetching from database", id);
        var people = (await personRepo.GetPersonByIdAsync(id)).ToList();

        if (people.Count > 0)
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(people), CacheEntryOptions, ct);
        }

        return people;
    }

    public Task<IEnumerable<Person>> GetAllPersonsAsync(CancellationToken ct = default) =>
        personRepo.GetAllAsync();

    public async Task QueuePersonRefreshAsync(int id)
    {
        await taskQueue.QueueBackgroundWorkItemAsync(async (scope, token) =>
        {
            logger.LogInformation("Background refresh started for person {Id}", id);
            var dbContext = scope!.GetRequiredService<AppDbContext>();
            var p = await dbContext.Persons.FindAsync([id], token);
            // ... do the actual refresh work with p here ...

            await cache.RemoveAsync(PersonCacheKey(id), token);

            var notification = JsonSerializer.Serialize(new { PersonId = id, Event = "refreshed" });
            await publisher.PublishAsync(RedisChannels.PersonUpdates, notification);

            logger.LogInformation("Background refresh completed for person {Id}", id);
        }, scopeFactory);
    }

    public async Task<Person?> UpdatePersonAsync(Person updatePerson, CancellationToken ct = default)
    {
        var existing = await personRepo.GetByIdAsync(updatePerson.Id);
        if (existing is null)
        {
            return null;
        }

        // `existing` is already tracked by the context, so copy values onto it
        // rather than attaching a second instance with the same key.
        existing.Name = updatePerson.Name;
        existing.DateOfBirth = updatePerson.DateOfBirth;
        existing.ManagerId = updatePerson.ManagerId;
        existing.Salary = updatePerson.Salary;

        personRepo.Update(existing);
        await personRepo.SaveAsync();
        await cache.RemoveAsync(PersonCacheKey(existing.Id), ct);

        logger.LogInformation("Updated person {Id}", existing.Id);
        return existing;
    }

    public async Task<Person> CreateANewAsync(Person newPerson)
    {
        // EF Core's default convention is ValueGeneratedOnAdd, 
        // meaning Postgres already auto-generates this as an identity/serial column.
        // var newId = await personRepo.GetMaxIdAsync() + 1;
        // newPerson.Id = newId;
        await personRepo.AddAsync(newPerson);
        await personRepo.SaveAsync();
        return newPerson;
    }

    public async Task<bool> DeletePersonAsync(int id, CancellationToken ct = default)
    {
        var existing = await personRepo.GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        personRepo.Delete(existing);
        await personRepo.SaveAsync();
        await cache.RemoveAsync(PersonCacheKey(id), ct);
        return true;
    }
}
