using Service.BAL.Prediction;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class PredictionController(ILogger<PredictionController> logger,
        IPredictionService predictionService): ControllerBase
{
    [HttpPost("predict-salary", Name = "PredictSalaryByName")]
    public ActionResult<object> PredictSalary([FromBody] PersonSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        logger.LogInformation("Predict-salary request for {Name}, Age: {Age}", request.Name, request.Age);
        var (name, age, predictedSalary) = predictionService.PredictSalary(request.Name, request.Age);

        return Ok(new { Name = name, Age = age, PredictedSalary = predictedSalary });
    }

    public record PersonSearchRequest(string Name, float Age = 0);
}
