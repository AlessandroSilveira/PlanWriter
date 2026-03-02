using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Projects.Commands;
using PlanWriter.Application.Projects.Dtos.Commands;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Projects.Commands;

public class SaveProjectDraftCommandHandlerTests
{
    private readonly Mock<ILogger<SaveProjectDraftCommandHandler>> _loggerMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();
    private readonly Mock<IProjectDraftRepository> _projectDraftRepositoryMock = new();

    private SaveProjectDraftCommandHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _projectReadRepositoryMock.Object,
            _projectDraftRepositoryMock.Object);

    [Fact]
    public async Task Handle_ShouldSaveDraft_WhenProjectBelongsToUser()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var draft = new SaveProjectDraftDto { HtmlContent = "<p>Texto rico</p>" };

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, UserId = userId });

        _projectDraftRepositoryMock
            .Setup(r => r.UpsertAsync(projectId, userId, draft.HtmlContent, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDraftDto
            {
                ProjectId = projectId,
                HtmlContent = draft.HtmlContent,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        var result = await CreateHandler().Handle(new SaveProjectDraftCommand(projectId, userId, draft), CancellationToken.None);

        result.ProjectId.Should().Be(projectId);
        result.HtmlContent.Should().Be("<p>Texto rico</p>");

        _projectDraftRepositoryMock.Verify(
            r => r.UpsertAsync(projectId, userId, draft.HtmlContent, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenProjectDoesNotBelongToUser()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var act = () => CreateHandler().Handle(
            new SaveProjectDraftCommand(projectId, userId, new SaveProjectDraftDto { HtmlContent = "<p>Draft</p>" }),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Project not found.");
        _projectDraftRepositoryMock.VerifyNoOtherCalls();
    }
}
