using Xunit;
using NutriFlow.Utils;

namespace NutriFlow.Tests
{
    public class EmailValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Validate_WhenEmailIsEmptyOrNull_ReturnsRequiredErrorMessage(string? email)
        {
            // Act
            var result = EmailValidator.Validate(email);

            // Assert
            Assert.Equal("O email é obrigatório", result);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("invalid@")]
        [InlineData("invalid@domain")]
        [InlineData("@domain.com")]
        [InlineData("invalid @domain.com")]
        [InlineData("invalid@ domain.com")]
        [InlineData("invalid@domain.")]
        [InlineData("invalid@.com")]
        public void Validate_WhenEmailIsInvalid_ReturnsInvalidErrorMessage(string email)
        {
            // Act
            var result = EmailValidator.Validate(email);

            // Assert
            Assert.Equal("Informe um email válido (ex: usuario@gmail.com)", result);
        }

        [Theory]
        [InlineData("valid@domain.com")]
        [InlineData("user.name@domain.co.uk")]
        [InlineData("user_name@domain.org")]
        [InlineData("user+name@domain.net")]
        [InlineData("12345@domain.com")]
        public void Validate_WhenEmailIsValid_ReturnsNull(string email)
        {
            // Act
            var result = EmailValidator.Validate(email);

            // Assert
            Assert.Null(result);
        }
    }
}
