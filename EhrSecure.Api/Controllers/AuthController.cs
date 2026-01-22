using EhrSecure.Api.Contracts.Auth;
using EhrSecure.Api.Infrastructure;
using EhrSecure.Api.Infrastructure.Auth;
using EhrSecure.Api.Infrastructure.Entities;
using EhrSecure.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;
    private readonly IOtpService _otp;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwt,
        IAuditService audit,
        ICurrentUserService current,
        IOtpService otp,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
        _audit = audit;
        _current = current;
        _otp = otp;
        _db = db;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = await _jwt.CreateAccessTokenAsync(user.Id, user.Email ?? string.Empty, user.PatientId, roles);

        await _audit.LogAsync("AUTH_LOGIN", "auth/login", user.PatientId);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToArray(),
            PatientId = user.PatientId
        };
    }

    [HttpPost("register")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest();
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            PatientId = request.PatientId
        };

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            return BadRequest(new { errors = create.Errors.Select(e => e.Description).ToArray() });
        }

        var addRole = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addRole.Succeeded)
        {
            return BadRequest(new { errors = addRole.Errors.Select(e => e.Description).ToArray() });
        }

        await _audit.LogAsync("USER_CREATE", $"auth/register/{request.Role}", request.PatientId);

        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        return new
        {
            userId = _current.UserId,
            email = _current.Email,
            roles = _current.Roles,
            patientId = _current.PatientId
        };
    }

    [HttpPost("request-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> RequestOtp([FromBody] RequestOtpRequest request)
    {
        Patient? patient = null;
        
        // Try to find by GUID first
        if (Guid.TryParse(request.PatientIdOrMrn, out var patientGuid))
        {
            patient = await _db.Patients.FindAsync(patientGuid);
        }
        
        // If not found, try by MRN (e.g., P001, P002)
        if (patient is null)
        {
            patient = await _db.Patients.FirstOrDefaultAsync(p => p.Mrn == request.PatientIdOrMrn);
        }
        
        if (patient is null)
        {
            return NotFound(new { error = "Patient not found. Enter Patient ID (GUID) or MRN (e.g., P001)" });
        }

        var code = await _otp.GenerateOtpAsync(patient.Id);

        return Ok(new { message = "OTP generated", otp = code, patientName = patient.FullName, patientId = patient.Id });
    }

    [HttpPost("login-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginWithOtp([FromBody] OtpLoginRequest request)
    {
        Guid patientId = request.PatientId;
        
        // If PatientId is empty, try to resolve from MRN
        if (patientId == Guid.Empty && !string.IsNullOrEmpty(request.PatientIdOrMrn))
        {
            if (Guid.TryParse(request.PatientIdOrMrn, out var parsed))
            {
                patientId = parsed;
            }
            else
            {
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Mrn == request.PatientIdOrMrn);
                if (patient != null) patientId = patient.Id;
            }
        }
        
        var valid = await _otp.ValidateOtpAsync(patientId, request.Otp);
        if (!valid)
        {
            return Unauthorized(new { error = "Invalid or expired OTP" });
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PatientId == patientId);
        if (user is null)
        {
            return Unauthorized(new { error = "No user account linked to this patient" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = await _jwt.CreateAccessTokenAsync(user.Id, user.Email ?? string.Empty, user.PatientId, roles);

        await _audit.LogAsync("AUTH_OTP_LOGIN", "auth/login-otp", user.PatientId);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToArray(),
            PatientId = user.PatientId
        };
    }
}
