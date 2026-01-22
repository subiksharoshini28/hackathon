using EhrSecure.Api.Infrastructure.Entities;
using EhrSecure.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http;

namespace EhrSecure.Api.Infrastructure;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IHttpContextAccessor _http;

    public AuditService(AppDbContext db, ICurrentUserService current, IHttpContextAccessor http)
    {
        _db = db;
        _current = current;
        _http = http;
    }

    public async Task LogAsync(string action, string resource, Guid? patientId = null)
    {
        var ctx = _http.HttpContext;
        var ip = ctx?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var ua = ctx?.Request.Headers.UserAgent.ToString() ?? string.Empty;

        var entry = new AuditLog
        {
            ActorUserId = _current.UserId,
            ActorEmail = _current.Email ?? string.Empty,
            ActorRoles = string.Join(",", _current.Roles),
            PatientId = patientId,
            Action = action,
            Resource = resource,
            TimestampUtc = DateTime.UtcNow,
            IpAddress = ip,
            UserAgent = ua
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }
}
