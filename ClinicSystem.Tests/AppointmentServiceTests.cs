using ClinicSystem.Application.Implementations;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Tests.TestHelpers;

namespace ClinicSystem.Tests
{
    public class AppointmentServiceTests
    {
        private static Task<AppointmentDTO> BookAsync(
            AppointmentService service, int patientId, int doctorId, DateTime start, int? referralId = null)
        {
            return service.AddAsync(new CreateAppointmentDTO
            {
                PatientId = patientId,
                DoctorId = doctorId,
                StartTime = start,
                ReferralId = referralId
            }, CancellationToken.None);
        }

        // ---------- ჩაწერა: დრო და გრაფიკი ----------

        [Fact]
        public async Task AddAsync_FamilyDoctorWithoutReferral_CreatesApprovedAppointment()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();

            var result = await BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, f.NextMonday(10, 0));

            Assert.Equal(AppointmentStatus.Approved, result.Status);
            Assert.Equal(f.FamilyDoctor.Id, result.DoctorId);
            Assert.Equal(f.Patient.Id, result.PatientId);
        }

        [Fact]
        public async Task AddAsync_TimeNotOnSlotGrid_ThrowsArgumentException()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, f.NextMonday(10, 15)));
        }

        [Fact]
        public async Task AddAsync_TimeInPast_ThrowsArgumentException()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, DateTime.Now.AddDays(-1)));
        }

        [Fact]
        public async Task AddAsync_OutsideDoctorSchedule_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();

            // გრაფიკი 09:00–13:00-ია, 15:00 მის გარეთაა
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, f.NextMonday(15, 0)));

            Assert.Contains("does not work", ex.Message);
        }

        // ---------- ჩაწერა: კონფლიქტები ----------

        [Fact]
        public async Task AddAsync_DoctorAlreadyBooked_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var time = f.NextMonday(10, 0);

            await BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, time);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BookAsync(service, f.OtherPatient.Id, f.FamilyDoctor.Id, time));

            Assert.Contains("Doctor already has", ex.Message);
        }

        [Fact]
        public async Task AddAsync_PatientAlreadyBookedAtSameTime_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var time = f.NextMonday(10, 0);

            await BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, time);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BookAsync(service, f.Patient.Id, f.Surgeon.Id, time));

            Assert.Contains("Patient already has", ex.Message);
        }

        // ---------- ჩაწერა: მიმართვა ----------

        [Fact]
        public async Task AddAsync_OtherSpecialistWithoutReferral_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BookAsync(service, f.Patient.Id, f.Surgeon.Id, f.NextMonday(11, 0)));

            Assert.Contains("referral", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AddAsync_LegacyNonFamilyDoctorAssignment_DoesNotBypassReferralRequirement()
        {
            var f = new ClinicFixture();
            f.Patient.FamilyDoctorId = f.Surgeon.Id;
            f.Context.SaveChanges();
            var service = f.CreateAppointmentService();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BookAsync(service, f.Patient.Id, f.Surgeon.Id, f.NextMonday(11, 0)));

            Assert.Contains("referral", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AddAsync_WithValidReferral_CreatesAppointmentAndMarksReferralUsed()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var referral = f.AddReferral(f.Patient, f.SurgerySpecialty, DateTime.UtcNow.AddDays(10));

            var result = await BookAsync(service, f.Patient.Id, f.Surgeon.Id, f.NextMonday(11, 0), referral.Id);

            Assert.Equal(referral.Id, result.ReferralId);
            Assert.True(referral.IsUsed);
        }

        [Fact]
        public async Task AddAsync_ExpiredReferral_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var referral = f.AddReferral(f.Patient, f.SurgerySpecialty, DateTime.UtcNow.AddDays(-1));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BookAsync(service, f.Patient.Id, f.Surgeon.Id, f.NextMonday(11, 0), referral.Id));

            Assert.Contains("expired", ex.Message);
        }

        [Fact]
        public async Task AddAsync_AlreadyUsedReferral_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var referral = f.AddReferral(f.Patient, f.SurgerySpecialty, DateTime.UtcNow.AddDays(10), isUsed: true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BookAsync(service, f.Patient.Id, f.Surgeon.Id, f.NextMonday(11, 0), referral.Id));

            Assert.Contains("already used", ex.Message);
        }

        [Fact]
        public async Task AddAsync_ReferralForDifferentSpecialty_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();

            // მიმართვა არის ოჯახის მედიცინაზე, ჩაწერა კი ქირურგთან
            var referral = f.AddReferral(f.Patient, f.FamilySpecialty, DateTime.UtcNow.AddDays(10));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BookAsync(service, f.Patient.Id, f.Surgeon.Id, f.NextMonday(11, 0), referral.Id));

            Assert.Contains("different specialty", ex.Message);
        }

        // ---------- სტატუსები ----------

        [Fact]
        public async Task ApproveAsync_PendingAppointment_BecomesApproved()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var pending = f.AddAppointment(
                f.Patient,
                f.FamilyDoctor,
                f.NextMonday(10, 0),
                AppointmentStatus.Pending);

            await service.ApproveAsync(pending.Id, CancellationToken.None);

            Assert.Equal(AppointmentStatus.Approved, f.Context.Appointments.Single().Status);
        }

        [Fact]
        public async Task ApproveAsync_AlreadyApproved_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var booked = await BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, f.NextMonday(10, 0));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ApproveAsync(booked.Id, CancellationToken.None));
        }

        [Fact]
        public async Task CancelAsync_AppointmentWithReferral_ReleasesReferral()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var referral = f.AddReferral(f.Patient, f.SurgerySpecialty, DateTime.UtcNow.AddDays(10));
            var booked = await BookAsync(service, f.Patient.Id, f.Surgeon.Id, f.NextMonday(11, 0), referral.Id);
            Assert.True(referral.IsUsed);

            await service.CancelAsync(booked.Id, CancellationToken.None);

            Assert.Equal(AppointmentStatus.Cancelled, f.Context.Appointments.Single().Status);
            Assert.False(referral.IsUsed);
        }

        [Fact]
        public async Task MarkNoShowAsync_BeforeAppointmentTime_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var booked = await BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, f.NextMonday(10, 0));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MarkNoShowAsync(booked.Id, CancellationToken.None));
        }

        [Fact]
        public async Task MarkNoShowAsync_BeforeAppointmentEndAndGracePeriod_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var appointment = f.AddAppointment(
                f.Patient,
                f.FamilyDoctor,
                DateTime.Now.AddMinutes(-40),
                AppointmentStatus.Approved);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MarkNoShowAsync(appointment.Id, CancellationToken.None));

            Assert.Contains("grace period", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task MarkNoShowAsync_AfterAppointmentEndAndGracePeriod_MarksNoShow()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var appointment = f.AddAppointment(
                f.Patient,
                f.FamilyDoctor,
                DateTime.Now.AddMinutes(-46),
                AppointmentStatus.Approved);

            await service.MarkNoShowAsync(appointment.Id, CancellationToken.None);

            Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
        }

        // ---------- თავისუფალი დროები ----------

        [Fact]
        public async Task GetAvailableSlotsAsync_ExcludesBookedSlot()
        {
            var f = new ClinicFixture();
            var service = f.CreateAppointmentService();
            var monday = f.NextMonday(0, 0);
            await BookAsync(service, f.Patient.Id, f.FamilyDoctor.Id, monday.AddHours(10));

            var slots = await service.GetAvailableSlotsAsync(
                f.FamilyDoctor.Id, DateOnly.FromDateTime(monday), CancellationToken.None);

            // 09:00–13:00 არის 8 ნახევარსაათიანი, ერთი დაკავებულია
            Assert.Equal(7, slots.Count);
            Assert.DoesNotContain(slots, s => s.StartTime == monday.AddHours(10));
            Assert.Contains(slots, s => s.StartTime == monday.AddHours(9).AddMinutes(30));
        }
    }
}
