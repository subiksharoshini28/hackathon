using EhrSecure.Api.Contracts.Patients;
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
[Route("api/receptionist")]
[Authorize(Policy = "FrontDesk")]
public sealed class ReceptionistController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _current;
    private readonly IAuditService _audit;

    public ReceptionistController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService current,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _current = current;
        _audit = audit;
    }

    [HttpGet("doctors")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetDoctors()
    {
        var doctorRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Doctor);
        if (doctorRole is null) return Ok(Array.Empty<object>());

        var doctorUserIds = await _db.UserRoles
            .Where(ur => ur.RoleId == doctorRole.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var doctors = await _userManager.Users
            .Where(u => doctorUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        return Ok(doctors);
    }

    [HttpPost("register-patient")]
    public async Task<ActionResult<object>> RegisterPatient([FromBody] RegisterPatientRequest request)
    {
        if (await _db.Patients.AnyAsync(p => p.Mrn == request.Mrn))
        {
            return BadRequest(new { error = "MRN already exists" });
        }

        string? doctorEmail = null;
        if (request.AssignedDoctorId.HasValue)
        {
            var doctor = await _userManager.FindByIdAsync(request.AssignedDoctorId.Value.ToString());
            doctorEmail = doctor?.Email;
        }

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Mrn = request.Mrn,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            CreatedAtUtc = DateTime.UtcNow,
            AssignedDoctorId = request.AssignedDoctorId,
            AssignedDoctorEmail = doctorEmail,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail
        };

        _db.Patients.Add(patient);

        var consent = new Consent
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            AllowDoctors = true,
            AllowNurses = true,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Consents.Add(consent);

        await _db.SaveChangesAsync();
        await _audit.LogAsync("PATIENT_REGISTER", $"receptionist/register-patient/{patient.Id}", patient.Id);

        return Ok(new
        {
            patient.Id,
            patient.Mrn,
            patient.FullName,
            patient.AssignedDoctorEmail,
            message = "Patient registered successfully. Create user account to enable portal access."
        });
    }

    [HttpGet("patients")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetPatients()
    {
        var patients = await _db.Patients
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new
            {
                p.Id,
                p.Mrn,
                p.FullName,
                p.DateOfBirth,
                p.Gender,
                p.AssignedDoctorEmail,
                p.ContactPhone,
                p.ContactEmail,
                p.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpGet("patients/by-doctor/{doctorId:guid}")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetPatientsByDoctor(Guid doctorId)
    {
        var patients = await _db.Patients
            .AsNoTracking()
            .Where(p => p.AssignedDoctorId == doctorId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new
            {
                p.Id,
                p.Mrn,
                p.FullName,
                p.DateOfBirth,
                p.Gender,
                p.ContactPhone,
                p.ContactEmail
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpPut("patients/{patientId:guid}/assign-doctor")]
    public async Task<ActionResult> AssignDoctor(Guid patientId, [FromBody] AssignDoctorRequest request)
    {
        var patient = await _db.Patients.FindAsync(patientId);
        if (patient is null) return NotFound();

        var doctor = await _userManager.FindByIdAsync(request.DoctorId.ToString());
        if (doctor is null) return BadRequest(new { error = "Doctor not found" });

        patient.AssignedDoctorId = request.DoctorId;
        patient.AssignedDoctorEmail = doctor.Email;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("PATIENT_ASSIGN_DOCTOR", $"receptionist/assign-doctor/{patientId}", patientId);

        return NoContent();
    }
}

public sealed class AssignDoctorRequest
{
    public Guid DoctorId { get; set; }
}
