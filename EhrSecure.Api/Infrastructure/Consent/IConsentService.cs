namespace EhrSecure.Api.Infrastructure;

public interface IConsentService
{
    Task<bool> CanClinicalStaffViewAsync(Guid actorUserId, IEnumerable<string> actorRoles, Guid patientId);
}
