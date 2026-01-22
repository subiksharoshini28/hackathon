namespace EhrSecure.Api.Infrastructure.Entities;

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? RelatedRecordId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? TriggeredByEmail { get; set; }
}
