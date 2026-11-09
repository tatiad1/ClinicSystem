using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Tests.TestHelpers;

namespace ClinicSystem.Tests
{
    public class ReferralServiceTests
    {
        [Fact]
        public async Task AddAsync_ByFamilyDoctor_CreatesActiveReferral()
        {
            var f = new ClinicFixture();
            var service = f.CreateReferralService();

            var result = await service.AddAsync(f.Patient.Id, new CreateReferralDTO
            {
                FromDoctorId = f.FamilyDoctor.Id,
                ToSpecialtyId = f.SurgerySpecialty.Id,
                Reason = "Knee pain",
                ValidForDays = 10
            }, CancellationToken.None);

            Assert.True(result.IsActive);
            Assert.False(result.IsUsed);
            Assert.True(result.ExpiresAt > DateTime.UtcNow.AddDays(9));
        }

        [Fact]
        public async Task AddAsync_ByDoctorWhoIsNotFamilyDoctor_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateReferralService();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddAsync(f.Patient.Id, new CreateReferralDTO
                {
                    FromDoctorId = f.Surgeon.Id,
                    ToSpecialtyId = f.FamilySpecialty.Id
                }, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_ByDoctorAfterCompletedVisit_AllowsUnassignedPatientToGetReferral()
        {
            var f = new ClinicFixture();
            var patient = f.AddPatientWithoutFamilyDoctor();
            f.AddAppointment(
                patient,
                f.Surgeon,
                DateTime.Now.AddDays(-1),
                ClinicSystem.Domain.Enums.AppointmentStatus.Completed);
            var service = f.CreateReferralService();

            var result = await service.AddAsync(patient.Id, new CreateReferralDTO
            {
                FromDoctorId = f.Surgeon.Id,
                ToSpecialtyId = f.NeurologySpecialty.Id,
                Reason = "Further examination"
            }, CancellationToken.None);

            Assert.Equal(patient.Id, result.PatientId);
            Assert.Equal(f.Surgeon.Id, result.FromDoctorId);
            Assert.Equal(f.NeurologySpecialty.Id, result.ToSpecialtyId);
            Assert.Null(patient.FamilyDoctorId);
        }

        [Fact]
        public async Task AddAsync_ToDoctorsOwnSpecialty_ThrowsArgumentException()
        {
            var f = new ClinicFixture();
            var service = f.CreateReferralService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddAsync(f.Patient.Id, new CreateReferralDTO
                {
                    FromDoctorId = f.FamilyDoctor.Id,
                    ToSpecialtyId = f.FamilySpecialty.Id
                }, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_SecondActiveReferralToSameSpecialty_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateReferralService();
            var dto = new CreateReferralDTO { FromDoctorId = f.FamilyDoctor.Id, ToSpecialtyId = f.SurgerySpecialty.Id };

            await service.AddAsync(f.Patient.Id, dto, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddAsync(f.Patient.Id, dto, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_ZeroValidDays_ThrowsArgumentException()
        {
            var f = new ClinicFixture();
            var service = f.CreateReferralService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddAsync(f.Patient.Id, new CreateReferralDTO
                {
                    FromDoctorId = f.FamilyDoctor.Id,
                    ToSpecialtyId = f.SurgerySpecialty.Id,
                    ValidForDays = 0
                }, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_InactiveDoctor_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateReferralService();
            f.FamilyDoctor.IsActive = false;
            f.Context.SaveChanges();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddAsync(f.Patient.Id, new CreateReferralDTO
                {
                    FromDoctorId = f.FamilyDoctor.Id,
                    ToSpecialtyId = f.SurgerySpecialty.Id
                }, CancellationToken.None));

            Assert.Contains("not active", ex.Message);
        }
    }
}
