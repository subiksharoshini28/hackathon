namespace EhrSecure.Api.Contracts.Patients;

public sealed class CreatePatientRequest
{
    public string Mrn { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
}
