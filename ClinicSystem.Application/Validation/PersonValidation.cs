namespace ClinicSystem.Application.Validation
{
    public static class PersonValidation
    {
        public static string ValidateName(string? value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{field} is required");

            var trimmed = value.Trim();
            if (trimmed.Length > 50)
                throw new ArgumentException($"{field} must be at most 50 characters");

            return trimmed;
        }

        public static string? ValidatePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var trimmed = phone.Trim();
            if (trimmed.Length > 20)
                throw new ArgumentException("Phone must be at most 20 characters");

            return trimmed;
        }

        public static void ValidatePersonalNumber(string? personalNumber)
        {
            if (personalNumber == null
                || personalNumber.Length != 11
                || !personalNumber.All(c => c >= '0' && c <= '9'))
                throw new ArgumentException("Personal number must be exactly 11 digits");
        }
    }
}