namespace ClinicSystem.Domain.Models
{
    public static class AppointmentRules
    {
        // ერთი ვიზიტის ხანგრძლივობა წუთებში
        public const int SlotMinutes = 30;

        // ვიზიტის დასრულების შემდეგ პაციენტს მოსასვლელად დამატებით ეძლევა
        public const int NoShowGraceMinutes = 15;
    }
}
