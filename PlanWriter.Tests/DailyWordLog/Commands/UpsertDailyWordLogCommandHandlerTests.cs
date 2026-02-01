using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.DailyWordLogs.Commands;
using PlanWriter.Application.DailyWordLogs.Dtos.Commands;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.DailyWordLog.Commands;

public class UpsertDailyWordLogCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IDailyWordLogRepository> _dailyWordLogRepoMock = new();
    private readonly Mock<ILogger<UpsertDailyWordLogCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldInsert_WhenLogDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var requestDto = new CreateDailyWordLogRequest
        {
            ProjectId = projectId,
            Date = date,
            WordsWritten = 500
        };

        var command = new UpsertDailyWordLogCommand(
            requestDto,
            user: new ClaimsPrincipal() // não importa o tipo real aqui
        );

        _userServiceMock
            .Setup(s => s.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _dailyWordLogRepoMock
            .Setup(r => r.GetByProjectAndDateAsync(projectId, date, userId))
            .ReturnsAsync((Domain.Entities.DailyWordLog?)null);

        _dailyWordLogRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Domain.Entities.DailyWordLog>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _dailyWordLogRepoMock.Verify(
            r => r.AddAsync(It.Is<Domain.Entities.DailyWordLog>(log =>
                log.ProjectId == projectId &&
                log.UserId == userId &&
                log.Date == date &&
                log.WordsWritten == 500
            )),
            Times.Once
        );

        _dailyWordLogRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<Domain.Entities.DailyWordLog>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldUpdate_WhenLogAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var existingLog = new Domain.Entities.DailyWordLog
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Date = date,
            WordsWritten = 300
        };

        var requestDto =
            new CreateDailyWordLogRequest
            {
                ProjectId = projectId,
                Date = date,
                WordsWritten = 800
            };

        var command = new UpsertDailyWordLogCommand(requestDto, It.IsAny<ClaimsPrincipal>());

        _userServiceMock
            .Setup(s => s.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _dailyWordLogRepoMock
            .Setup(r => r.GetByProjectAndDateAsync(projectId, date, userId))
            .ReturnsAsync(existingLog);

        _dailyWordLogRepoMock
            .Setup(r => r.UpdateAsync(existingLog))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        existingLog.WordsWritten.Should().Be(800);

        _dailyWordLogRepoMock.Verify(
            r => r.UpdateAsync(existingLog),
            Times.Once
        );

        _dailyWordLogRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Domain.Entities.DailyWordLog>()),
            Times.Never
        );
    }

    /* ===================== HELPERS ===================== */

    private UpsertDailyWordLogCommandHandler CreateHandler() 
        => new(_userServiceMock.Object, _dailyWordLogRepoMock.Object, _loggerMock.Object);
}