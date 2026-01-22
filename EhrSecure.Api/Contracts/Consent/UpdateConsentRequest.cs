namespace EhrSecure.Api.Contracts.Consent;

public sealed class UpdateConsentRequest
{
    public bool AllowDoctors { get; set; }
    public bool AllowNurses { get; set; }
}
