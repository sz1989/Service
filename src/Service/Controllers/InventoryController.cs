using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class InventoryController(
    ILogger<InventoryController> logger,
    AppDbContext dbContext) : ControllerBase
{
    [HttpGet("All")]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAllInventoryItems()
    {
        logger.LogInformation("Getting all inventory items");

        var items = await dbContext.Inventory.ToListAsync();

        return Ok(items);
    }
}