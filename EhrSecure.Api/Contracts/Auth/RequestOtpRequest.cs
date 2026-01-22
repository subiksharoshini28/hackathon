namespace EhrSecure.Api.Contracts.Auth;

public sealed class RequestOtpRequest
{
    public Guid PatientId { get; set; }
    public string PatientIdOrMrn { get; set; } = string.Empty;
}
