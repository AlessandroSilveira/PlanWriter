using FluentValidation;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Dtos.Auth;

namespace PlanWriter.Application.Validators;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .Must(password => PasswordPolicy.Validate(password) is null)
            .WithMessage(dto => PasswordPolicy.Validate(dto.NewPassword)!);
    }
}
