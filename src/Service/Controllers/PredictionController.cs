using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class PredictionController(ILogger<PersonController> logger,
        PredictionEnginePool<PersonData, PersonPrediction> predictionEnginePool): ControllerBase
{
    // private readonly ILogger<WeatherForecastController> _logger = logger;
    // private readonly PredictionEnginePool<PersonData, PersonPrediction> _predictionEnginePool = predictionEnginePool;

    [HttpPost("predict-salary", Name = "PredictSalaryByName")]
    public ActionResult<object> PredictSalary([FromBody] PersonSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var input = new PersonData { Name = request.Name, Age = request.Age };
        logger.LogInformation("Predicting salary for {Name}, Age: {Age}", request.Name, request.Age);
        var prediction = predictionEnginePool.Predict(modelName: "PersonSalaryModel", example: input);

        return Ok(new
        {
            request.Name,
            request.Age,
            prediction.PredictedSalary
        });
    }

    public record PersonSearchRequest(string Name, float Age = 0);
}