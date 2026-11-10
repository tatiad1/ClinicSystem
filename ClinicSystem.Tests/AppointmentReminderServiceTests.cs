using ClinicSystem.Domain.Enums;
using ClinicSystem.Tests.TestHelpers;

namespace ClinicSystem.Tests
{
    public class AppointmentReminderServiceTests
    {
        [Fact]
        public async Task SendDueRemindersAsync_BookedAppointmentWithinWindow_SendsAndMarksAsSent()
        {
            var f = new ClinicFixture();
            var sender = new FakeNotificationSender();
            var service = f.CreateReminderService(sender, hoursBefore: 24);
            var appointment = f.AddAppointment(f.Patient, f.FamilyDoctor, DateTime.Now.AddHours(5), AppointmentStatus.Booked);

            var count = await service.SendDueRemindersAsync(CancellationToken.None);

            Assert.Equal(1, count);
            var message = Assert.Single(sender.Sent);
            Assert.Equal("giorgi@test.ge", message.To);
            Assert.True(appointment.ReminderSent);
        }

        [Fact]
        public async Task SendDueRemindersAsync_CalledTwice_DoesNotSendAgain()
        {
            var f = new ClinicFixture();
            var sender = new FakeNotificationSender();
            var service = f.CreateReminderService(sender);
            f.AddAppointment(f.Patient, f.FamilyDoctor, DateTime.Now.AddHours(5), AppointmentStatus.Booked);

            await service.SendDueRemindersAsync(CancellationToken.None);
            var secondCount = await service.SendDueRemindersAsync(CancellationToken.None);

            Assert.Equal(0, secondCount);
            Assert.Single(sender.Sent);
        }

        [Fact]
        public async Task SendDueRemindersAsync_CancelledAppointment_SendsNothing()
        {
            var f = new ClinicFixture();
            var sender = new FakeNotificationSender();
            var service = f.CreateReminderService(sender);
            f.AddAppointment(f.Patient, f.FamilyDoctor, DateTime.Now.AddHours(5), AppointmentStatus.Cancelled);

            var count = await service.SendDueRemindersAsync(CancellationToken.None);

            Assert.Equal(0, count);
            Assert.Empty(sender.Sent);
        }

        [Fact]
        public async Task SendDueRemindersAsync_PatientWithoutAccount_SendsNothing()
        {
            var f = new ClinicFixture();
            var sender = new FakeNotificationSender();
            var service = f.CreateReminderService(sender);
            var appointment = f.AddAppointment(f.OtherPatient, f.FamilyDoctor, DateTime.Now.AddHours(5), AppointmentStatus.Booked);

            var count = await service.SendDueRemindersAsync(CancellationToken.None);

            Assert.Equal(0, count);
            Assert.False(appointment.ReminderSent);
        }

        [Fact]
        public async Task SendDueRemindersAsync_AppointmentOutsideWindow_SendsNothing()
        {
            var f = new ClinicFixture();
            var sender = new FakeNotificationSender();
            var service = f.CreateReminderService(sender, hoursBefore: 24);
            f.AddAppointment(f.Patient, f.FamilyDoctor, DateTime.Now.AddHours(48), AppointmentStatus.Booked);

            var count = await service.SendDueRemindersAsync(CancellationToken.None);

            Assert.Equal(0, count);
        }
    }
}
