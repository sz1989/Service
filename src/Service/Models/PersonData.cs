using Microsoft.ML.Data;

namespace Service.Models;

// One row of training data. Also used as the input shape for predictions.
public class PersonData
{
    [LoadColumn(0)]
    public string Name { get; set; } = string.Empty;

    [LoadColumn(1)]
    public float Age { get; set; }

    // Salary is the training label. Not needed on prediction requests.
    [LoadColumn(2)]
    [ColumnName("Label")]
    public float Salary { get; set; }
}
