namespace Service.Model;

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public int? ManagerId { get; set; }
    public decimal Salary { get; set; }
}