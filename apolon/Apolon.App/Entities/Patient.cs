namespace Apolon.App.Entities;

public class Patient
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public double? WeightKg { get; set; }

    public List<Checkup> Checkups { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
}