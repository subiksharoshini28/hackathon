using Microsoft.AspNetCore.Identity;

namespace EhrSecure.Api.Infrastructure;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? PatientId { get; set; }
}
