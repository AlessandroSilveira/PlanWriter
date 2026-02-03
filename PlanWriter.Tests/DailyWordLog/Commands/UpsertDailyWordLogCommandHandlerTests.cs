using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.DailyWordLogs.Commands;
using PlanWriter.Application.DailyWordLogs.Dtos.Commands;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Interfaces.Repositories.DailyWordLogWrite;
using Xunit;

namespace PlanWriter.Tests.DailyWordLog.Commands;

public class UpsertDailyWordLogCommandHandlerTests
{
    private readonly Mock<IDailyWordLogWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<ILogger<UpsertDailyWordLogCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_ShouldCallUpsert_WithCorrectData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var requestDto = new UpsertDailyWordLogRequest{
           ProjectId = projectId,
            Date = date,
            WordsWritten = 500
        };

        var command = new UpsertDailyWordLogCommand(
            userId,
            requestDto
        );

        _writeRepositoryMock
            .Setup(r => r.UpsertAsync(
                projectId,
                userId,
                date,
                500,
                It.IsAny<CancellationToken>()
            ))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _writeRepositoryMock.Verify(
            r => r.UpsertAsync(
                projectId,
                userId,
                date,
                500,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenWordsWrittenIsNegative()
    {
        // Arrange
        var command = new UpsertDailyWordLogCommand(
            Guid.NewGuid(),
            new UpsertDailyWordLogRequest
            {
                ProjectId = Guid.NewGuid(),
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                WordsWritten = -10
            });
            
       
        var handler = CreateHandler();

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("WordsWritten não pode ser negativo.");
    }

    /* ===================== HELPERS ===================== */

    private UpsertDailyWordLogCommandHandler CreateHandler()
        => new(
            _writeRepositoryMock.Object,
            _loggerMock.Object
        );
}
