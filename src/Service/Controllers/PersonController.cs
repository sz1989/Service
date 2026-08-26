using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Service.Model;
using Service.Services;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class PersonController(ILogger<PersonController> logger,
    IPersonRepository personRepo,
    IBackgroundTaskQueue taskQueue,
    IDistributedCache cache,
    IRedisPublisher publisher) : ControllerBase
{
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2) // Renews if accessed within 2 minutes
    };

    private static string PersonCacheKey(int id) => $"person:{id}";

    [Authorize(Roles = "admin, user")]
    [HttpGet("{id}")]
    public async Task<ActionResult<Person>> GetPerson(int id)
    {
        logger.LogInformation("Getting person {id}", id);

        var cacheKey = PersonCacheKey(id);
        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            logger.LogInformation("Cache hit for person {id}", id);
            return Ok(JsonSerializer.Deserialize<IEnumerable<Person>>(cached));
        }

        logger.LogInformation("Cache miss for person {id}, fetching from database", id);
        var p = await personRepo.GetPersonByIdAsync(id);
        if (!p.Any())
        {
            return NotFound();
        }

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(p), CacheEntryOptions);

        return Ok(p);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("All")]
    public async Task<ActionResult<IEnumerable<Person>>> GetAllPersons()
    {
        // test global exception handling by throwing an exception here
        throw new NotImplementedException("GetAllPersons is not implemented yet.");
    }

    [AllowAnonymous]
    [HttpPost("{id}/refresh")]
    public async Task<IActionResult> RefreshPerson(int id)
    {
        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            logger.LogInformation("Background refresh started for person {id}", id);
            var p = await personRepo.GetPersonByIdAsync(id);
            // ... do the actual refresh work with p here ...

            await cache.RemoveAsync(PersonCacheKey(id), token);

            var notification = JsonSerializer.Serialize(new { PersonId = id, Event = "refreshed" });
            await publisher.PublishAsync(RedisChannels.PersonUpdates, notification);

            logger.LogInformation("Background refresh completed for person {id}", id);
        });

        return Accepted();  //202
    }
}