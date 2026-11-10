using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Web.Seeding;

/// <summary>
/// Adds a presentation-ready sample clinic to an empty Development database.
/// It deliberately does not send welcome or appointment emails.
/// </summary>
public sealed class DevelopmentDataSeeder
{
    public const string DemoPassword = "ClinicDemo123!";

    private readonly ClinicDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        ClinicDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        // One database check is enough: this seeder runs only when the clinic
        // catalogue is empty, then inserts the whole demo dataset in one pass.
        if (await _db.Specialties.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Demo clinic data already exists; sample seeding was skipped.");
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var familyMedicine = new Specialty
            {
                Name = "ოჯახის მედიცინა",
                IsFamilyMedicine = true,
                RequiresReferral = false
            };
            var cardiology = new Specialty
            {
                Name = "კარდიოლოგია",
                RequiresReferral = true
            };
            var dermatology = new Specialty
            {
                Name = "დერმატოლოგია",
                RequiresReferral = false
            };
            var pediatrics = new Specialty
            {
                Name = "პედიატრია",
                RequiresReferral = true
            };

            _db.Specialties.AddRange(familyMedicine, cardiology, dermatology, pediatrics);
            await _db.SaveChangesAsync(cancellationToken);

            // Staff accounts are created through UserManager so Identity hashes
            // each password and stores the role membership correctly.
            await CreateAccountAsync("admin.demo@clinic.test", "ნინო", "ადმინისტრატორი", 3, 1, Roles.Admin, cancellationToken);
            await CreateAccountAsync("receptionist.demo@clinic.test", "მარიამ", "რეგისტრატორი", 3, 2, Roles.Receptionist, cancellationToken);

            var doctorSpecs = new[]
            {
                (Email: "doctor.family1@clinic.test", First: "ანა", Last: "გელაშვილი", Phone: "599100001", Specialty: familyMedicine),
                (Email: "doctor.family2@clinic.test", First: "გიორგი", Last: "მაისურაძე", Phone: "599100002", Specialty: familyMedicine),
                (Email: "doctor.cardio@clinic.test", First: "თამარ", Last: "ლომიძე", Phone: "599100003", Specialty: cardiology),
                (Email: "doctor.derma@clinic.test", First: "ირაკლი", Last: "ბერიძე", Phone: "599100004", Specialty: dermatology),
                (Email: "doctor.pediatrics@clinic.test", First: "ეკა", Last: "ჯაფარიძე", Phone: "599100005", Specialty: pediatrics)
            };

            var doctors = new List<Doctor>();
            for (var i = 0; i < doctorSpecs.Length; i++)
            {
                var spec = doctorSpecs[i];
                var user = await CreateAccountAsync(
                    spec.Email, spec.First, spec.Last, 2, i + 1, Roles.Doctor, cancellationToken, spec.Phone);
                var doctor = new Doctor { UserId = user.Id, SpecialtyId = spec.Specialty.Id };
                doctors.Add(doctor);
                _db.Doctors.Add(doctor);
            }

            await _db.SaveChangesAsync(cancellationToken);

            // All doctors have weekday hours, so the sample includes a useful
            // range of open appointment slots no matter which day it is opened.
            foreach (var doctor in doctors)
            {
                foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
                {
                    _db.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId = doctor.Id,
                        DayOfWeek = day,
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(17, 0)
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            var patientDetails = new[]
            {
                ("ლუკა", "აბაშიძე", "599200001", "A+", "პენიცილინი", "—"),
                ("სალომე", "გოგოლაძე", "599200002", "O+", "—", "ასთმა"),
                ("ნიკა", "კაპანაძე", "599200003", "B+", "თხილი", "—"),
                ("ელენე", "მიქაძე", "599200004", "AB+", "—", "—"),
                ("დემეტრე", "ნოზაძე", "599200005", "O-", "—", "დიაბეტი ტიპი 1"),
                ("თინათინ", "გურგენიძე", "599200006", "A-", "—", "—"),
                ("სანდრო", "ქავთარაძე", "599200007", "B-", "—", "ჰიპერტენზია"),
                ("ანა", "მელაძე", "599200008", "O+", "ციტრუსი", "—"),
                ("მარი", "ხუციშვილი", "599200009", "—", "—", "—"),
                ("ლევან", "ჩხეიძე", "599200010", "—", "—", "—"),
                ("ნატა", "გოგუაძე", "599200011", "—", "—", "—"),
                ("გაბრიელ", "კიკნაძე", "—", "—", "—", "—")
            };

            var patients = new List<Patient>();
            for (var i = 0; i < patientDetails.Length; i++)
            {
                var detail = patientDetails[i];
                var contactEmail = $"patient{i + 1:00}@clinic.test";
                var person = new Person
                {
                    FirstName = detail.Item1,
                    LastName = detail.Item2,
                    PersonalNumber = PersonalNumber(1, i + 1),
                    Phone = detail.Item3 == "—" ? null : detail.Item3,
                    ContactEmail = contactEmail
                };

                if (i < 5)
                {
                    var user = new ApplicationUser
                    {
                        UserName = contactEmail,
                        Email = contactEmail,
                        EmailConfirmed = true,
                        Person = person
                    };
                    await CreateUserAsync(user, Roles.Patient, cancellationToken);
                }
                else
                {
                    _db.People.Add(person);
                }

                // Eight patients have a family doctor; the remaining four
                // demonstrate the flow for people who are not registered yet.
                var patient = new Patient
                {
                    Person = person,
                    BirthDate = new DateOnly(1985 + (i % 20), (i % 12) + 1, (i % 26) + 1),
                    FamilyDoctorId = i < 8 ? doctors[i % 2].Id : null
                };
                patients.Add(patient);
                _db.Patients.Add(patient);

                // Keep some profiles absent so the UI can demonstrate both cases.
                if (i < 8 && detail.Item4 != "—")
                {
                    _db.MedicalProfiles.Add(new MedicalProfile
                    {
                        Patient = patient,
                        BloodType = detail.Item4,
                        Allergies = detail.Item5 == "—" ? null : detail.Item5,
                        ChronicConditions = detail.Item6 == "—" ? null : detail.Item6
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            var referrals = new List<Referral>
            {
                NewReferral(patients[0], doctors[0], cardiology, "კარდიოლოგთან გეგმური შემოწმება", used: true),
                NewReferral(patients[1], doctors[1], cardiology, "გულის რიტმის შეფასება", used: true),
                NewReferral(patients[2], doctors[0], pediatrics, "ბავშვის დამატებითი გამოკვლევა", used: true),
                NewReferral(patients[3], doctors[1], pediatrics, "პედიატრის კონსულტაცია", used: true),
                NewReferral(patients[4], doctors[0], cardiology, "აქტიური, ჯერ გამოუყენებელი მიმართვა", used: false),
                NewReferral(patients[5], doctors[1], cardiology, "მიმართვა მოქმედების ვადით", used: false)
            };
            _db.Referrals.AddRange(referrals);
            await _db.SaveChangesAsync(cancellationToken);

            var appointments = new List<Appointment>();

            // Future booked visits. Referral-required specialties use a valid
            // matching referral; family medicine and dermatology do not.
            AddAppointment(appointments, patients[0], doctors[2], FutureAt(DayOfWeek.Monday, 9, 0), AppointmentStatus.Booked, referrals[0]);
            AddAppointment(appointments, patients[1], doctors[2], FutureAt(DayOfWeek.Tuesday, 9, 0), AppointmentStatus.Booked, referrals[1]);
            AddAppointment(appointments, patients[2], doctors[4], FutureAt(DayOfWeek.Wednesday, 9, 30), AppointmentStatus.Booked, referrals[2]);
            AddAppointment(appointments, patients[3], doctors[4], FutureAt(DayOfWeek.Thursday, 9, 0), AppointmentStatus.Booked, referrals[3]);
            AddAppointment(appointments, patients[4], doctors[0], FutureAt(DayOfWeek.Monday, 10, 0), AppointmentStatus.Booked);
            AddAppointment(appointments, patients[5], doctors[1], FutureAt(DayOfWeek.Tuesday, 10, 0), AppointmentStatus.Booked);
            AddAppointment(appointments, patients[6], doctors[3], FutureAt(DayOfWeek.Wednesday, 10, 0), AppointmentStatus.Booked);
            AddAppointment(appointments, patients[7], doctors[3], FutureAt(DayOfWeek.Friday, 10, 0), AppointmentStatus.Booked);
            AddAppointment(appointments, patients[8], doctors[0], FutureAt(DayOfWeek.Thursday, 11, 0), AppointmentStatus.Booked);
            AddAppointment(appointments, patients[9], doctors[1], FutureAt(DayOfWeek.Friday, 11, 0), AppointmentStatus.Booked);

            // Past visits cover completed, no-show and cancelled states.
            var completed1 = AddAppointment(appointments, patients[0], doctors[0], PastAt(DayOfWeek.Monday, 9, 0), AppointmentStatus.Completed);
            var completed2 = AddAppointment(appointments, patients[1], doctors[1], PastAt(DayOfWeek.Tuesday, 10, 0), AppointmentStatus.Completed);
            var completed3 = AddAppointment(appointments, patients[2], doctors[0], PastAt(DayOfWeek.Wednesday, 11, 0), AppointmentStatus.Completed);
            AddAppointment(appointments, patients[10], doctors[1], PastAt(DayOfWeek.Thursday, 12, 0), AppointmentStatus.NoShow);
            AddAppointment(appointments, patients[11], doctors[0], PastAt(DayOfWeek.Friday, 13, 0), AppointmentStatus.Cancelled);

            _db.Appointments.AddRange(appointments);
            await _db.SaveChangesAsync(cancellationToken);

            _db.Consultations.AddRange(
                new Consultation { AppointmentId = completed1.Id, Diagnosis = "საკონტროლო შემოწმება", Prescription = "რეკომენდებულია გეგმური დაკვირვება." },
                new Consultation { AppointmentId = completed2.Id, Diagnosis = "სეზონური ალერგია", Prescription = "ალერგენთან კონტაქტის შემცირება." },
                new Consultation { AppointmentId = completed3.Id, Diagnosis = "ვირუსული ინფექციის შემდგომი კონტროლი", Prescription = "სითხის მიღება და დასვენება." });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Seeded demo clinic data: {Specialties} specialties, {Doctors} doctors, {Patients} patients, {Appointments} appointments.",
                4, doctors.Count, patients.Count, appointments.Count);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ApplicationUser> CreateAccountAsync(
        string email,
        string firstName,
        string lastName,
        int personalNumberGroup,
        int sequence,
        string role,
        CancellationToken cancellationToken,
        string? phone = null)
    {
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            PersonalNumber = PersonalNumber(personalNumberGroup, sequence),
            Phone = phone ?? $"599300{sequence:000}"
        };
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Person = person
        };

        await CreateUserAsync(user, role, cancellationToken);
        return user;
    }

    private async Task CreateUserAsync(ApplicationUser user, string role, CancellationToken cancellationToken)
    {
        var createResult = await _userManager.CreateAsync(user, DemoPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create demo account '{user.Email}': {errors}");
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not assign role '{role}' to '{user.Email}': {errors}");
        }
    }

    private static Referral NewReferral(Patient patient, Doctor familyDoctor, Specialty target, string reason, bool used) => new()
    {
        Patient = patient,
        FromDoctorId = familyDoctor.Id,
        ToSpecialtyId = target.Id,
        Reason = reason,
        ExpiresAt = DateTime.UtcNow.AddDays(60),
        IsUsed = used
    };

    private static Appointment AddAppointment(
        ICollection<Appointment> appointments,
        Patient patient,
        Doctor doctor,
        DateTime start,
        AppointmentStatus status,
        Referral? referral = null)
    {
        var appointment = new Appointment
        {
            Patient = patient,
            Doctor = doctor,
            StartTime = start,
            Status = status,
            Referral = referral
        };
        appointments.Add(appointment);
        return appointment;
    }

    private static DateTime FutureAt(DayOfWeek day, int hour, int minute)
    {
        var date = DateTime.Today.AddDays(4);
        while (date.DayOfWeek != day)
            date = date.AddDays(1);

        return date.AddHours(hour).AddMinutes(minute);
    }

    private static DateTime PastAt(DayOfWeek day, int hour, int minute)
    {
        var date = DateTime.Today.AddDays(-1);
        while (date.DayOfWeek != day)
            date = date.AddDays(-1);

        return date.AddHours(hour).AddMinutes(minute);
    }

    private static string PersonalNumber(int group, int sequence) => $"{group}{sequence:0000000000}";
}
