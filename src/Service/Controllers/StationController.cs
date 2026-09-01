using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class StationController(
    ILogger<StationController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAllStations([FromQuery] PaginationFilter? filter)
    {
        // url -> Stations
        if (filter is null)
        {
            logger.LogInformation("Getting all stations");
            return Ok();
        }
        
        //Paginated; Stations?pageNumber=2&pageSize=10
        // db.Stations.Skip((filter.pageNumber -1) * filter.pageSize).Take(filter.pageSize)
        // total = db.Stations.Count
        // totalPage = total / filter.pageSize
        // 5. Wrap your data inside a standard envelope pattern
        // var response = new
        // {
        //     PageNumber = filter.PageNumber,
        //     PageSize = filter.PageSize,
        //     TotalPages = totalPages,
        //     TotalRecords = totalRecords,
        //     Data = pagedData
        // };
        // return Ok(response);
        return Ok(filter);
    }

    [HttpGet("{id:string}")]
    public ActionResult<Station> GetStation(string id)
    {
        // url -> Stations/{id}
        logger.LogInformation("Getting station with ID: {id}", id);
        return Ok();
    }

    [HttpGet("{id}/arrivals")]
    public IActionResult GetArrivals(string id)
    {
        // url -> Stations/{id}/arrivals
        logger.LogInformation("Getting arrivals for station: {Id}", id);
        return Ok();
    }

    [HttpGet("service-alerts")]
    public IActionResult GetServiceAlerts([FromQuery] string station)
    {
        // url -> /Station/service-alerts?station=123
        logger.LogInformation("Getting service alerts for station: {StationId}", station);
        return Ok();
    }

    public record Station(string Id, string Name);
}