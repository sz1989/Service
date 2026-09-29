using Asp.Versioning;
using Service.BAL.People;
using Service.Model;

namespace Service.Controllers.v2;

[Authorize]
[ApiController]
[ApiVersion(2.0)]
[Route("[controller]")]                        // legacy, unversioned — remove after clients migrate
[Route("v{version:apiVersion}/[controller]")]
public class PersonController(ILogger<PersonController> logger, IPersonService personService) : ControllerBase
{
    [Authorize(Roles = "admin, user")]
    [HttpGet("{id}")]
    public async Task<ActionResult<Person>> GetPerson(int id)
    {
        logger.LogInformation("Getting person from V2 {id}", id);

        var people = await personService.GetPersonByIdAsync(id);
        if (!people.Any())
        {
            return NotFound();
        }

        return Ok(people);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("All")]
    public async Task<ActionResult<IEnumerable<Person>>> GetAllPersons()
    {
        return Ok(await personService.GetAllPersonsAsync());
    }

    [AllowAnonymous]
    [HttpPost("{id}/refresh")]
    public async Task<IActionResult> RefreshPerson(int id)
    {
        await personService.QueuePersonRefreshAsync(id);

        return Accepted();  //202
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] Person newPerson)
    {
        // POST = server-assigned creation
        var person = await personService.CreateANewAsync(newPerson);
        return CreatedAtAction(nameof(GetPerson), new { id = person.Id }, person);
    }

    [Authorize(Roles = "admin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Person updatePerson)
    {
        // PUT = idempotent update-at-a-known-id
        var updated = await personService.UpdatePersonAsync(updatePerson);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await personService.DeletePersonAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent(); // 204
    }
}
