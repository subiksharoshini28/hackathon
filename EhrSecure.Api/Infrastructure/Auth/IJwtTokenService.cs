using System.Security.Claims;

namespace EhrSecure.Api.Infrastructure.Auth;

public interface IJwtTokenService
{
    Task<(string token, DateTime expiresAtUtc)> CreateAccessTokenAsync(Guid userId, string email, Guid? patientId, IEnumerable<string> roles);
    ClaimsPrincipal? ValidateToken(string token);
}
