namespace EhrSecure.Api.Contracts.Records;

public sealed class MedicalRecordResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string Prescriptions { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
