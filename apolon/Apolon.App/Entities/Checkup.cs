namespace Apolon.App.Entities;

public class Checkup
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int CheckupTypeId { get; set; }
    public DateTime CheckupDate { get; set; }

    public Patient? Patient { get; set; }
    public CheckupType? CheckupType { get; set; }
}