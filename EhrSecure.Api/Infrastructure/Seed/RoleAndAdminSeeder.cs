using EhrSecure.Api.Infrastructure.Auth;
using EhrSecure.Api.Infrastructure.Crypto;
using EhrSecure.Api.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EhrSecure.Api.Infrastructure.Seed;

public static class RoleAndAdminSeeder
{
    private const string DefaultPassword = "SecurePass123!@#";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<AppDbContext>();
        var encryptionService = services.GetRequiredService<IFieldEncryptionService>();
        var seedOptions = services.GetRequiredService<IOptions<SeedOptions>>().Value;

        await EnsureRoleAsync(roleManager, AppRoles.Admin);
        await EnsureRoleAsync(roleManager, AppRoles.Doctor);
        await EnsureRoleAsync(roleManager, AppRoles.Nurse);
        await EnsureRoleAsync(roleManager, AppRoles.Patient);
        await EnsureRoleAsync(roleManager, AppRoles.Receptionist);

        var admin = await EnsureUserAsync(userManager, seedOptions.AdminEmail, seedOptions.AdminPassword, AppRoles.Admin, null);

        var existingPatients = await db.Patients.AnyAsync();
        if (existingPatients) return;

        var patient1Id = Guid.NewGuid();
        var patient2Id = Guid.NewGuid();

        var patient1 = new Patient
        {
            Id = patient1Id,
            Mrn = "P001",
            FullName = "John Smith",
            DateOfBirth = new DateOnly(1985, 3, 15),
            Gender = "Male",
            CreatedAtUtc = DateTime.UtcNow
        };

        var patient2 = new Patient
        {
            Id = patient2Id,
            Mrn = "P002",
            FullName = "Sarah Johnson",
            DateOfBirth = new DateOnly(1990, 7, 22),
            Gender = "Female",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Patients.AddRange(patient1, patient2);

        db.Consents.AddRange(
            new Consent { Id = Guid.NewGuid(), PatientId = patient1Id, AllowDoctors = true, AllowNurses = true, UpdatedAtUtc = DateTime.UtcNow },
            new Consent { Id = Guid.NewGuid(), PatientId = patient2Id, AllowDoctors = true, AllowNurses = false, UpdatedAtUtc = DateTime.UtcNow }
        );

        var doctor = await EnsureUserAsync(userManager, "doctor@ehr.local", DefaultPassword, AppRoles.Doctor, null);
        var nurse = await EnsureUserAsync(userManager, "nurse@ehr.local", DefaultPassword, AppRoles.Nurse, null);
        var receptionist = await EnsureUserAsync(userManager, "receptionist@ehr.local", DefaultPassword, AppRoles.Receptionist, null);
        var patientUser1 = await EnsureUserAsync(userManager, "john.smith@ehr.local", DefaultPassword, AppRoles.Patient, patient1Id);
        var patientUser2 = await EnsureUserAsync(userManager, "sarah.johnson@ehr.local", DefaultPassword, AppRoles.Patient, patient2Id);

        patient1.AssignedDoctorId = doctor?.Id;
        patient1.AssignedDoctorEmail = "doctor@ehr.local";
        patient2.AssignedDoctorId = doctor?.Id;
        patient2.AssignedDoctorEmail = "doctor@ehr.local";

        await db.SaveChangesAsync();

        db.MedicalRecords.AddRange(
            new MedicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patient1Id,
                DiagnosisEnc = encryptionService.EncryptToBase64("Hypertension Stage 1"),
                PrescriptionsEnc = encryptionService.EncryptToBase64("Lisinopril 10mg daily"),
                ClinicalNotesEnc = encryptionService.EncryptToBase64("Patient presents with elevated blood pressure. Advised lifestyle modifications and started on ACE inhibitor."),
                CreatedByUserId = doctor!.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30)
            },
            new MedicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patient1Id,
                DiagnosisEnc = encryptionService.EncryptToBase64("Follow-up: Hypertension controlled"),
                PrescriptionsEnc = encryptionService.EncryptToBase64("Continue Lisinopril 10mg daily"),
                ClinicalNotesEnc = encryptionService.EncryptToBase64("Blood pressure well controlled at 128/82. Continue current medication."),
                CreatedByUserId = doctor.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-7)
            },
            new MedicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patient2Id,
                DiagnosisEnc = encryptionService.EncryptToBase64("Type 2 Diabetes Mellitus"),
                PrescriptionsEnc = encryptionService.EncryptToBase64("Metformin 500mg twice daily"),
                ClinicalNotesEnc = encryptionService.EncryptToBase64("HbA1c 7.2%. Started on Metformin. Diet and exercise counseling provided."),
                CreatedByUserId = doctor.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-14)
            }
        );

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = admin?.Id,
            ActorEmail = seedOptions.AdminEmail,
            ActorRoles = AppRoles.Admin,
            Action = "SYSTEM_SEED",
            Resource = "database/seed",
            TimestampUtc = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "SystemSeeder"
        });

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role,
        Guid? patientId)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PatientId = patientId
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var msg = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user {email}: {msg}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}
