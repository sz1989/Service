using Microsoft.ML.Data;

namespace Service.Models;

// What the model hands back for a given input.
public class PersonPrediction
{
    [ColumnName("Score")]
    public float PredictedSalary { get; set; }
}
