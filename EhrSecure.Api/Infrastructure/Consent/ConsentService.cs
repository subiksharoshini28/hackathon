using EhrSecure.Api.Infrastructure.Auth;
using EhrSecure.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Infrastructure;

public sealed class ConsentService : IConsentService
{
    private readonly AppDbContext _db;

    public ConsentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanClinicalStaffViewAsync(Guid actorUserId, IEnumerable<string> actorRoles, Guid patientId)
    {
        var roles = actorRoles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (roles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var consent = await _db.Consents.AsNoTracking().SingleOrDefaultAsync(x => x.PatientId == patientId);
        if (consent is null)
        {
            return false;
        }

        if (roles.Contains(AppRoles.Doctor, StringComparer.OrdinalIgnoreCase))
        {
            return consent.AllowDoctors;
        }

        if (roles.Contains(AppRoles.Nurse, StringComparer.OrdinalIgnoreCase))
        {
            return consent.AllowNurses;
        }

        return false;
    }
}
