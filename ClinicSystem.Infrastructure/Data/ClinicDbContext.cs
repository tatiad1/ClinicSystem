using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Domain.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Data
{
    public class ClinicDbContext : IdentityDbContext<ApplicationUser>
    {
        public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options)
        {
        }

        public DbSet<Person> People => Set<Person>();
        public DbSet<Specialty> Specialties => Set<Specialty>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<MedicalProfile> MedicalProfiles => Set<MedicalProfile>();
        public DbSet<Referral> Referrals => Set<Referral>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Consultation> Consultations => Set<Consultation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ადამიანი: იდენტობა ყველასთვის
            modelBuilder.Entity<Person>(e =>
            {
                e.Property(p => p.FirstName).IsRequired().HasMaxLength(50);
                e.Property(p => p.LastName).IsRequired().HasMaxLength(50);
                e.Property(p => p.PersonalNumber).IsRequired().HasMaxLength(11).IsFixedLength();
                e.Property(p => p.Phone).HasMaxLength(20);
                e.HasIndex(p => p.PersonalNumber).IsUnique();

                // პირადი ნომერი: ზუსტად 11 ციფრი
                e.ToTable(t => t.HasCheckConstraint(
                    "CK_People_PersonalNumber",
                    "LEN([PersonalNumber]) = 11 AND [PersonalNumber] NOT LIKE '%[^0-9]%'"));

                // Person 1:1 ApplicationUser
                e.HasOne(p => p.User)
                    .WithOne(u => u.Person)
                    .HasForeignKey<ApplicationUser>(u => u.PersonId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Person 1:1 Patient
                e.HasOne(p => p.Patient)
                    .WithOne(pt => pt.Person)
                    .HasForeignKey<Patient>(pt => pt.PersonId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Specialty>(e =>
            {
                e.Property(s => s.Name).IsRequired().HasMaxLength(100);
                e.HasIndex(s => s.Name).IsUnique();
                e.ToTable(t => t.HasCheckConstraint(
                    "CK_Specialties_FamilyMedicine_NoReferral",
                    "NOT ([IsFamilyMedicine] = 1 AND [RequiresReferral] = 1)"));
            });

            modelBuilder.Entity<Doctor>(e =>
            {
                e.HasIndex(d => d.UserId).IsUnique();

                e.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.Specialty)
                    .WithMany(s => s.Doctors)
                    .HasForeignKey(d => d.SpecialtyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DoctorSchedule>(e =>
            {
                // დასრულება დაწყებაზე გვიან უნდა იყოს
                e.ToTable(t => t.HasCheckConstraint(
                    "CK_DoctorSchedules_EndAfterStart", "[EndTime] > [StartTime]"));

                // ერთსა და იმავე დღეს ერთი და იგივე დაწყების დროით ორი ჩანაწერი არ იქნება
                e.HasIndex(s => new { s.DoctorId, s.DayOfWeek, s.StartTime }).IsUnique();

                e.HasOne(s => s.Doctor)
                    .WithMany(d => d.Schedules)
                    .HasForeignKey(s => s.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Patient>(e =>
            {
                e.Ignore(p => p.IsRegistered); // გამოთვლილი, ბაზაში არ ინახება
                e.Property(p => p.BirthDate).IsRequired();

                e.HasOne(p => p.FamilyDoctor)
                    .WithMany(d => d.FamilyPatients)
                    .HasForeignKey(p => p.FamilyDoctorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MedicalProfile>(e =>
            {
                e.Property(m => m.BloodType).HasMaxLength(3);
                e.Property(m => m.Allergies).HasMaxLength(1000);
                e.Property(m => m.ChronicConditions).HasMaxLength(1000);

                // სისხლის ჯგუფი მხოლოდ დასაშვები მნიშვნელობებიდან
                e.ToTable(t => t.HasCheckConstraint(
                    "CK_MedicalProfiles_BloodType",
                    "[BloodType] IS NULL OR [BloodType] IN ('A+','A-','B+','B-','AB+','AB-','O+','O-')"));

                e.HasOne(m => m.Patient)
                    .WithOne(p => p.MedicalProfile)
                    .HasForeignKey<MedicalProfile>(m => m.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Referral>(e =>
            {
                e.Property(r => r.Reason).HasMaxLength(500);
                e.Property(r => r.ExpiresAt).IsRequired();

                // ვადა შექმნის შემდეგ უნდა იწურებოდეს
                e.ToTable(t => t.HasCheckConstraint(
                    "CK_Referrals_ExpiresAfterCreated", "[ExpiresAt] > [CreatedAt]"));

                e.HasOne(r => r.Patient)
                    .WithMany(p => p.Referrals)
                    .HasForeignKey(r => r.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.FromDoctor)
                    .WithMany()
                    .HasForeignKey(r => r.FromDoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.ToSpecialty)
                    .WithMany()
                    .HasForeignKey(r => r.ToSpecialtyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Appointment>(e =>
            {
                e.Property(a => a.StartTime).IsRequired();
                e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

                // ექიმს ერთ დროს ერთი აქტიური ვიზიტი
                e.HasIndex(a => new { a.DoctorId, a.StartTime })
                    .IsUnique()
                    .HasFilter("[Status] IN ('Pending', 'Approved')");

                // პაციენტსაც ერთ დროს ერთი აქტიური ვიზიტი
                e.HasIndex(a => new { a.PatientId, a.StartTime })
                    .IsUnique()
                    .HasFilter("[Status] IN ('Pending', 'Approved')");

                // ერთი მიმართვა მხოლოდ ერთ აქტიურ ვიზიტზე (პარალელური ჩაწერისგან დასაცავად)
                e.HasIndex(a => a.ReferralId)
                    .IsUnique()
                    .HasFilter("[ReferralId] IS NOT NULL AND [Status] IN ('Pending', 'Approved')");

                e.HasOne(a => a.Patient)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.Doctor)
                    .WithMany(d => d.Appointments)
                    .HasForeignKey(a => a.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.Referral)
                    .WithMany()
                    .HasForeignKey(a => a.ReferralId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Consultation>(e =>
            {
                e.Property(c => c.Diagnosis).IsRequired().HasMaxLength(1000);
                e.Property(c => c.Prescription).HasMaxLength(1000);

                e.HasOne(c => c.Appointment)
                    .WithOne(a => a.Consultation)
                    .HasForeignKey<Consultation>(c => c.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public override int SaveChanges()
        {
            ApplyAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditFields()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // CreatedAt განახლებისას არასდროს იცვლება
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }
}
