using ClinicSystem.Application.Validation;

namespace ClinicSystem.Tests
{
    public class PersonValidationTests
    {
        [Fact]
        public void ValidatePersonalNumber_ElevenDigits_DoesNotThrow()
        {
            var exception = Record.Exception(() => PersonValidation.ValidatePersonalNumber("01001012345"));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("0100101234a")]
        [InlineData("010010123456")]
        [InlineData("")]
        [InlineData(null)]
        public void ValidatePersonalNumber_InvalidValue_ThrowsArgumentException(string? value)
        {
            Assert.Throws<ArgumentException>(() => PersonValidation.ValidatePersonalNumber(value));
        }

        [Fact]
        public void ValidateName_TrimsWhitespace()
        {
            var result = PersonValidation.ValidateName("  Nino  ", "First name");

            Assert.Equal("Nino", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateName_BlankValue_ThrowsArgumentException(string? value)
        {
            Assert.Throws<ArgumentException>(() => PersonValidation.ValidateName(value, "First name"));
        }

        [Fact]
        public void ValidateName_LongerThan50Characters_ThrowsArgumentException()
        {
            var tooLong = new string('a', 51);

            Assert.Throws<ArgumentException>(() => PersonValidation.ValidateName(tooLong, "First name"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidatePhone_BlankValue_ReturnsNull(string? value)
        {
            Assert.Null(PersonValidation.ValidatePhone(value));
        }

        [Fact]
        public void ValidatePhone_LongerThan20Characters_ThrowsArgumentException()
        {
            var tooLong = new string('5', 21);

            Assert.Throws<ArgumentException>(() => PersonValidation.ValidatePhone(tooLong));
        }
    }
}