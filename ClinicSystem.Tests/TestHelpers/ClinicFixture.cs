using AutoMapper;
using ClinicSystem.Application.Implementations;
using ClinicSystem.Application.Settings;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Infrastructure;
using ClinicSystem.Infrastructure.Data;
using ClinicSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClinicSystem.Tests.TestHelpers
{
    public class ClinicFixture
    {
        public ClinicDbContext Context { get; }
        public IMapper Mapper { get; }
        public UserManager<ApplicationUser> UserManager { get; }

        public Specialty FamilySpecialty { get; }
        public Specialty SurgerySpecialty { get; }
        public Specialty NeurologySpecialty { get; }

        public Doctor FamilyDoctor { get; }
        public Doctor Surgeon { get; }

        // ანგარიშიანი პაციენტი (ელფოსტით), ოჯახის ექიმი: FamilyDoctor
        public Patient Patient { get; }

        // პაციენტი ანგარიშის გარეშე, ოჯახის ექიმი: FamilyDoctor
        public Patient OtherPatient { get; }

        public ClinicFixture()
        {
            var options = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            Context = new ClinicDbContext(options);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => { }, typeof(SpecialtyService).Assembly);
            services.AddScoped(_ => Context);
            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ClinicDbContext>();

            var serviceProvider = services.BuildServiceProvider();
            Mapper = serviceProvider.GetRequiredService<IMapper>();
            UserManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            FamilySpecialty = new Specialty { Name = "Family medicine", IsFamilyMedicine = true };
            SurgerySpecialty = new Specialty { Name = "Surgery", RequiresReferral = true };
            NeurologySpecialty = new Specialty { Name = "Neurology", RequiresReferral = true };
            Context.Specialties.AddRange(FamilySpecialty, SurgerySpecialty, NeurologySpecialty);

            FamilyDoctor = CreateDoctor("Nino", "Beridze", "01001012345", FamilySpecialty);
            Surgeon = CreateDoctor("Davit", "Kiknadze", "01001012346", SurgerySpecialty);

            Patient = CreatePatient("Giorgi", "Kapanadze", "02002023456", FamilyDoctor, withAccount: true);
            OtherPatient = CreatePatient("Mari", "Kapanadze", "03003034567", FamilyDoctor, withAccount: false);

            AddMondaySchedule(FamilyDoctor);
            AddMondaySchedule(Surgeon);

            Context.SaveChanges();
        }

        // ---------- მონაცემების შექმნა ----------

        private Doctor CreateDoctor(string firstName, string lastName, string personalNumber, Specialty specialty)
        {
            var email = $"{firstName.ToLower()}@test.ge";
            var person = new Person { FirstName = firstName, LastName = lastName, PersonalNumber = personalNumber };
            var user = new ApplicationUser { UserName = email, Email = email, Person = person };
            person.User = user;

            var doctor = new Doctor { User = user, Specialty = specialty, IsActive = true };
            Context.Doctors.Add(doctor);
            return doctor;
        }

        private Patient CreatePatient(string firstName, string lastName, string personalNumber, Doctor familyDoctor, bool withAccount)
        {
            var person = new Person { FirstName = firstName, LastName = lastName, PersonalNumber = personalNumber };

            if (withAccount)
            {
                var email = $"{firstName.ToLower()}@test.ge";
                var user = new ApplicationUser { UserName = email, Email = email, Person = person };
                person.User = user;
            }

            var patient = new Patient
            {
                Person = person,
                BirthDate = new DateOnly(1990, 1, 1),
                FamilyDoctor = familyDoctor
            };

            Context.Patients.Add(patient);
            return patient;
        }

        private void AddMondaySchedule(Doctor doctor)
        {
            Context.DoctorSchedules.Add(new DoctorSchedule
            {
                Doctor = doctor,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(13, 0)
            });
        }

        public Referral AddReferral(Patient patient, Specialty toSpecialty, DateTime expiresAtUtc, bool isUsed = false)
        {
            var referral = new Referral
            {
                Patient = patient,
                FromDoctor = FamilyDoctor,
                ToSpecialty = toSpecialty,
                ExpiresAt = expiresAtUtc,
                IsUsed = isUsed
            };

            Context.Referrals.Add(referral);
            Context.SaveChanges();
            return referral;
        }

        public Patient AddPatientWithoutFamilyDoctor()
        {
            var patient = new Patient
            {
                Person = new Person
                {
                    FirstName = "New",
                    LastName = "Patient",
                    PersonalNumber = "04004045678"
                },
                BirthDate = new DateOnly(1995, 5, 20),
                FamilyDoctorId = null
            };

            Context.Patients.Add(patient);
            Context.SaveChanges();
            return patient;
        }

        public Appointment AddAppointment(Patient patient, Doctor doctor, DateTime start, AppointmentStatus status)
        {
            var appointment = new Appointment
            {
                Patient = patient,
                Doctor = doctor,
                StartTime = start,
                Status = status
            };

            Context.Appointments.Add(appointment);
            Context.SaveChanges();
            return appointment;
        }

        // მომავალი ორშაბათი (მინიმუმ 7 დღის შემდეგ) მითითებულ საათზე და წუთზე
        public DateTime NextMonday(int hour, int minute)
        {
            var date = DateTime.Today.AddDays(7);
            while (date.DayOfWeek != DayOfWeek.Monday)
                date = date.AddDays(1);

            return date.AddHours(hour).AddMinutes(minute);
        }

        // ---------- სერვისები რეალური რეპოზიტორიებით ----------

        public AppointmentService CreateAppointmentService() => new(
            new AppointmentRepository(Context),
            new PatientRepository(Context),
            new DoctorRepository(Context),
            new DoctorScheduleRepository(Context),
            new ReferralRepository(Context),
            new SpecialtyRepository(Context),
            new UnitOfWork(Context),
            Mapper);

        public ReferralService CreateReferralService() => new(
            new ReferralRepository(Context),
            new PatientRepository(Context),
            new DoctorRepository(Context),
            new AppointmentRepository(Context),
            new SpecialtyRepository(Context),
            new UnitOfWork(Context),
            Mapper);

        public SpecialtyService CreateSpecialtyService() => new(
            new SpecialtyRepository(Context),
            new UnitOfWork(Context),
            Mapper);

        public PatientService CreatePatientService() => new(
            new PatientRepository(Context),
            new PersonRepository(Context),
            new DoctorRepository(Context),
            UserManager,
            new UnitOfWork(Context),
            Mapper);

        public ConsultationService CreateConsultationService() => new(
            new ConsultationRepository(Context),
            new AppointmentRepository(Context),
            new PatientRepository(Context),
            new UnitOfWork(Context),
            Mapper);

        public AppointmentReminderService CreateReminderService(INotificationSender sender, int hoursBefore = 24) => new(
            new AppointmentRepository(Context),
            sender,
            new UnitOfWork(Context),
            Options.Create(new ReminderSettings { HoursBefore = hoursBefore }),
            NullLogger<AppointmentReminderService>.Instance);
    }
}
