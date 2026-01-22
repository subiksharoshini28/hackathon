# EhrSecure - Secure Electronic Health Record System

A secure EHR web application built with ASP.NET Core, Entity Framework Core, and JWT authentication for academic/hackathon evaluation.

## Features

- **Four Roles**: Admin, Doctor, Nurse, Patient
- **JWT Authentication** with role claims and expiry
- **AES-256-GCM Encryption** for medical data at rest
- **Append-Only Medical Records** (cannot be modified or deleted)
- **Patient Consent Management** (patients control who can view their records)
- **Immutable Audit Logging** (every read/write is logged)
- **Server-Side Authorization** (not just frontend)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (included with Visual Studio) or SQL Server

## Quick Start

### 1. Run the Application

```bash
cd EhrSecure.Api
dotnet run
```

The application will:
- Automatically create the database and apply migrations
- Seed the Admin user and roles
- Start the API server

### 2. Access Swagger UI

Open your browser to: **https://localhost:5001/swagger** (or the port shown in console)

### 3. Default Admin Credentials

- **Email**: `admin@ehr.local`
- **Password**: `ChangeMe!AdminPassword123$`

## API Endpoints

### Authentication
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/auth/login` | Public | Login and get JWT token |
| POST | `/api/auth/register` | Admin | Create new user with role |
| GET | `/api/auth/me` | Authenticated | Get current user info |

### Admin
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/admin/users` | Admin | List all users |
| POST | `/api/admin/users/role` | Admin | Assign role to user |
| GET | `/api/admin/audit-logs` | Admin | View audit logs |

### Patients
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/patients` | Admin | Create patient |
| GET | `/api/patients/{id}` | Admin | Get patient by ID |

### Medical Records (Append-Only)
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/records/{patientId}` | Doctor | Add medical record |
| GET | `/api/records/{patientId}` | Doctor/Nurse | View records (requires consent) |

### Patient Portal (Read-Only)
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/portal/records` | Patient | View own records |

### Consent Management
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/consents/me` | Patient | Get consent settings |
| PUT | `/api/consents/me` | Patient | Update consent settings |

## Testing the API

### 1. Login as Admin
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@ehr.local","password":"ChangeMe!AdminPassword123$"}'
```

### 2. Create a Patient (as Admin)
```bash
curl -X POST https://localhost:5001/api/patients \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"mrn":"P001","fullName":"John Doe","dateOfBirth":"1990-01-15","gender":"Male"}'
```

### 3. Create a Doctor User (as Admin)
```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email":"doctor@ehr.local","password":"DoctorPass123!@#","role":"Doctor"}'
```

### 4. Create a Patient User (as Admin)
```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email":"patient@ehr.local","password":"PatientPass123!@#","role":"Patient","patientId":"PATIENT_GUID_HERE"}'
```

## Security Controls

### Authentication
- ASP.NET Core Identity with strong password requirements
- Account lockout after 5 failed attempts (15 min)
- JWT tokens with 30-minute expiry

### Authorization
- Role-based policies enforced server-side
- Patient portal scoped to own records via JWT claims
- Consent checked before returning medical data

### Data Protection
- **In Transit**: HTTPS enforced
- **At Rest**: AES-256-GCM encryption for diagnosis, prescriptions, clinical notes
- Encryption key stored separately from database

### Integrity
- Medical records are append-only (no UPDATE/DELETE)
- Audit logs are append-only and immutable
- Full history preserved

### Audit Trail
- Every API access logged with:
  - Actor identity and roles
  - Patient ID (when applicable)
  - Action performed
  - Timestamp
  - IP address and User-Agent

## Threat Mitigations

| Threat | Mitigation |
|--------|------------|
| Stolen Credentials | Account lockout, short JWT expiry, strong passwords |
| Privilege Escalation | Server-side role enforcement, claim-scoped patient access |
| Database Breach | AES-256-GCM encryption of sensitive fields |
| Record Tampering | Append-only enforcement at application level |
| Insider Misuse | Comprehensive audit logging, consent controls |
| Data Overexposure | Minimum necessary access, consent checks |

## Project Structure

```
EhrSecure.Api/
├── Controllers/          # API endpoints
├── Contracts/            # Request/Response DTOs
├── Infrastructure/
│   ├── Auth/             # JWT services, role constants
│   ├── Audit/            # Audit logging service
│   ├── Consent/          # Consent enforcement
│   ├── Crypto/           # AES encryption service
│   ├── Entities/         # EF Core entities
│   ├── Security/         # Current user service
│   └── Seed/             # Role and admin seeding
├── Migrations/           # EF Core migrations
├── Program.cs            # Application setup
└── appsettings.json      # Configuration
```

## Configuration

Key settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=EhrSecureDb;..."
  },
  "Jwt": {
    "SigningKey": "YOUR_SECRET_KEY_AT_LEAST_32_CHARS",
    "AccessTokenMinutes": 30
  },
  "Encryption": {
    "AesKeyBase64": "BASE64_ENCODED_32_BYTE_KEY"
  },
  "Seed": {
    "AdminEmail": "admin@ehr.local",
    "AdminPassword": "YOUR_STRONG_PASSWORD"
  }
}
```

## For Viva/Evaluation

Key talking points:
1. **Defense in Depth**: Multiple layers (auth, authz, encryption, audit)
2. **Server-Side Enforcement**: Never trust frontend alone
3. **Consent-Based Access**: Patients control their data
4. **Immutability**: Medical history cannot be altered
5. **Accountability**: Complete audit trail
6. **Healthcare Standards**: Follows security best practices

## License

Academic/Educational Use
