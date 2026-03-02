using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Projects.Dtos.Queries;
using PlanWriter.Application.Projects.Queries;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using Xunit;

namespace PlanWriter.Tests.Projects.Queries;

public class GetProjectDraftQueryHandlerTests
{
    private readonly Mock<ILogger<GetProjectDraftQueryHandler>> _loggerMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();
    private readonly Mock<IProjectDraftReadRepository> _projectDraftReadRepositoryMock = new();

    private GetProjectDraftQueryHandler CreateHandler()
        => new(
            _loggerMock.Object,
            _projectReadRepositoryMock.Object,
            _projectDraftReadRepositoryMock.Object);

    [Fact]
    public async Task Handle_ShouldReturnDraft_WhenItExists()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, UserId = userId });

        _projectDraftReadRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDraftDto
            {
                ProjectId = projectId,
                HtmlContent = "<h1>Rascunho</h1>",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        var result = await CreateHandler().Handle(new GetProjectDraftQuery(projectId, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.HtmlContent.Should().Be("<h1>Rascunho</h1>");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenDraftDoesNotExist()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, UserId = userId });

        _projectDraftReadRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectDraftDto?)null);

        var result = await CreateHandler().Handle(new GetProjectDraftQuery(projectId, userId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenProjectDoesNotBelongToUser()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _projectReadRepositoryMock
            .Setup(r => r.GetUserProjectByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var act = () => CreateHandler().Handle(new GetProjectDraftQuery(projectId, userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Project not found.");
        _projectDraftReadRepositoryMock.VerifyNoOtherCalls();
    }
}
