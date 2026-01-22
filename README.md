# 🏥 SAMUNNATI - Secure Electronic Health Record System

A comprehensive, secure EHR system built with ASP.NET Core, featuring end-to-end encryption, patient consent management, and role-based access control.

## 🎯 Features

### 🔐 Security
- **AES-256-GCM Encryption** for sensitive medical data
- **JWT Authentication** with role-based authorization
- **Patient Consent Management** - Patients control who can view their records
- **Immutable Audit Trail** - Complete logging of all data access
- **OTP-based Patient Login** - No password storage for patients

### 👥 User Roles
- **Admin** - User management, system configuration, audit logs
- **Doctor** - Add/view medical records, see previously attended patients
- **Nurse** - Read-only access to patient records (with consent)
- **Receptionist** - Patient registration, doctor assignment
- **Patient** - View own records, download prescriptions

### 📊 Key Features
- **MRN Support** - Login with Medical Record Number (P001, P002)
- **Secure Downloads** - Encrypted PDF/HTML generation for records
- **Doctor Dashboard** - View previously attended patients
- **Real-time Audit Logging** - Track all data access and modifications

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB included with Visual Studio)

### Installation
```bash
# Clone the repository
git clone <repository-url>
cd hackathon

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project .\EhrSecure.Api\EhrSecure.Api.csproj
```

### Access
- **Web Application**: http://localhost:5164
- **Swagger UI**: http://localhost:5164/swagger (development only)

## 👤 Login Credentials

### Staff Login
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@ehr.local | Admin123! |
| Doctor | doctor@ehr.local | Doctor123! |
| Nurse | nurse@ehr.local | Nurse123! |
| Receptionist | receptionist@ehr.local | Receptionist123! |

### Patient Login (OTP)
| MRN | Name | Instructions |
|-----|------|-------------|
| P001 | John Smith | Enter `P001` → Get OTP → Enter displayed OTP |
| P002 | Sarah Johnson | Enter `P002` → Get OTP → Enter displayed OTP |

## 🏗️ Architecture

### Technology Stack
- **Frontend**: HTML5, CSS3, JavaScript (ES6+)
- **Backend**: ASP.NET Core 8.0, Entity Framework Core
- **Database**: SQL Server with encrypted fields
- **Authentication**: JWT Bearer + ASP.NET Identity
- **Encryption**: AES-256-GCM (field-level)

### Security Architecture
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend API   │    │   Database      │
│   (HTML/JS/CSS) │◄──►│  (ASP.NET Core) │◄──►│ (SQL Server)    │
│                 │    │                 │    │                 │
│ • JWT Tokens    │    │ • RBAC          │    │ • Encrypted     │
│ • Role Display  │    │ • Consent Check │    │   Fields        │
│ • OTP Login     │    │ • Audit Logging │    │ • Audit Logs    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 📁 Project Structure

```
EhrSecure.Api/
├── Controllers/              # API endpoints
│   ├── AuthController.cs     # Authentication (JWT/OTP)
│   ├── AdminController.cs    # Admin operations
│   ├── MedicalRecordsController.cs  # Medical records CRUD
│   ├── PatientPortalController.cs    # Patient access
│   └── ReceptionistController.cs     # Patient registration
├── Infrastructure/
│   ├── Auth/                 # JWT & authentication
│   ├── Crypto/               # AES encryption services
│   ├── Entities/             # Database models
│   ├── Security/             # Consent & audit services
│   └── Seed/                 # Database seeding
├── wwwroot/
│   ├── index.html           # Main UI
│   ├── css/style.css        # Styling
│   └── js/app.js            # Frontend logic
└── Program.cs               # Application configuration
```

## 🔧 Configuration

### Database Connection
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EhrSecureDb;Trusted_Connection=true"
  }
}
```

### JWT Settings
```json
{
  "Jwt": {
    "Key": "your-256-bit-secret-key-here",
    "Issuer": "EhrSecure",
    "Audience": "EhrSecure",
    "ExpiresInMinutes": 60
  }
}
```

### Encryption Key
```json
{
  "Encryption": {
    "Key": "your-32-byte-encryption-key-here"
  }
}
```

## 🛡️ Security Features

### Field-Level Encryption
Sensitive fields are encrypted at rest:
- `DiagnosisEnc` - Encrypted diagnosis
- `PrescriptionsEnc` - Encrypted prescriptions
- `ClinicalNotesEnc` - Encrypted clinical notes

### Access Control
```csharp
[Authorize(Policy = "DoctorOnly")]      // Medical record creation
[Authorize(Policy = "ClinicalStaff")]   // Record viewing
[Authorize(Policy = "AdminOnly")]       // User management
```

### Audit Logging
All actions are logged with:
- User ID and role
- Action performed
- Resource accessed
- Timestamp
- Patient ID (if applicable)

## 📊 API Endpoints

### Authentication
- `POST /api/auth/login` - Staff login
- `POST /api/auth/request-otp` - Request OTP for patient
- `POST /api/auth/login-otp` - Patient OTP login

### Medical Records
- `GET /api/records/{patientId}` - Get patient records
- `POST /api/records/{patientId}` - Add medical record
- `GET /api/records/my-patients` - Doctor's previously attended patients

### Patient Portal
- `GET /api/portal/records` - Patient's own records
- `GET /api/portal/records/download-all` - Download all records
- `GET /api/portal/prescription/{recordId}/download` - Download prescription

### Admin
- `GET /api/admin/users` - List all users
- `POST /api/admin/users` - Create new user
- `GET /api/admin/patients` - List all patients
- `GET /api/admin/audit` - View audit logs

### Receptionist
- `POST /api/receptionist/register` - Register new patient
- `GET /api/receptionist/doctors` - List doctors
- `POST /api/receptionist/assign-doctor` - Assign doctor to patient

## 🧪 Testing

### Unit Tests
```bash
dotnet test
```

### Integration Tests
```bash
dotnet test --filter Category=Integration
```

### API Testing with Swagger
Navigate to http://localhost:5164/swagger for interactive API testing.

## 📱 Demo Scenarios

### 1. Admin Workflow
1. Login as admin
2. Create new users (doctor, nurse, receptionist)
3. View audit logs
4. Monitor system activity

### 2. Doctor Workflow
1. Login as doctor
2. View "My Patients" list
3. Search for patient by MRN
4. Add new medical record
5. View patient history

### 3. Patient Workflow
1. Login with MRN (P001)
2. View own medical records
3. Download prescriptions
4. Manage consent preferences

### 4. Receptionist Workflow
1. Register new patient
2. Assign doctor to patient
3. View patient list
4. Manage appointments

## 🔍 Troubleshooting

### Common Issues

#### Port Already in Use
```powershell
Get-NetTCPConnection -LocalPort 5164 | Stop-Process -Force
```

#### Database Connection Error
- Ensure SQL Server is running
- Check connection string in appsettings.json
- Verify SQL Server Express is installed

#### Build Errors
```bash
dotnet clean
dotnet build
```

#### OTP Not Working
- Check patient exists in database
- Verify OTP service configuration
- Check system clock synchronization

## 🚀 Deployment

### Development
```bash
dotnet run
```

### Production
```bash
dotnet publish -c Release -o ./publish
```

### Docker
```bash
docker build -t ehrsecure .
docker run -p 8080:80 ehrsecure
```

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📞 Support

For support and questions:
- Create an issue in the repository
- Contact the development team
- Check the troubleshooting section above

---

**SAMUNNATI - Securing Healthcare Data, Empowering Patients** 🏥🔒
