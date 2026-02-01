using System;

namespace PlanWriter.Application.EventValidation.Dtos.Commands;

public record ValidateRequest(Guid ProjectId, int Words, string? Source);