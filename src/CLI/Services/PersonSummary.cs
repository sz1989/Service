using System.ComponentModel;

namespace CLI.Services;

internal sealed record PersonSummary(
    int Id,
    string Name,
    [property: Description("Date of birth")] DateOnly DateOfBirth,
    [property: Description("Annual salary")] decimal Salary
);

