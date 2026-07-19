using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using Service.Models;

namespace Service.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(
    ILogger<WeatherForecastController> logger,
    PredictionEnginePool<PersonData, PersonPrediction> predictionEnginePool) : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger = logger;
    private readonly PredictionEnginePool<PersonData, PersonPrediction> _predictionEnginePool = predictionEnginePool;

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return [.. Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })];
    }

    public record PersonSearchRequest(string Name, float Age = 0);

    // POST /WeatherForecast/predict-salary
    // Body: { "name": "Alice", "age": 25 }
    [HttpPost("predict-salary", Name = "PredictSalaryByName")]
    public ActionResult<object> PredictSalary([FromBody] PersonSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var input = new PersonData { Name = request.Name, Age = request.Age };
        var prediction = _predictionEnginePool.Predict(modelName: "PersonSalaryModel", example: input);

        return Ok(new
        {
            request.Name,
            request.Age,
            prediction.PredictedSalary
        });
    }
}