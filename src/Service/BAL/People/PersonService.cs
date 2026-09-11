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

    public async Task<IEnumerable<Person>> GetPersonByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = PersonCacheKey(id);
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Cache hit for person {Id}", id);
            return JsonSerializer.Deserialize<IEnumerable<Person>>(cached) ?? [];
        }

        logger.LogInformation("Cache miss for person {Id}, fetching from database", id);
        var people = (await personRepo.GetPersonByIdAsync(id)).ToList();

        if (people.Count > 0)
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(people), CacheEntryOptions, cancellationToken);
        }

        return people;
    }

    public Task<IEnumerable<Person>> GetAllPersonsAsync(CancellationToken cancellationToken = default) =>
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
}
