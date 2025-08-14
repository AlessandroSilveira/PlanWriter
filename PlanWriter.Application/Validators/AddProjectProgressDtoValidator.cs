using FluentValidation;
using PlanWriter.Application.DTOs;
using System;
using PlanWriter.Application.DTO;

namespace PlanWriter.Application.Validators
{
    public class AddProjectProgressDtoValidator : AbstractValidator<AddProjectProgressDto>
    {
        public AddProjectProgressDtoValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required.")
                .NotEqual(Guid.Empty).WithMessage("Project ID cannot be empty GUID.");

            // RuleFor(x => x.TotalWordsWritten)
            //     .GreaterThanOrEqualTo(0).WithMessage("Total words written must be zero or greater.");
        }
    }
}