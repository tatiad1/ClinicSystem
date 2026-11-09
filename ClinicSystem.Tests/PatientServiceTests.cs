using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Tests.TestHelpers;

namespace ClinicSystem.Tests;

public class PatientServiceTests
{
    [Fact]
    public async Task AssignFamilyDoctorAsync_RejectsDoctorOutsideFamilyMedicine()
    {
        var fixture = new ClinicFixture();
        var patient = fixture.AddPatientWithoutFamilyDoctor();
        var service = fixture.CreatePatientService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignFamilyDoctorAsync(patient.Id, fixture.Surgeon.Id, CancellationToken.None));

        Assert.Contains("Family Medicine", exception.Message);
        Assert.Null(patient.FamilyDoctorId);
    }

    [Fact]
    public async Task AddAsync_RejectsNonFamilyMedicineFamilyDoctor()
    {
        var fixture = new ClinicFixture();
        var service = fixture.CreatePatientService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAsync(new CreatePatientDTO
            {
                FirstName = "Another",
                LastName = "Patient",
                PersonalNumber = "05005056789",
                BirthDate = new DateOnly(1990, 1, 1),
                FamilyDoctorId = fixture.Surgeon.Id
            }, CancellationToken.None));

        Assert.Contains("Family Medicine", exception.Message);
        Assert.DoesNotContain(fixture.Context.People, p => p.PersonalNumber == "05005056789");
    }

    [Fact]
    public async Task AssignFamilyDoctorAsync_AllowsFamilyMedicineDoctor()
    {
        var fixture = new ClinicFixture();
        var patient = fixture.AddPatientWithoutFamilyDoctor();
        var service = fixture.CreatePatientService();

        await service.AssignFamilyDoctorAsync(patient.Id, fixture.FamilyDoctor.Id, CancellationToken.None);

        Assert.Equal(fixture.FamilyDoctor.Id, patient.FamilyDoctorId);
    }
}
