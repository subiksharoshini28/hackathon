namespace EhrSecure.Api.Infrastructure.Entities;

public sealed class Consent
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public bool AllowDoctors { get; set; }
    public bool AllowNurses { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
