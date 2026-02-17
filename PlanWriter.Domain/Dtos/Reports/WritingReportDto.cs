using System;
using System.Collections.Generic;

namespace PlanWriter.Domain.Dtos.Reports;

public enum WritingReportPeriod
{
    Day = 0,
    Week = 1,
    Month = 2
}

public sealed class WritingReportDto
{
    public WritingReportPeriod Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? ProjectId { get; set; }

    public int TotalWords { get; set; }
    public decimal AverageWords { get; set; }
    public int CurrentStreakDays { get; set; }
    public BestWritingDayDto? BestDay { get; set; }

    public IReadOnlyList<WritingReportBucketDto> Buckets { get; set; } = Array.Empty<WritingReportBucketDto>();
    public WritingReportCsvDto Csv { get; set; } = new();
}

public sealed class BestWritingDayDto
{
    public DateTime Date { get; set; }
    public int Words { get; set; }
}

public sealed class WritingReportBucketDto
{
    public DateTime BucketStartDate { get; set; }
    public DateTime BucketEndDate { get; set; }
    public int TotalWords { get; set; }
}

public sealed class WritingReportCsvDto
{
    public IReadOnlyList<string> Columns { get; set; } = new[] { "bucketStartDate", "bucketEndDate", "totalWords" };
    public IReadOnlyList<WritingReportCsvRowDto> Rows { get; set; } = Array.Empty<WritingReportCsvRowDto>();
}

public sealed class WritingReportCsvRowDto
{
    public DateTime BucketStartDate { get; set; }
    public DateTime BucketEndDate { get; set; }
    public int TotalWords { get; set; }
}
