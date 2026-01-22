using EhrSecure.Api.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Infrastructure;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Consent> Consents => Set<Consent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Otp> Otps => Set<Otp>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Patient>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Mrn).IsUnique();
            b.Property(x => x.Mrn).HasMaxLength(64).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Gender).HasMaxLength(50);
        });

        builder.Entity<MedicalRecord>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PatientId, x.CreatedAtUtc });
            b.Property(x => x.DiagnosisEnc).IsRequired();
            b.Property(x => x.PrescriptionsEnc).IsRequired();
            b.Property(x => x.ClinicalNotesEnc).IsRequired();
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Consent>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PatientId).IsUnique();
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.TimestampUtc);
            b.Property(x => x.Action).HasMaxLength(100).IsRequired();
            b.Property(x => x.Resource).HasMaxLength(200).IsRequired();
            b.Property(x => x.ActorEmail).HasMaxLength(256);
            b.Property(x => x.ActorRoles).HasMaxLength(400);
            b.Property(x => x.IpAddress).HasMaxLength(100);
            b.Property(x => x.UserAgent).HasMaxLength(400);
        });

        builder.Entity<ApplicationUser>(b =>
        {
            b.HasIndex(x => x.PatientId);
        });

        builder.Entity<Otp>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PatientId, x.Code });
            b.Property(x => x.Code).HasMaxLength(10).IsRequired();
        });

        builder.Entity<Notification>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PatientId, x.IsRead, x.CreatedAtUtc });
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Type).HasMaxLength(50).IsRequired();
            b.Property(x => x.TriggeredByEmail).HasMaxLength(256);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceAppendOnly()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is MedicalRecord || entry.Entity is AuditLog)
            {
                if (entry.State is EntityState.Modified or EntityState.Deleted)
                {
                    throw new InvalidOperationException("Medical records and audit logs are append-only and cannot be modified or deleted.");
                }
            }
        }
    }
}
