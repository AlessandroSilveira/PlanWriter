using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Reports.Dtos.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Dtos.Reports;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;

namespace PlanWriter.Application.Reports.Queries;

public sealed class GetWritingReportQueryHandler(
    ILogger<GetWritingReportQueryHandler> logger,
    IProjectReadRepository projectReadRepository,
    IProjectProgressReadRepository projectProgressReadRepository)
    : IRequestHandler<GetWritingReportQuery, WritingReportDto>
{
    public async Task<WritingReportDto> Handle(GetWritingReportQuery request, CancellationToken cancellationToken)
    {
        var (startDate, endDate) = ResolveDateRange(request.Period, request.StartDate, request.EndDate);

        if (startDate > endDate)
            throw new ValidationException("StartDate must be less than or equal to EndDate.");

        if (request.ProjectId.HasValue)
        {
            var project =
                await projectReadRepository.GetUserProjectByIdAsync(request.ProjectId.Value, request.UserId, cancellationToken);
            if (project is null)
                throw new NotFoundException("Project not found.");
        }

        var dailyRows = await projectProgressReadRepository.GetUserProgressByDayAsync(
            request.UserId,
            startDate,
            endDate,
            request.ProjectId,
            cancellationToken);

        var normalizedRows = dailyRows
            .Select(x => new ProgressHistoryRow(x.Date.Date, x.WordsWritten))
            .OrderBy(x => x.Date)
            .ToList();

        var buckets = BuildBuckets(request.Period, normalizedRows);
        var totalWords = buckets.Sum(x => x.TotalWords);
        var averageWords = buckets.Count == 0
            ? 0
            : Math.Round((decimal)totalWords / buckets.Count, 2, MidpointRounding.AwayFromZero);

        var bestDay = normalizedRows
            .OrderByDescending(x => x.WordsWritten)
            .ThenBy(x => x.Date)
            .FirstOrDefault();

        var currentStreak = CalculateCurrentStreak(normalizedRows, startDate, endDate);

        var csvRows = buckets
            .Select(x => new WritingReportCsvRowDto
            {
                BucketStartDate = x.BucketStartDate,
                BucketEndDate = x.BucketEndDate,
                TotalWords = x.TotalWords
            })
            .ToList();

        logger.LogInformation(
            "Writing report generated. UserId: {UserId}, Period: {Period}, StartDate: {StartDate}, EndDate: {EndDate}, ProjectId: {ProjectId}, TotalWords: {TotalWords}, Buckets: {BucketCount}",
            request.UserId,
            request.Period,
            startDate,
            endDate,
            request.ProjectId,
            totalWords,
            buckets.Count);

        return new WritingReportDto
        {
            Period = request.Period,
            StartDate = startDate,
            EndDate = endDate,
            ProjectId = request.ProjectId,
            TotalWords = totalWords,
            AverageWords = averageWords,
            CurrentStreakDays = currentStreak,
            BestDay = bestDay is null
                ? null
                : new BestWritingDayDto
                {
                    Date = bestDay.Date,
                    Words = bestDay.WordsWritten
                },
            Buckets = buckets,
            Csv = new WritingReportCsvDto
            {
                Columns = new[] { "bucketStartDate", "bucketEndDate", "totalWords" },
                Rows = csvRows
            }
        };
    }

    private static (DateTime StartDate, DateTime EndDate) ResolveDateRange(
        WritingReportPeriod period,
        DateTime? startDate,
        DateTime? endDate)
    {
        var end = (endDate ?? DateTime.UtcNow).Date;
        if (startDate.HasValue)
            return (startDate.Value.Date, end);

        var start = period switch
        {
            WritingReportPeriod.Month => new DateTime(end.Year, end.Month, 1),
            WritingReportPeriod.Week => end.AddDays(-27),
            _ => end.AddDays(-29)
        };

        return (start, end);
    }

    private static List<WritingReportBucketDto> BuildBuckets(
        WritingReportPeriod period,
        IReadOnlyCollection<ProgressHistoryRow> dailyRows)
    {
        return dailyRows
            .GroupBy(row =>
            {
                var bucketStart = period switch
                {
                    WritingReportPeriod.Month => new DateTime(row.Date.Year, row.Date.Month, 1),
                    WritingReportPeriod.Week => StartOfWeek(row.Date),
                    _ => row.Date
                };

                var bucketEnd = period switch
                {
                    WritingReportPeriod.Month => bucketStart.AddMonths(1).AddDays(-1),
                    WritingReportPeriod.Week => bucketStart.AddDays(6),
                    _ => bucketStart
                };

                return (bucketStart, bucketEnd);
            })
            .Select(group => new WritingReportBucketDto
            {
                BucketStartDate = group.Key.bucketStart,
                BucketEndDate = group.Key.bucketEnd,
                TotalWords = group.Sum(row => row.WordsWritten)
            })
            .OrderBy(bucket => bucket.BucketStartDate)
            .ToList();
    }

    private static int CalculateCurrentStreak(
        IReadOnlyCollection<ProgressHistoryRow> dailyRows,
        DateTime startDate,
        DateTime endDate)
    {
        var datesWithWords = dailyRows
            .Where(row => row.WordsWritten > 0)
            .Select(row => row.Date.Date)
            .ToHashSet();

        var streak = 0;
        var cursor = endDate.Date;

        while (cursor >= startDate.Date && datesWithWords.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }
}
