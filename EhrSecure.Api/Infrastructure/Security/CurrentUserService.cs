using System.Security.Claims;
using EhrSecure.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace EhrSecure.Api.Infrastructure.Security;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal Principal => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public Guid? UserId
    {
        get
        {
            var sub = Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => Principal.FindFirstValue(ClaimTypes.Email) ?? Principal.FindFirstValue("email");

    public Guid? PatientId
    {
        get
        {
            var val = Principal.FindFirstValue(JwtClaims.PatientId);
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public IReadOnlyCollection<string> Roles => Principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
