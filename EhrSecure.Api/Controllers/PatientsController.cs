using EhrSecure.Api.Contracts.Patients;
using EhrSecure.Api.Infrastructure;
using EhrSecure.Api.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Controllers;

[ApiController]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public PatientsController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Patient>> Create([FromBody] CreatePatientRequest request)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Mrn = request.Mrn,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Patients.Add(patient);
        _db.Consents.Add(new Consent
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            AllowDoctors = false,
            AllowNurses = false,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync("PATIENT_CREATE", $"patients/{patient.Id}", patient.Id);

        return CreatedAtAction(nameof(GetById), new { patientId = patient.Id }, patient);
    }

    [HttpGet("{patientId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Patient>> GetById(Guid patientId)
    {
        var patient = await _db.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == patientId);
        if (patient is null)
        {
            return NotFound();
        }

        await _audit.LogAsync("PATIENT_READ", $"patients/{patientId}", patientId);
        return patient;
    }
}
