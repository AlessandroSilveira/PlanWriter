using System;

namespace PlanWriter.Domain.Dtos.Projects;

public sealed record ProgressRow(Guid Id, Guid ProjectId, DateTime Date);