using FluentValidation.TestHelper;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Validators;
using Xunit;

namespace PlanWriter.Tests.Validators
{
    public class RegisterUserDtoValidatorTests
    {
        private readonly RegisterUserDtoValidator _validator;

        public RegisterUserDtoValidatorTests()
        {
            _validator = new RegisterUserDtoValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Fields_Are_Empty()
        {
            var dto = new RegisterUserDto();

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            result.ShouldHaveValidationErrorFor(x => x.LastName);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
            result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Invalid()
        {
            var dto = new RegisterUserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "invalid-email",
                Password = "123456",
                DateOfBirth = DateTime.Now.AddYears(-20)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Is_Too_Short()
        {
            var dto = new RegisterUserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Password = "123",
                DateOfBirth = DateTime.Now.AddYears(-20)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Should_Have_Error_When_DateOfBirth_Is_In_The_Future()
        {
            var dto = new RegisterUserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Password = "123456",
                DateOfBirth = DateTime.Now.AddDays(1)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
        }

        [Fact]
        public void Should_Pass_When_All_Fields_Are_Valid()
        {
            var dto = new RegisterUserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Password = "123456",
                DateOfBirth = DateTime.Now.AddYears(-20)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
