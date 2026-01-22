namespace EhrSecure.Api.Infrastructure.Entities;

public sealed class Patient
{
    public Guid Id { get; set; }
    public string Mrn { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? AssignedDoctorId { get; set; }
    public string? AssignedDoctorEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
}
