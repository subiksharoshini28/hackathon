using EhrSecure.Api.Contracts.Records;
using EhrSecure.Api.Infrastructure;
using EhrSecure.Api.Infrastructure.Crypto;
using EhrSecure.Api.Infrastructure.Entities;
using EhrSecure.Api.Infrastructure.Notifications;
using EhrSecure.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Controllers;

[ApiController]
[Route("api/portal")]
public sealed class PatientPortalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFieldEncryptionService _enc;
    private readonly ICurrentUserService _current;
    private readonly IAuditService _audit;
    private readonly INotificationService _notify;

    public PatientPortalController(AppDbContext db, IFieldEncryptionService enc, ICurrentUserService current, IAuditService audit, INotificationService notify)
    {
        _db = db;
        _enc = enc;
        _current = current;
        _audit = audit;
        _notify = notify;
    }

    [HttpGet("records")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult<IReadOnlyList<MedicalRecordResponse>>> MyRecords()
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        var patientId = _current.PatientId.Value;

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

        await _audit.LogAsync("PORTAL_RECORD_READ", "portal/records", patientId);

        return response;
    }

    [HttpGet("notifications")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications()
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        var notifications = await _notify.GetAllAsync(_current.PatientId.Value);
        return Ok(notifications);
    }

    [HttpGet("notifications/unread")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetUnreadNotifications()
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        var notifications = await _notify.GetUnreadAsync(_current.PatientId.Value);
        return Ok(notifications);
    }

    [HttpPost("notifications/{id:guid}/read")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult> MarkAsRead(Guid id)
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        await _notify.MarkAsReadAsync(id, _current.PatientId.Value);
        return NoContent();
    }

    [HttpPost("notifications/read-all")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        await _notify.MarkAllAsReadAsync(_current.PatientId.Value);
        return NoContent();
    }

    [HttpGet("prescription/{recordId:guid}/download")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult> DownloadPrescription(Guid recordId)
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        var patientId = _current.PatientId.Value;
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId);
        var record = await _db.MedicalRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recordId && r.PatientId == patientId);

        if (record is null || patient is null)
        {
            return NotFound();
        }

        var diagnosis = _enc.DecryptFromBase64(record.DiagnosisEnc);
        var prescriptions = _enc.DecryptFromBase64(record.PrescriptionsEnc);
        var clinicalNotes = _enc.DecryptFromBase64(record.ClinicalNotesEnc);

        var html = GeneratePrescriptionHtml(patient, record, diagnosis, prescriptions, clinicalNotes);

        await _audit.LogAsync("PRESCRIPTION_DOWNLOAD", $"portal/prescription/{recordId}/download", patientId);

        return Content(html, "text/html");
    }

    [HttpGet("records/download-all")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<ActionResult> DownloadAllRecords()
    {
        if (!_current.PatientId.HasValue)
        {
            return Forbid();
        }

        var patientId = _current.PatientId.Value;
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId);
        var records = await _db.MedicalRecords.AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        if (patient is null)
        {
            return NotFound();
        }

        var html = GenerateAllRecordsHtml(patient, records);

        await _audit.LogAsync("RECORDS_DOWNLOAD_ALL", "portal/records/download-all", patientId);

        return Content(html, "text/html");
    }

    private string GeneratePrescriptionHtml(Patient patient, MedicalRecord record, string diagnosis, string prescriptions, string clinicalNotes)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Prescription - {patient.FullName}</title>
    <style>
        body {{ font-family: Arial, sans-serif; max-width: 800px; margin: 40px auto; padding: 20px; }}
        .header {{ text-align: center; border-bottom: 2px solid #667eea; padding-bottom: 20px; margin-bottom: 30px; }}
        .header h1 {{ color: #667eea; margin: 0; }}
        .patient-info {{ background: #f8f9fa; padding: 15px; border-radius: 8px; margin-bottom: 20px; }}
        .section {{ margin-bottom: 25px; }}
        .section h3 {{ color: #333; border-bottom: 1px solid #ddd; padding-bottom: 5px; }}
        .prescription-box {{ background: #e8f4ff; padding: 20px; border-radius: 8px; border-left: 4px solid #007bff; }}
        .footer {{ text-align: center; margin-top: 40px; font-size: 12px; color: #666; }}
        @media print {{ body {{ margin: 0; }} .no-print {{ display: none; }} }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🏥 EhrSecure Medical Center</h1>
        <p>Official Medical Prescription</p>
    </div>
    <div class='patient-info'>
        <strong>Patient:</strong> {patient.FullName}<br>
        <strong>MRN:</strong> {patient.Mrn}<br>
        <strong>Date of Birth:</strong> {patient.DateOfBirth:yyyy-MM-dd}<br>
        <strong>Date:</strong> {record.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC
    </div>
    <div class='section'>
        <h3>Diagnosis</h3>
        <p>{diagnosis}</p>
    </div>
    <div class='section'>
        <h3>Prescription</h3>
        <div class='prescription-box'>
            <pre style='white-space: pre-wrap; margin: 0;'>{prescriptions}</pre>
        </div>
    </div>
    <div class='section'>
        <h3>Clinical Notes</h3>
        <p>{clinicalNotes}</p>
    </div>
    <div class='footer'>
        <p>This is an official medical document from EhrSecure Electronic Health Record System</p>
        <p>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC | Record ID: {record.Id}</p>
    </div>
    <div class='no-print' style='text-align: center; margin-top: 20px;'>
        <button onclick='window.print()' style='padding: 10px 30px; font-size: 16px; cursor: pointer;'>🖨️ Print / Save as PDF</button>
    </div>
</body>
</html>";
    }

    private string GenerateAllRecordsHtml(Patient patient, List<MedicalRecord> records)
    {
        var recordsHtml = string.Join("\n", records.Select((r, i) => $@"
        <div class='record' style='page-break-inside: avoid; margin-bottom: 30px; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
            <h3>Record #{i + 1} - {r.CreatedAtUtc:yyyy-MM-dd}</h3>
            <p><strong>Diagnosis:</strong> {_enc.DecryptFromBase64(r.DiagnosisEnc)}</p>
            <div class='prescription-box' style='background: #e8f4ff; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                <strong>Prescription:</strong><br>
                <pre style='white-space: pre-wrap; margin: 5px 0;'>{_enc.DecryptFromBase64(r.PrescriptionsEnc)}</pre>
            </div>
            <p><strong>Clinical Notes:</strong> {_enc.DecryptFromBase64(r.ClinicalNotesEnc)}</p>
        </div>"));

        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Medical Records - {patient.FullName}</title>
    <style>
        body {{ font-family: Arial, sans-serif; max-width: 800px; margin: 40px auto; padding: 20px; }}
        .header {{ text-align: center; border-bottom: 2px solid #667eea; padding-bottom: 20px; margin-bottom: 30px; }}
        .header h1 {{ color: #667eea; margin: 0; }}
        .patient-info {{ background: #f8f9fa; padding: 15px; border-radius: 8px; margin-bottom: 20px; }}
        .footer {{ text-align: center; margin-top: 40px; font-size: 12px; color: #666; }}
        @media print {{ body {{ margin: 0; }} .no-print {{ display: none; }} }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🏥 EhrSecure Medical Center</h1>
        <p>Complete Medical Records</p>
    </div>
    <div class='patient-info'>
        <strong>Patient:</strong> {patient.FullName}<br>
        <strong>MRN:</strong> {patient.Mrn}<br>
        <strong>Date of Birth:</strong> {patient.DateOfBirth:yyyy-MM-dd}<br>
        <strong>Total Records:</strong> {records.Count}
    </div>
    {recordsHtml}
    <div class='footer'>
        <p>This is an official medical document from EhrSecure Electronic Health Record System</p>
        <p>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
    </div>
    <div class='no-print' style='text-align: center; margin-top: 20px;'>
        <button onclick='window.print()' style='padding: 10px 30px; font-size: 16px; cursor: pointer;'>🖨️ Print / Save as PDF</button>
    </div>
</body>
</html>";
    }
}
