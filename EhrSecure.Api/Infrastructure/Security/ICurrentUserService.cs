using System.Security.Claims;

namespace EhrSecure.Api.Infrastructure.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    Guid? PatientId { get; }
    bool IsInRole(string role);
    ClaimsPrincipal Principal { get; }
}
