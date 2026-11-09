namespace ClinicSystem.Application.Settings
{
    public class ReminderSettings
    {
        // რამდენი საათით ადრე ეგზავნება შეხსენება
        public int HoursBefore { get; set; } = 24;

        // რამდენ წუთში ერთხელ ამოწმებს
        public int CheckIntervalMinutes { get; set; } = 1;
    }
}