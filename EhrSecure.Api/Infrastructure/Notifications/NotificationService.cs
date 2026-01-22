using EhrSecure.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task NotifyRecordAddedAsync(Guid patientId, Guid recordId, string doctorEmail)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Title = "New Medical Record Added",
            Message = $"Dr. {doctorEmail} has added a new medical record to your file.",
            Type = "RECORD_ADDED",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
            RelatedRecordId = recordId,
            TriggeredByEmail = doctorEmail
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }

    public async Task NotifyConsentChangedAsync(Guid patientId, bool allowDoctors, bool allowNurses)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Title = "Consent Settings Updated",
            Message = $"Your consent settings have been updated. Doctors: {(allowDoctors ? "Allowed" : "Denied")}, Nurses: {(allowNurses ? "Allowed" : "Denied")}",
            Type = "CONSENT_CHANGED",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(Guid patientId)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.PatientId == patientId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAllAsync(Guid patientId, int take = 50)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.PatientId == patientId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid patientId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.PatientId == patientId);

        if (notification is not null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid patientId)
    {
        await _db.Notifications
            .Where(n => n.PatientId == patientId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
