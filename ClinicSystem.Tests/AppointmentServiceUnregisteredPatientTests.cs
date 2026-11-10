using ClinicSystem.Application.Implementations;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Tests.TestHelpers;

namespace ClinicSystem.Tests;

public class AppointmentServiceUnregisteredPatientTests
{
    [Fact]
    public async Task AddAsync_PatientWithoutFamilyDoctor_CanBookFamilyMedicineWithoutReferral()
    {
        var fixture = new ClinicFixture();
        var patient = fixture.AddPatientWithoutFamilyDoctor();
        var service = fixture.CreateAppointmentService();
        var appointmentTime = fixture.NextMonday(10, 0);

        // An unassigned patient can book a one-off family-medicine visit.
        var result = await service.AddAsync(
            new CreateAppointmentDTO
            {
                PatientId = patient.Id,
                DoctorId = fixture.FamilyDoctor.Id,
                StartTime = appointmentTime
            },
            CancellationToken.None);

        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal(fixture.FamilyDoctor.Id, result.DoctorId);
        Assert.Null(result.ReferralId);
        Assert.Null(patient.FamilyDoctorId);
    }

    [Fact]
    public async Task AddAsync_PatientWithoutFamilyDoctor_CannotBookReferralSpecialtyWithoutReferral()
    {
        var fixture = new ClinicFixture();
        var patient = fixture.AddPatientWithoutFamilyDoctor();
        var service = fixture.CreateAppointmentService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAsync(
                new CreateAppointmentDTO
                {
                    PatientId = patient.Id,
                    DoctorId = fixture.Surgeon.Id,
                    StartTime = fixture.NextMonday(11, 0)
                },
                CancellationToken.None));

        Assert.Contains("family-medicine", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(fixture.Context.Appointments);
    }

    [Fact]
    public async Task AddAsync_PatientWithoutFamilyDoctor_CanUseReferralAfterFamilyMedicineConsultation()
    {
        var fixture = new ClinicFixture();
        var patient = fixture.AddPatientWithoutFamilyDoctor();
        fixture.AddAppointment(
            patient,
            fixture.FamilyDoctor,
            DateTime.Now.AddDays(-1),
            ClinicSystem.Domain.Enums.AppointmentStatus.Completed);

        var referral = await fixture.CreateReferralService().AddAsync(
            patient.Id,
            new CreateReferralDTO
            {
                FromDoctorId = fixture.FamilyDoctor.Id,
                ToSpecialtyId = fixture.SurgerySpecialty.Id,
                Reason = "Needs specialist review"
            },
            CancellationToken.None);

        var appointment = await fixture.CreateAppointmentService().AddAsync(
            new CreateAppointmentDTO
            {
                PatientId = patient.Id,
                DoctorId = fixture.Surgeon.Id,
                StartTime = fixture.NextMonday(11, 0),
                ReferralId = referral.Id
            },
            CancellationToken.None);

        Assert.Equal(referral.Id, appointment.ReferralId);
        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
        Assert.Null(patient.FamilyDoctorId); // referral doesn't assign a permanent family doctor
    }

    [Fact]
    public async Task AddAsync_PatientWithFamilyDoctor_StillNeedsReferralForAnotherDoctor()
    {
        // Arrange: this patient has a family doctor and wants to see a different doctor.
        var fixture = new ClinicFixture();
        var service = fixture.CreateAppointmentService();

        // Act and assert: a referral is still required.
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAsync(
                new CreateAppointmentDTO
                {
                    PatientId = fixture.Patient.Id,
                    DoctorId = fixture.Surgeon.Id,
                    StartTime = fixture.NextMonday(11, 0),
                    ReferralId = null
                },
                CancellationToken.None));

        Assert.Contains("referral", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(fixture.Context.Appointments);
    }
}
