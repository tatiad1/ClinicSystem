namespace ClinicSystem.Application.Interfaces
{
    public interface IAccountWelcomeEmailService
    {
        Task SendWelcomeEmailAsync(string email, string firstName, string role);
    }
}
