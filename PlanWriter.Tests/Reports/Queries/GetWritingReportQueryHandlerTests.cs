using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Reports.Dtos.Queries;
using PlanWriter.Application.Reports.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Dtos.Reports;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Reports.Queries;

public class GetWritingReportQueryHandlerTests
{
    private readonly Mock<ILogger<GetWritingReportQueryHandler>> _loggerMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();
    private readonly Mock<IProjectProgressReadRepository> _projectProgressReadRepositoryMock = new();

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenStartDateIsGreaterThanEndDate()
    {
        var query = new GetWritingReportQuery(
            Guid.NewGuid(),
            WritingReportPeriod.Day,
            new DateTime(2026, 2, 20),
            new DateTime(2026, 2, 10),
            null);

        var sut = CreateHandler();
        var act = async () => await sut.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("StartDate must be less than or equal to EndDate.");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenProjectIsNotFromUser()
    {
        var projectId = Guid.NewGuid();
        var query = new GetWritingReportQuery(
            Guid.NewGuid(),
            WritingReportPeriod.Day,
            new DateTime(2026, 2, 1),
            new DateTime(2026, 2, 10),
            projectId);

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var sut = CreateHandler();
        var act = async () => await sut.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Project not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnDayBuckets_WithSummaryAndCsv()
    {
        var query = new GetWritingReportQuery(
            Guid.NewGuid(),
            WritingReportPeriod.Day,
            new DateTime(2026, 2, 10),
            new DateTime(2026, 2, 12),
            null);

        _projectProgressReadRepositoryMock
            .Setup(r => r.GetUserProgressByDayAsync(
                query.UserId,
                query.StartDate!.Value.Date,
                query.EndDate!.Value.Date,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProgressHistoryRow(new DateTime(2026, 2, 10), 100),
                new ProgressHistoryRow(new DateTime(2026, 2, 11), 120),
                new ProgressHistoryRow(new DateTime(2026, 2, 12), 80)
            });

        var sut = CreateHandler();
        var result = await sut.Handle(query, CancellationToken.None);

        result.TotalWords.Should().Be(300);
        result.AverageWords.Should().Be(100);
        result.BestDay.Should().NotBeNull();
        result.BestDay!.Date.Should().Be(new DateTime(2026, 2, 11));
        result.BestDay.Words.Should().Be(120);
        result.CurrentStreakDays.Should().Be(3);
        result.Buckets.Should().HaveCount(3);
        result.Csv.Rows.Should().HaveCount(3);
        result.Csv.Columns.Should().ContainInOrder("bucketStartDate", "bucketEndDate", "totalWords");
    }

    [Fact]
    public async Task Handle_ShouldGroupByWeek_WhenPeriodIsWeek()
    {
        var query = new GetWritingReportQuery(
            Guid.NewGuid(),
            WritingReportPeriod.Week,
            new DateTime(2026, 2, 1),
            new DateTime(2026, 2, 28),
            null);

        _projectProgressReadRepositoryMock
            .Setup(r => r.GetUserProgressByDayAsync(
                query.UserId,
                query.StartDate!.Value.Date,
                query.EndDate!.Value.Date,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProgressHistoryRow(new DateTime(2026, 2, 2), 100), // week 1
                new ProgressHistoryRow(new DateTime(2026, 2, 5), 200), // week 1
                new ProgressHistoryRow(new DateTime(2026, 2, 10), 300) // week 2
            });

        var sut = CreateHandler();
        var result = await sut.Handle(query, CancellationToken.None);

        result.Buckets.Should().HaveCount(2);
        result.Buckets[0].TotalWords.Should().Be(300);
        result.Buckets[1].TotalWords.Should().Be(300);
        result.AverageWords.Should().Be(300);
    }

    [Fact]
    public async Task Handle_ShouldPassProjectFilterToRepository_WhenProjectIdIsProvided()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var query = new GetWritingReportQuery(
            userId,
            WritingReportPeriod.Month,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 2, 28),
            projectId);

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, UserId = userId });

        _projectProgressReadRepositoryMock
            .Setup(r => r.GetUserProgressByDayAsync(
                userId,
                query.StartDate!.Value.Date,
                query.EndDate!.Value.Date,
                projectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProgressHistoryRow(new DateTime(2026, 1, 2), 500),
                new ProgressHistoryRow(new DateTime(2026, 2, 4), 700)
            });

        var sut = CreateHandler();
        var result = await sut.Handle(query, CancellationToken.None);

        result.ProjectId.Should().Be(projectId);
        result.Buckets.Should().HaveCount(2);
        result.TotalWords.Should().Be(1200);

        _projectProgressReadRepositoryMock.Verify(
            r => r.GetUserProgressByDayAsync(
                userId,
                query.StartDate!.Value.Date,
                query.EndDate!.Value.Date,
                projectId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private GetWritingReportQueryHandler CreateHandler()
    {
        return new GetWritingReportQueryHandler(
            _loggerMock.Object,
            _projectReadRepositoryMock.Object,
            _projectProgressReadRepositoryMock.Object);
    }
}
