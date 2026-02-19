using FluentValidation.TestHelper;
using PlanWriter.Application.Validators;
using PlanWriter.Domain.Dtos.Auth;
using Xunit;

namespace PlanWriter.Tests.Validators;

public class ChangePasswordDtoValidatorTests
{
    private readonly ChangePasswordDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        var dto = new ChangePasswordDto { NewPassword = string.Empty };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Senha é obrigatória.");
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Common()
    {
        var dto = new ChangePasswordDto { NewPassword = "Password123!" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Esta senha é muito comum. Escolha uma senha mais forte.");
    }

    [Fact]
    public void Should_Pass_When_Password_Is_Strong()
    {
        var dto = new ChangePasswordDto { NewPassword = "StrongPassword#2026" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }
}
