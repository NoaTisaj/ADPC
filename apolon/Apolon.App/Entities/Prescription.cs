namespace Apolon.App.Entities;

public class Prescription
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int MedicationId { get; set; }
    public decimal Dosage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Patient? Patient { get; set; }
    public Medication? Medication { get; set; }
}