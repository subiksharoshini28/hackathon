namespace EhrSecure.Api.Contracts.Auth;

public sealed class OtpLoginRequest
{
    public Guid PatientId { get; set; }
    public string PatientIdOrMrn { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}
