namespace Service.BAL.Prediction;

public interface IPredictionService
{
    (string Name, float Age, float PredictedSalary) PredictSalary(string name, float age);
}
