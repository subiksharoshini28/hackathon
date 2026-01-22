namespace EhrSecure.Api.Contracts.Records;

public sealed class AddMedicalRecordRequest
{
    public string Diagnosis { get; set; } = string.Empty;
    public string Prescriptions { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
}
