using Microsoft.Extensions.ML;
using Service.Models;

namespace Service.BAL.Prediction;

public class PredictionService(
    PredictionEnginePool<PersonData, PersonPrediction> predictionEnginePool) : IPredictionService
{
    public (string Name, float Age, float PredictedSalary) PredictSalary(string name, float age)
    {
        var input = new PersonData { Name = name, Age = age };
        var prediction = predictionEnginePool.Predict(modelName: "PersonSalaryModel", example: input);

        return (name, age, prediction.PredictedSalary);
    }
}
