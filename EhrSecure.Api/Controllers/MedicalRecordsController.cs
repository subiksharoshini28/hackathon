using EhrSecure.Api.Contracts.Records;
using EhrSecure.Api.Infrastructure;
using EhrSecure.Api.Infrastructure.Auth;
using EhrSecure.Api.Infrastructure.Crypto;
using EhrSecure.Api.Infrastructure.Entities;
using EhrSecure.Api.Infrastructure.Notifications;
using EhrSecure.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Controllers;

[ApiController]
[Route("api/records")]
public sealed class MedicalRecordsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFieldEncryptionService _enc;
    private readonly IConsentService _consent;
    private readonly ICurrentUserService _current;
    private readonly IAuditService _audit;
    private readonly INotificationService _notify;

    public MedicalRecordsController(
        AppDbContext db,
        IFieldEncryptionService enc,
        IConsentService consent,
        ICurrentUserService current,
        IAuditService audit,
        INotificationService notify)
    {
        _db = db;
        _enc = enc;
        _consent = consent;
        _current = current;
        _audit = audit;
        _notify = notify;
    }

    [HttpPost("{patientId:guid}")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<ActionResult> Add(Guid patientId, [FromBody] AddMedicalRecordRequest request)
    {
        if (!_current.UserId.HasValue)
        {
            return Forbid();
        }

        var exists = await _db.Patients.AsNoTracking().AnyAsync(x => x.Id == patientId);
        if (!exists)
        {
            return NotFound();
        }

        var record = new MedicalRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DiagnosisEnc = _enc.EncryptToBase64(request.Diagnosis),
            PrescriptionsEnc = _enc.EncryptToBase64(request.Prescriptions),
            ClinicalNotesEnc = _enc.EncryptToBase64(request.ClinicalNotes),
            CreatedByUserId = _current.UserId.Value,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.MedicalRecords.Add(record);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("RECORD_ADD", $"records/{patientId}", patientId);
        await _notify.NotifyRecordAddedAsync(patientId, record.Id, _current.Email ?? "Unknown");

        return Created($"/api/records/{patientId}/{record.Id}", new { record.Id });
    }

    [HttpGet("{patientId:guid}")]
    [Authorize(Policy = "ClinicalStaff")]
    public async Task<ActionResult<IReadOnlyList<MedicalRecordResponse>>> GetForPatient(Guid patientId)
    {
        if (!_current.UserId.HasValue)
        {
            return Forbid();
        }

        if (!_current.IsInRole(AppRoles.Admin))
        {
            var allowed = await _consent.CanClinicalStaffViewAsync(_current.UserId.Value, _current.Roles, patientId);
            if (!allowed)
            {
                await _audit.LogAsync("RECORD_READ_DENY", $"records/{patientId}", patientId);
                return Forbid();
            }
        }

        var records = await _db.MedicalRecords.AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        var response = records.Select(x => new MedicalRecordResponse
        {
            Id = x.Id,
            PatientId = x.PatientId,
            Diagnosis = _enc.DecryptFromBase64(x.DiagnosisEnc),
            Prescriptions = _enc.DecryptFromBase64(x.PrescriptionsEnc),
            ClinicalNotes = _enc.DecryptFromBase64(x.ClinicalNotesEnc),
            CreatedByUserId = x.CreatedByUserId,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();

        await _audit.LogAsync("RECORD_READ", $"records/{patientId}", patientId);

        return response;
    }

    [HttpGet("my-patients")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetMyPatients()
    {
        if (!_current.UserId.HasValue)
        {
            return Forbid();
        }

        // Get patients that this doctor has created records for
        var patientIds = await _db.MedicalRecords.AsNoTracking()
            .Where(r => r.CreatedByUserId == _current.UserId.Value)
            .Select(r => r.PatientId)
            .Distinct()
            .ToListAsync();

        var patients = await _db.Patients.AsNoTracking()
            .Where(p => patientIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Mrn,
                p.FullName,
                p.DateOfBirth,
                p.Gender,
                LastVisit = _db.MedicalRecords
                    .Where(r => r.PatientId == p.Id && r.CreatedByUserId == _current.UserId.Value)
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .Select(r => r.CreatedAtUtc)
                    .FirstOrDefault(),
                RecordCount = _db.MedicalRecords
                    .Count(r => r.PatientId == p.Id && r.CreatedByUserId == _current.UserId.Value)
            })
            .OrderByDescending(p => p.LastVisit)
            .ToListAsync();

        return Ok(patients);
    }
}
