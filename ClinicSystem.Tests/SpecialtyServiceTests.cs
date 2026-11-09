using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Tests
{
    public class SpecialtyServiceTests
    {
        [Fact]
        public async Task AddAsync_ValidName_TrimsAndSaves()
        {
            var f = new ClinicFixture();
            var service = f.CreateSpecialtyService();

            var result = await service.AddAsync(new CreateSpecialtyDTO { Name = "  Cardiology  " }, CancellationToken.None);

            Assert.Equal("Cardiology", result.Name);
            Assert.True(await f.Context.Specialties.AnyAsync(s => s.Name == "Cardiology"));
        }

        [Fact]
        public async Task AddAsync_DuplicateName_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateSpecialtyService();

            // "Surgery" უკვე არსებობს fixture-ში
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddAsync(new CreateSpecialtyDTO { Name = "Surgery" }, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_BlankName_ThrowsArgumentException()
        {
            var f = new ClinicFixture();
            var service = f.CreateSpecialtyService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddAsync(new CreateSpecialtyDTO { Name = "   " }, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_SpecialtyWithDoctors_ThrowsInvalidOperation()
        {
            var f = new ClinicFixture();
            var service = f.CreateSpecialtyService();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeleteAsync(f.FamilySpecialty.Id, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_SpecialtyWithoutDoctors_RemovesIt()
        {
            var f = new ClinicFixture();
            var service = f.CreateSpecialtyService();
            var created = await service.AddAsync(new CreateSpecialtyDTO { Name = "Temporary" }, CancellationToken.None);

            await service.DeleteAsync(created.Id, CancellationToken.None);

            Assert.False(await f.Context.Specialties.AnyAsync(s => s.Id == created.Id));
        }
    }
}