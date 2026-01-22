namespace EhrSecure.Api.Infrastructure.Auth;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(Guid patientId);
    Task<bool> ValidateOtpAsync(Guid patientId, string code);
}
