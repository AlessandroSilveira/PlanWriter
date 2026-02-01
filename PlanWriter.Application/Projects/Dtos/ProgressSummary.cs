using System;

namespace PlanWriter.Application.Projects.Dtos;

public sealed record ProgressSummary(DateTime Date, int Total);