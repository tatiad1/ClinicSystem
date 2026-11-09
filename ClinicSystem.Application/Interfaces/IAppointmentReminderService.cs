namespace ClinicSystem.Application.Interfaces
{
    public interface IAppointmentReminderService
    {
        // აბრუნებს, რამდენ პაციენტს გაუგზავნა შეხსენება
        Task<int> SendDueRemindersAsync(CancellationToken cancellationToken);
    }
}