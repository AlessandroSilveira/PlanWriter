using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.WordWar.Commands;
using PlanWriter.Application.WordWar.Dtos.Commands;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Dtos.WordWars;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;
using Xunit;

namespace PlanWriter.Tests.WordWar.Commands;

public class JoinWordWarCommandHandlerTests
{
    private readonly Mock<ILogger<JoinWordWarCommandHandler>> _loggerMock = new();
    private readonly Mock<IWordWarReadRepository> _wordWarReadRepositoryMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();
    private readonly Mock<IWordWarParticipantReadRepository> _wordWarParticipantReadRepositoryMock = new();
    private readonly Mock<IWordWarRepository> _wordWarRepositoryMock = new();

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenWordWarDoesNotExist()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarsDto?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("WordWar not exist.");

        _wordWarRepositoryMock.Verify(
            r => r.JoinAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenWordWarStatusIsNotWaiting()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running
            });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Can't join the word war when the status is not waiting.");

        _wordWarRepositoryMock.Verify(
            r => r.JoinAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenProjectDoesNotBelongToUser()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProjectDto>());

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("This project doesn't belong to this user.");

        _wordWarParticipantReadRepositoryMock.Verify(
            r => r.GetParticipant(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenUserAlreadyParticipates()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectDto { Id = command.ProjectId, Title = "Book" }
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                Id = Guid.NewGuid(),
                WordWarId = command.WarId,
                UserId = command.UserId,
                ProjectId = command.ProjectId
            });

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        _wordWarRepositoryMock.Verify(
            r => r.JoinAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenJoinAffectsOneRow()
    {
        var command = NewCommand();
        using var cts = new CancellationTokenSource();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectDto { Id = command.ProjectId, Title = "Book" }
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarParticipantsDto?)null);

        _wordWarRepositoryMock
            .Setup(r => r.JoinAsync(command.WarId, command.UserId, command.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(command, cts.Token);

        result.Should().BeTrue();
        _wordWarRepositoryMock.Verify(
            r => r.JoinAsync(command.WarId, command.UserId, command.ProjectId, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenJoinDoesNotAffectRowsButParticipantExistsAfterRetry()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectDto { Id = command.ProjectId, Title = "Book" }
            });

        _wordWarParticipantReadRepositoryMock
            .SetupSequence(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarParticipantsDto?)null)
            .ReturnsAsync(new EventWordWarParticipantsDto
            {
                Id = Guid.NewGuid(),
                WordWarId = command.WarId,
                UserId = command.UserId,
                ProjectId = command.ProjectId
            });

        _wordWarRepositoryMock
            .Setup(r => r.JoinAsync(command.WarId, command.UserId, command.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenJoinDoesNotAffectRowsAndUserStillNotParticipant()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .Setup(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            });

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectDto { Id = command.ProjectId, Title = "Book" }
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarParticipantsDto?)null);

        _wordWarRepositoryMock
            .Setup(r => r.JoinAsync(command.WarId, command.UserId, command.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Unable to join word war due to state conflict.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenJoinRacesWithStartAndStatusChanges()
    {
        var command = NewCommand();

        _wordWarReadRepositoryMock
            .SetupSequence(r => r.GetByIdAsync(command.WarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Waiting
            })
            .ReturnsAsync(new EventWordWarsDto
            {
                Id = command.WarId,
                Status = WordWarStatus.Running
            });

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectsAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectDto { Id = command.ProjectId, Title = "Book" }
            });

        _wordWarParticipantReadRepositoryMock
            .Setup(r => r.GetParticipant(command.WarId, command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventWordWarParticipantsDto?)null);

        _wordWarRepositoryMock
            .Setup(r => r.JoinAsync(command.WarId, command.UserId, command.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Can't join the word war when the status is not waiting.");
    }

    private JoinWordWarCommandHandler CreateHandler()
    {
        return new JoinWordWarCommandHandler(
            _loggerMock.Object,
            _wordWarReadRepositoryMock.Object,
            _projectReadRepositoryMock.Object,
            _wordWarParticipantReadRepositoryMock.Object,
            _wordWarRepositoryMock.Object);
    }

    private static JoinWordWarCommand NewCommand()
    {
        return new JoinWordWarCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
    }
}
