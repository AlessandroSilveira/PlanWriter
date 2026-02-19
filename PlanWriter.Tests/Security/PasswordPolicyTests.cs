using FluentAssertions;
using PlanWriter.Application.Security;
using Xunit;

namespace PlanWriter.Tests.Security;

public class PasswordPolicyTests
{
    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordIsEmpty()
    {
        var result = PasswordPolicy.Validate(string.Empty);

        result.Should().Be("Senha é obrigatória.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordIsShort()
    {
        var result = PasswordPolicy.Validate("Abc123!");

        result.Should().Be("A senha deve ter pelo menos 12 caracteres.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordMissingUppercase()
    {
        var result = PasswordPolicy.Validate("validpassword#2026");

        result.Should().Be("A senha deve conter ao menos uma letra maiúscula.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordMissingLowercase()
    {
        var result = PasswordPolicy.Validate("VALIDPASSWORD#2026");

        result.Should().Be("A senha deve conter ao menos uma letra minúscula.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordMissingNumber()
    {
        var result = PasswordPolicy.Validate("ValidPassword#");

        result.Should().Be("A senha deve conter ao menos um número.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordMissingSymbol()
    {
        var result = PasswordPolicy.Validate("ValidPassword2026");

        result.Should().Be("A senha deve conter ao menos um símbolo.");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordIsBlocked()
    {
        var result = PasswordPolicy.Validate("Password123!");

        result.Should().Be("Esta senha é muito comum. Escolha uma senha mais forte.");
    }

    [Fact]
    public void Validate_ShouldReturnNull_WhenPasswordIsValid()
    {
        var result = PasswordPolicy.Validate("ValidPassword#2026");

        result.Should().BeNull();
    }
}
