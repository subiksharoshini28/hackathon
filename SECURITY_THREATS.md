# Security Threat Models and Attack Scenarios

## EhrSecure - Electronic Health Record System

This document outlines the threat models, attack scenarios, and mitigations implemented in the EhrSecure system. Suitable for academic evaluation and viva presentation.

---

## 1. Unauthorized Access via Credential Compromise

### Threat Description
Attackers obtain valid user credentials through phishing, brute force attacks, or credential stuffing to gain unauthorized access to patient records.

### Attack Scenarios
- Brute force password guessing on login endpoint
- Credential stuffing using leaked password databases
- Session hijacking via stolen JWT tokens

### Implemented Mitigations

| Control | Implementation | Location |
|---------|----------------|----------|
| **Strong Password Policy** | Minimum 12 characters, uppercase, lowercase, digit, special character required | `Program.cs` - Identity configuration |
| **Account Lockout** | 5 failed attempts = 15-minute lockout | `Program.cs` - `options.Lockout.*` |
| **Short Token Expiry** | JWT tokens expire in 30 minutes | `appsettings.json` - `Jwt.AccessTokenMinutes` |
| **Secure Token Storage** | Tokens stored with HttpOnly recommendation | Frontend best practice |
| **Login Rate Limiting** | Built-in via ASP.NET Core Identity lockout | `UserManager` |

### Code Reference
```csharp
// Program.cs
options.Password.RequiredLength = 12;
options.Password.RequireNonAlphanumeric = true;
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
```

### Audit Trail
All login attempts (successful and failed) are logged in `AuditLogs` table with:
- Actor identity
- Timestamp
- IP address
- User agent

---

## 2. Privilege Escalation

### Threat Description
Users attempt to access or modify records beyond their assigned role permissions, potentially by manipulating requests or exploiting authorization flaws.

### Attack Scenarios
- Patient tries to access another patient's records
- Nurse attempts to add medical records (write access)
- User modifies JWT token claims to elevate privileges

### Implemented Mitigations

| Control | Implementation | Location |
|---------|----------------|----------|
| **Server-Side Role Enforcement** | All authorization checked on server, never trusted from client | Controllers with `[Authorize(Policy = "...")]` |
| **Policy-Based Authorization** | Granular policies for each role | `Program.cs` - `AddAuthorization()` |
| **JWT Signature Validation** | Tokens cryptographically signed, tampering detected | `JwtBearerOptions` |
| **Patient-Scoped Access** | Patient ID embedded in JWT, validated on every request | `CurrentUserService`, `PatientPortalController` |

### Authorization Policies
```csharp
options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
options.AddPolicy("ClinicalStaff", policy => policy.RequireRole(AppRoles.Doctor, AppRoles.Nurse));
options.AddPolicy("DoctorOnly", policy => policy.RequireRole(AppRoles.Doctor));
options.AddPolicy("PatientOnly", policy => policy.RequireRole(AppRoles.Patient));
```

### Patient Portal Isolation
```csharp
// PatientPortalController.cs
if (!_current.PatientId.HasValue)
{
    return Forbid();
}
// Patient can ONLY see their own records - PatientId comes from validated JWT
var patientId = _current.PatientId.Value;
```

---

## 3. Data Leakage or Breach

### Threat Description
Exposure of sensitive patient records due to insecure storage, unencrypted transmission, or API vulnerabilities.

### Attack Scenarios
- Database breach exposing medical records
- Man-in-the-middle attack intercepting data in transit
- SQL injection to extract data
- Verbose error messages revealing system internals

### Implemented Mitigations

| Control | Implementation | Location |
|---------|----------------|----------|
| **Encryption at Rest (AES-256-GCM)** | Diagnosis, prescriptions, clinical notes encrypted before storage | `AesGcmFieldEncryptionService` |
| **Encryption in Transit (HTTPS)** | Enforced via HSTS in production | `Program.cs` - `UseHttpsRedirection()`, `UseHsts()` |
| **Parameterized Queries** | EF Core prevents SQL injection | All database operations |
| **Minimal Error Exposure** | ProblemDetails pattern, no stack traces in production | `AddProblemDetails()` |
| **Separate Key Management** | Encryption key stored in configuration, not database | `appsettings.json` |

### Encryption Implementation
```csharp
// AesGcmFieldEncryptionService.cs
public string EncryptToBase64(string plaintext)
{
    var nonce = RandomNumberGenerator.GetBytes(12);
    using (var aes = new AesGcm(_key, 16))
    {
        aes.Encrypt(nonce, pt, ct, tag);
    }
    // Returns: nonce + tag + ciphertext (base64)
}
```

### Database Schema
```
MedicalRecords Table:
- DiagnosisEnc (encrypted)
- PrescriptionsEnc (encrypted)
- ClinicalNotesEnc (encrypted)
```

---

## 4. Record Tampering

### Threat Description
Unauthorized modification or deletion of medical records, compromising the integrity and legal validity of health data.

### Attack Scenarios
- Doctor attempts to modify past diagnosis
- Admin tries to delete audit logs to cover tracks
- Direct database manipulation to alter records

### Implemented Mitigations

| Control | Implementation | Location |
|---------|----------------|----------|
| **Append-Only Records** | Medical records cannot be updated or deleted at application level | `AppDbContext.EnforceAppendOnly()` |
| **Immutable Audit Logs** | Audit logs cannot be modified or deleted | `AppDbContext.EnforceAppendOnly()` |
| **Server-Side Validation** | All data validated before persistence | Controllers, EF Core |
| **Complete History Preserved** | New records added, old records remain intact | Design pattern |

### Append-Only Enforcement
```csharp
// AppDbContext.cs
private void EnforceAppendOnly()
{
    foreach (var entry in ChangeTracker.Entries())
    {
        if (entry.Entity is MedicalRecord || entry.Entity is AuditLog)
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    "Medical records and audit logs are append-only and cannot be modified or deleted.");
            }
        }
    }
}
```

---

## 5. Insider Misuse

### Threat Description
Authorized personnel (doctors, nurses, admins) access patient data without legitimate medical need, violating patient privacy.

### Attack Scenarios
- Curious employee looks up celebrity patient records
- Doctor accesses ex-spouse's medical history
- Admin exports patient data for personal use

### Implemented Mitigations

| Control | Implementation | Location |
|---------|----------------|----------|
| **Comprehensive Audit Logging** | Every read/write operation logged with actor identity | `AuditService` |
| **Consent-Based Access** | Patients control who can view their records | `ConsentService` |
| **Role-Based Access Control** | Minimum necessary access per role | Authorization policies |
| **Patient Notifications** | Patients notified when records are accessed/modified | `NotificationService` |

### Audit Log Fields
```csharp
public sealed class AuditLog
{
    public Guid? ActorUserId { get; set; }
    public string ActorEmail { get; set; }
    public string ActorRoles { get; set; }
    public Guid? PatientId { get; set; }
    public string Action { get; set; }      // e.g., "RECORD_READ", "RECORD_ADD"
    public string Resource { get; set; }    // e.g., "records/{patientId}"
    public DateTime TimestampUtc { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
}
```

### Consent Enforcement
```csharp
// ConsentService.cs
public async Task<bool> CanClinicalStaffViewAsync(Guid userId, IEnumerable<string> roles, Guid patientId)
{
    var consent = await _db.Consents.AsNoTracking()
        .SingleOrDefaultAsync(x => x.PatientId == patientId);
    
    if (roles.Contains(AppRoles.Doctor) && consent.AllowDoctors) return true;
    if (roles.Contains(AppRoles.Nurse) && consent.AllowNurses) return true;
    
    return false;
}
```

---

## 6. Patient Data Overexposure

### Threat Description
Systems expose more patient data than necessary, increasing risk if any component is compromised.

### Attack Scenarios
- API returns full patient history when only summary needed
- All fields returned when only specific data requested
- Cross-patient data leakage through insecure queries

### Implemented Mitigations

| Control | Implementation | Location |
|---------|----------------|----------|
| **Patient-Scoped Queries** | Queries always filter by PatientId | All controllers |
| **DTO Pattern** | Response objects contain only necessary fields | `Contracts/` folder |
| **Decrypt-After-Authorization** | Data decrypted only after auth checks pass | Controllers |
| **Read-Only Patient Portal** | Patients cannot modify any data | `PatientPortalController` - GET only |

### Query Scoping Example
```csharp
// PatientPortalController.cs
var records = await _db.MedicalRecords.AsNoTracking()
    .Where(x => x.PatientId == patientId)  // Scoped to patient
    .OrderByDescending(x => x.CreatedAtUtc)
    .ToListAsync();
```

---

## Security Architecture Summary

```
                    ┌─────────────────────────────────────────────────────┐
                    │                    FRONTEND                         │
                    │   (HTML/JS - No sensitive logic, display only)     │
                    └─────────────────────┬───────────────────────────────┘
                                          │ HTTPS
                    ┌─────────────────────▼───────────────────────────────┐
                    │                 ASP.NET CORE API                    │
                    │  ┌──────────────────────────────────────────────┐   │
                    │  │           JWT Authentication                 │   │
                    │  │     (Validate signature, expiry, claims)     │   │
                    │  └──────────────────────────────────────────────┘   │
                    │  ┌──────────────────────────────────────────────┐   │
                    │  │         Authorization Policies               │   │
                    │  │  (AdminOnly, DoctorOnly, PatientOnly, etc.)  │   │
                    │  └──────────────────────────────────────────────┘   │
                    │  ┌──────────────────────────────────────────────┐   │
                    │  │          Consent Enforcement                 │   │
                    │  │    (Check patient consent before access)     │   │
                    │  └──────────────────────────────────────────────┘   │
                    │  ┌──────────────────────────────────────────────┐   │
                    │  │            Audit Logging                     │   │
                    │  │      (Log every read/write operation)        │   │
                    │  └──────────────────────────────────────────────┘   │
                    │  ┌──────────────────────────────────────────────┐   │
                    │  │         AES-256-GCM Encryption               │   │
                    │  │   (Encrypt sensitive fields before storage)  │   │
                    │  └──────────────────────────────────────────────┘   │
                    └─────────────────────┬───────────────────────────────┘
                                          │
                    ┌─────────────────────▼───────────────────────────────┐
                    │               SQL SERVER DATABASE                   │
                    │  ┌──────────────────────────────────────────────┐   │
                    │  │  Encrypted Fields (DiagnosisEnc, etc.)       │   │
                    │  │  Append-Only Tables (MedicalRecords, Audit)  │   │
                    │  └──────────────────────────────────────────────┘   │
                    └─────────────────────────────────────────────────────┘
```

---

## Compliance Considerations

This system design addresses key requirements from:

| Standard | Addressed Controls |
|----------|-------------------|
| **HIPAA** | Access controls, audit trails, encryption, minimum necessary |
| **GDPR** | Consent management, data minimization, right to access |
| **HITECH** | Breach notification (via notification system), audit logging |

---

## Testing Attack Scenarios

### Test 1: Privilege Escalation
1. Login as Patient (john.smith@ehr.local)
2. Try to access `/api/admin/users` → Should get 403 Forbidden
3. Try to POST to `/api/records/{patientId}` → Should get 403 Forbidden

### Test 2: Cross-Patient Access
1. Login as Patient A
2. Try to access Patient B's records via `/api/portal/records` → Only sees own records

### Test 3: Consent Enforcement
1. Login as Patient, disable nurse consent
2. Login as Nurse, try to view that patient's records → Should get 403 Forbidden

### Test 4: Audit Trail Verification
1. Perform various operations as different users
2. Login as Admin, view audit logs → All operations logged with details

### Test 5: Record Immutability
1. Add a medical record as Doctor
2. Attempt to modify via direct API call → Should fail
3. Record remains unchanged in database

---

## Viva Discussion Points

1. **Why AES-256-GCM?** - Authenticated encryption, prevents tampering, industry standard
2. **Why JWT over sessions?** - Stateless, scalable, contains claims for authorization
3. **Why append-only?** - Legal requirement for medical records, audit compliance
4. **Why consent model?** - Patient autonomy, HIPAA/GDPR requirements
5. **Why server-side authorization?** - Client can be bypassed, server is trusted boundary

---

*Document prepared for EhrSecure Academic Evaluation*
