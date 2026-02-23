using System;

namespace PlanWriter.Domain.Dtos.Projects;

public sealed record ProgressHistoryRow(DateTime Date, int WordsWritten, DateTime? CreatedAt = null);
