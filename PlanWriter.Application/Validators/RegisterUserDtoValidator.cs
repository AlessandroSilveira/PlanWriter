using System;
using FluentValidation;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Security;

namespace PlanWriter.Application.Validators;

public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .Must(password => PasswordPolicy.Validate(password) is null)
            .WithMessage(dto => PasswordPolicy.Validate(dto.Password)!);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(BeAValidDate).WithMessage("Date of birth must be a valid date.")
            .LessThan(DateTime.Now).WithMessage("Date of birth must be in the past.");
    }

    private bool BeAValidDate(DateTime date)
    {
        return date != default;
    }
}
