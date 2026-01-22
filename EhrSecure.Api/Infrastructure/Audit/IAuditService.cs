namespace EhrSecure.Api.Infrastructure;

public interface IAuditService
{
    Task LogAsync(string action, string resource, Guid? patientId = null);
}
