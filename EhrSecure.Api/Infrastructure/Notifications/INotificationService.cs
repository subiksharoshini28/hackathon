namespace EhrSecure.Api.Infrastructure.Notifications;

public interface INotificationService
{
    Task NotifyRecordAddedAsync(Guid patientId, Guid recordId, string doctorEmail);
    Task NotifyConsentChangedAsync(Guid patientId, bool allowDoctors, bool allowNurses);
    Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(Guid patientId);
    Task<IReadOnlyList<NotificationDto>> GetAllAsync(Guid patientId, int take = 50);
    Task MarkAsReadAsync(Guid notificationId, Guid patientId);
    Task MarkAllAsReadAsync(Guid patientId);
}

public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
