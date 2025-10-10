using FluentValidation;
using PlanWriter.Application.DTO;
using System;

namespace PlanWriter.Application.Validators
{
    public class AddProjectProgressDtoValidator : AbstractValidator<AddProjectProgressDto>
    {
        public AddProjectProgressDtoValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required.")
                .NotEqual(Guid.Empty).WithMessage("Project ID cannot be empty GUID.");

            RuleFor(x => x.WordsWritten)
                .GreaterThan(0).WithMessage("Words written must be greater than zero.");
        }
    }
}
