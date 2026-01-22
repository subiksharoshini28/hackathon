using System.Security.Cryptography;
using EhrSecure.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Infrastructure.Auth;

public sealed class OtpService : IOtpService
{
    private readonly AppDbContext _db;

    public OtpService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateOtpAsync(Guid patientId)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        var otp = new Otp
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Code = code,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Otps.Add(otp);
        await _db.SaveChangesAsync();

        return code;
    }

    public async Task<bool> ValidateOtpAsync(Guid patientId, string code)
    {
        var otp = await _db.Otps
            .Where(x => x.PatientId == patientId && x.Code == code && !x.IsUsed && x.ExpiresAtUtc > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (otp is null) return false;

        otp.IsUsed = true;
        await _db.SaveChangesAsync();

        return true;
    }
}
