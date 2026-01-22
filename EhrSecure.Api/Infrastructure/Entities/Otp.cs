namespace EhrSecure.Api.Infrastructure.Entities;

public sealed class Otp
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
