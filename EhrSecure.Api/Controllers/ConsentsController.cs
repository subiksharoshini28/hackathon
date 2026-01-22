using EhrSecure.Api.Contracts.Consent;
using EhrSecure.Api.Infrastructure;
using EhrSecure.Api.Infrastructure.Notifications;
using EhrSecure.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Controllers;

[ApiController]
[Route("api/consents")]
public sealed class ConsentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditService _audit;
    private readonly INotificationService _notify;

    public ConsentsController(AppDbContext db, ICurrentUserService current, IAuditService audit, INotificationService notify)
    {
        _db = db;
        _current = current;
        _audit = audit;
        _notify = notify;
    }

    [HttpGet("me")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult<object>> GetMyConsent()
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        var consent = await _db.Consents.AsNoTracking().SingleOrDefaultAsync(x => x.PatientId == _current.PatientId.Value);
        if (consent is null)
        {
            return NotFound();
        }

        await _audit.LogAsync("CONSENT_READ", "consents/me", _current.PatientId.Value);

        return new { consent.PatientId, consent.AllowDoctors, consent.AllowNurses, consent.UpdatedAtUtc };
    }

    [HttpPut("me")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult> UpdateMyConsent([FromBody] UpdateConsentRequest request)
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        var consent = await _db.Consents.SingleOrDefaultAsync(x => x.PatientId == _current.PatientId.Value);
        if (consent is null)
        {
            return NotFound();
        }

        consent.AllowDoctors = request.AllowDoctors;
        consent.AllowNurses = request.AllowNurses;
        consent.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("CONSENT_UPDATE", "consents/me", _current.PatientId.Value);
        await _notify.NotifyConsentChangedAsync(_current.PatientId.Value, request.AllowDoctors, request.AllowNurses);

        return NoContent();
    }
}
