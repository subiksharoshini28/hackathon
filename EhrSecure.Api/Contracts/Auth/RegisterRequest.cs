namespace EhrSecure.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
}
