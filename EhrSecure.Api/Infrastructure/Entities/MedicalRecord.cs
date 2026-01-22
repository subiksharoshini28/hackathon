namespace EhrSecure.Api.Infrastructure.Entities;

public sealed class MedicalRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string DiagnosisEnc { get; set; } = string.Empty;
    public string PrescriptionsEnc { get; set; } = string.Empty;
    public string ClinicalNotesEnc { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
