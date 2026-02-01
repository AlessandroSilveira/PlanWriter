using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Projects.Commands;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Projects.Commands;

public class DeleteProgressCommandHandlerTests
{
    private readonly Mock<ILogger<DeleteProgressCommandHandler>> _logger = new();
    private readonly Mock<IProjectProgressReadRepository> _progressReadRepo = new();
    private readonly Mock<IProjectProgressRepository> _progressWriteRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();

    private DeleteProgressCommandHandler CreateHandler()
        => new(
            _logger.Object,
            _progressReadRepo.Object,
            _progressWriteRepo.Object,
            _projectRepo.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenProgressNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _progressReadRepo
            .Setup(r => r.GetByIdAsync(progressId, userId, ct))
            .ReturnsAsync((ProgressRow?)null);

        var handler = CreateHandler();
        var cmd = new DeleteProgressCommand(progressId, userId);

        // Act
        var result = await handler.Handle(cmd, ct);

        // Assert
        result.Should().BeFalse();

        _progressWriteRepo.Verify(w => w.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _projectRepo.Verify(p => p.UpdateAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenDeleteReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var progressDate = new DateTime(2026, 02, 01);
        var ct = CancellationToken.None;

        _progressReadRepo
            .Setup(r => r.GetByIdAsync(progressId, userId, ct))
            .ReturnsAsync(new ProgressRow(progressId, projectId, progressDate));

        _progressWriteRepo
            .Setup(w => w.DeleteAsync(progressId, userId))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var cmd = new DeleteProgressCommand(progressId, userId);

        // Act
        var result = await handler.Handle(cmd, ct);

        // Assert
        result.Should().BeFalse();

        _progressReadRepo.Verify(r => r.GetLastTotalBeforeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        _projectRepo.Verify(p => p.UpdateAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenProjectNotFoundDuringRecalc()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var progressDate = new DateTime(2026, 02, 01);
        var ct = CancellationToken.None;

        _progressReadRepo
            .Setup(r => r.GetByIdAsync(progressId, userId, ct))
            .ReturnsAsync(new ProgressRow(progressId, projectId, progressDate));

        _progressWriteRepo
            .Setup(w => w.DeleteAsync(progressId, userId))
            .ReturnsAsync(true);

        _progressReadRepo
            .Setup(r => r.GetLastTotalBeforeAsync(projectId, userId, progressDate, ct))
            .ReturnsAsync(1234);

        _projectRepo
            .Setup(p => p.GetUserProjectByIdAsync(projectId, userId))
            .ReturnsAsync((Project?)null);

        var handler = CreateHandler();
        var cmd = new DeleteProgressCommand(progressId, userId);

        // Act
        var result = await handler.Handle(cmd, ct);

        // Assert
        result.Should().BeFalse();

        _projectRepo.Verify(p => p.UpdateAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRecalculateProjectTotalAndReturnTrue_WhenDeleteSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var progressDate = new DateTime(2026, 02, 01);
        var ct = CancellationToken.None;

        _progressReadRepo
            .Setup(r => r.GetByIdAsync(progressId, userId, ct))
            .ReturnsAsync(new ProgressRow(progressId, projectId, progressDate));

        _progressWriteRepo
            .Setup(w => w.DeleteAsync(progressId, userId))
            .ReturnsAsync(true);

        // exemplo: progress anterior deixa o total em 4500
        _progressReadRepo
            .Setup(r => r.GetLastTotalBeforeAsync(projectId, userId, progressDate, ct))
            .ReturnsAsync(4500);

        var project = new Project
        {
            Id = projectId,
            UserId = userId,
            CurrentWordCount = 9999 // valor antigo (vai ser recalculado)
        };

        _projectRepo
            .Setup(p => p.GetUserProjectByIdAsync(projectId, userId))
            .ReturnsAsync(project);

        _projectRepo
            .Setup(p => p.UpdateAsync(It.IsAny<Project>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var cmd = new DeleteProgressCommand(progressId, userId);

        // Act
        var result = await handler.Handle(cmd, ct);

        // Assert
        result.Should().BeTrue();

        project.CurrentWordCount.Should().Be(4500);

        _projectRepo.Verify(p =>
            p.UpdateAsync(It.Is<Project>(x => x.Id == projectId && x.CurrentWordCount == 4500)),
            Times.Once);

        _progressReadRepo.Verify(r => r.GetByIdAsync(progressId, userId, ct), Times.Once);
        _progressWriteRepo.Verify(w => w.DeleteAsync(progressId, userId), Times.Once);
        _progressReadRepo.Verify(r => r.GetLastTotalBeforeAsync(projectId, userId, progressDate, ct), Times.Once);
        _projectRepo.Verify(p => p.GetUserProjectByIdAsync(projectId, userId), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetProjectTotalToZero_WhenNoPreviousProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var progressDate = new DateTime(2026, 02, 01);
        var ct = CancellationToken.None;

        _progressReadRepo
            .Setup(r => r.GetByIdAsync(progressId, userId, ct))
            .ReturnsAsync(new ProgressRow(progressId, projectId, progressDate));

        _progressWriteRepo
            .Setup(w => w.DeleteAsync(progressId, userId))
            .ReturnsAsync(true);

        // repositório devolve 0 quando não tem anterior
        _progressReadRepo
            .Setup(r => r.GetLastTotalBeforeAsync(projectId, userId, progressDate, ct))
            .ReturnsAsync(0);

        var project = new Project
        {
            Id = projectId,
            UserId = userId,
            CurrentWordCount = 7777
        };

        _projectRepo
            .Setup(p => p.GetUserProjectByIdAsync(projectId, userId))
            .ReturnsAsync(project);

        _projectRepo
            .Setup(p => p.UpdateAsync(It.IsAny<Project>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var cmd = new DeleteProgressCommand(progressId, userId);

        // Act
        var result = await handler.Handle(cmd, ct);

        // Assert
        result.Should().BeTrue();
        project.CurrentWordCount.Should().Be(0);

        _projectRepo.Verify(p =>
            p.UpdateAsync(It.Is<Project>(x => x.Id == projectId && x.CurrentWordCount == 0)),
            Times.Once);
    }
}
