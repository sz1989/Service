using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Model;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class PersonController(ILogger<PersonController> logger,
    IPersonRepository personRepo) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<Person>> GetPerson(int id)
    {
        logger.LogInformation("Getting person {id}", id);
        var p = await personRepo.GetPersonByIdAsync(id);
        return Ok(p);
    }
}