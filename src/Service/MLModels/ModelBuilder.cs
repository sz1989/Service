using Microsoft.ML;
using Service.Models;

namespace Service.MLModels;

public static class ModelBuilder
{
    public static void TrainAndSaveModel(string trainingDataPath, string modelSavePath)
    {
        var mlContext = new MLContext(seed: 0);

        // IDataView dataView = mlContext.Data.LoadFromTextFile<PersonData>(
        //     trainingDataPath, hasHeader: true, separatorChar: ',');

        IDataView dataView = mlContext.Data.LoadFromEnumerable(GetSampleData());

        var pipeline = mlContext.Transforms.Categorical.OneHotEncoding("NameEncoded", nameof(PersonData.Name))
            .Append(mlContext.Transforms.Concatenate("Features", "NameEncoded", nameof(PersonData.Age)))
            .Append(mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features"));

        var model = pipeline.Fit(dataView);

        mlContext.Model.Save(model, dataView.Schema, modelSavePath);
    }

    private static IEnumerable<PersonData> GetSampleData()
    {
        return
        [
            new PersonData { Name = "Alice", Age = 25f, Salary = 45000f },
            new PersonData { Name = "Bob", Age = 30f, Salary = 52000f },
            new PersonData { Name = "Charlie", Age = 35f, Salary = 61000f },
            new PersonData { Name = "Diana", Age = 28f, Salary = 48000f },
            new PersonData { Name = "Ethan", Age = 40f, Salary = 72000f },
            new PersonData { Name = "Fiona", Age = 45f, Salary = 80000f },
            new PersonData { Name = "George", Age = 50f, Salary = 90000f },
            new PersonData { Name = "Hannah", Age = 33f, Salary = 58000f },
            new PersonData { Name = "Ian", Age = 38f, Salary = 66000f },
            new PersonData { Name = "Julia", Age = 29f, Salary = 47000f }
        ];
    }
}
