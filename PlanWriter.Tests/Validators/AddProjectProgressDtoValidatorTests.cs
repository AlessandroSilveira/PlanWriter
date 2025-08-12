using FluentValidation.TestHelper;
using PlanWriter.Application.DTOs;
using PlanWriter.Application.Validators;
using System;
using PlanWriter.Application.DTO;
using Xunit;

namespace PlanWriter.Application.Tests.Validators
{
    public class AddProjectProgressDtoValidatorTests
    {
        private readonly AddProjectProgressDtoValidator _validator;

        public AddProjectProgressDtoValidatorTests()
        {
            _validator = new AddProjectProgressDtoValidator();
        }

        [Fact]
        public void Should_Have_Error_When_ProjectId_Is_Empty()
        {
            var dto = new AddProjectProgressDto
            {
                ProjectId = Guid.Empty,
                TotalWordsWritten = 1000
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.ProjectId);
        }

        [Fact]
        public void Should_Have_Error_When_TotalWordsWritten_Is_Negative()
        {
            var dto = new AddProjectProgressDto
            {
                ProjectId = Guid.NewGuid(),
                TotalWordsWritten = -50
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.TotalWordsWritten);
        }

        [Fact]
        public void Should_Pass_When_Data_Is_Valid()
        {
            var dto = new AddProjectProgressDto
            {
                ProjectId = Guid.NewGuid(),
                TotalWordsWritten = 1500
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}