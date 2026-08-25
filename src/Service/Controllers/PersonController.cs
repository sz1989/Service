using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Model;
using Service.Services;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class PersonController(ILogger<PersonController> logger,
    IPersonRepository personRepo,
    IBackgroundTaskQueue taskQueue) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<Person>> GetPerson(int id)
    {
        logger.LogInformation("Getting person {id}", id);
        var p = await personRepo.GetPersonByIdAsync(id);
        if (!p.Any())
        {
            return NotFound();
        }
        return Ok(p);
    }

    [HttpGet("All")]
    public async Task<ActionResult<IEnumerable<Person>>> GetAllPersons()
    {
        // test global exception handling by throwing an exception here
        throw new NotImplementedException("GetAllPersons is not implemented yet.");
    }

    [HttpPost("{id}/refresh")]
    public async Task<IActionResult> RefreshPerson(int id)
    {
        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            logger.LogInformation("Background refresh started for person {id}", id);
            var p = await personRepo.GetPersonByIdAsync(id);
            // ... do the actual refresh work with p here ...
            logger.LogInformation("Background refresh completed for person {id}", id);
        });

        return Accepted();  //202
    }
}