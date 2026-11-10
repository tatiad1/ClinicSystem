using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Tests.TestHelpers;

namespace ClinicSystem.Tests;

public class ConsultationServiceTests
{
    [Fact]
    public async Task AddAsync_BeforeAppointmentStarts_RejectsConsultation()
    {
        var fixture = new ClinicFixture();
        var appointment = fixture.AddAppointment(
            fixture.Patient,
            fixture.FamilyDoctor,
            DateTime.Now.AddHours(1),
            AppointmentStatus.Booked);
        var service = fixture.CreateConsultationService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAsync(appointment.Id, new CreateConsultationDTO
            {
                Diagnosis = "Test diagnosis"
            }, CancellationToken.None));

        Assert.Contains("before the appointment starts", exception.Message);
        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
        Assert.Empty(fixture.Context.Consultations);
    }

    [Fact]
    public async Task AddAsync_AfterAppointmentStarts_CompletesVisit()
    {
        var fixture = new ClinicFixture();
        var appointment = fixture.AddAppointment(
            fixture.Patient,
            fixture.FamilyDoctor,
            DateTime.Now.AddMinutes(-5),
            AppointmentStatus.Booked);
        var service = fixture.CreateConsultationService();

        var result = await service.AddAsync(appointment.Id, new CreateConsultationDTO
        {
            Diagnosis = "Test diagnosis"
        }, CancellationToken.None);

        Assert.Equal(appointment.Id, result.AppointmentId);
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Single(fixture.Context.Consultations);
    }
}
