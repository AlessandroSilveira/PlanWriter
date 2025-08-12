using FluentValidation;
using PlanWriter.Application.DTOs;

namespace PlanWriter.Application.Validators
{
    public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
    {
        public CreateProjectDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required.")
                .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters.");

            RuleFor(x => x.TotalWordsGoal)
                .GreaterThan(0).WithMessage("Total word goal must be greater than zero.");
        }
    }
}