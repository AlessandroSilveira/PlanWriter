using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Profile.Dtos.Queries;
using PlanWriter.Application.Profile.Queries;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.Users;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Profile.Queries;

public class GetPublicProfileQueryHandlerTests
{
    private readonly Mock<IUserReadRepository> _userReadRepositoryMock = new();
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IProjectEventsRepository> _projectEventsRepositoryMock = new();
    private readonly Mock<ILogger<GetPublicProfileQueryHandler>> _loggerMock = new();
    private readonly Mock<IProjectProgressReadRepository> _projectProgressReadRepositoryMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepositoryMock = new();

    private GetPublicProfileQueryHandler CreateHandler()
        => new(
            _userReadRepositoryMock.Object,
            _eventRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _loggerMock.Object,
            _projectProgressReadRepositoryMock.Object,
            _projectEventsReadRepositoryMock.Object
        );

    [Fact]
    public async Task Handle_ShouldReturnPublicProfile_WhenUserIsPublic()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var slug = "john-doe";

        var user = new User
        {
            Id = userId,
            Email = "john@test.com",
            DisplayName = "John Doe",
            Slug = slug,
            Bio = "Writer",
            AvatarUrl = "avatar.png",
            IsProfilePublic = true
        };

        var activeEvent = new EventDto(
            Guid.NewGuid(),
            "NaNoWriMo",
            "nano",
            "Global",
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(10),
            50000,
            true
        );

        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "My Book",
            CurrentWordCount = 12000,
            WordCountGoal = 50000
        };

        var projectEvent = new ProjectEvent
        {
            ProjectId = project.Id,
            EventId = activeEvent.Id,
            TargetWords = 50000
        };

        _userReadRepositoryMock
            .Setup(r => r.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _eventRepositoryMock
            .Setup(r => r.GetActiveEvents())
            .ReturnsAsync(new List<EventDto> { activeEvent });

        _projectRepositoryMock
            .Setup(r => r.GetPublicProjectsByUserIdAsync(userId))
            .ReturnsAsync(new List<Project> { project });

        _projectEventsRepositoryMock
            .Setup(r => r.GetByProjectAndEventAsync(project.Id, activeEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectEvent);

        _projectProgressReadRepositoryMock
            .Setup(r => r.GetTotalWordsByUsersAsync(
                It.IsAny<IEnumerable<Guid>>(),
                activeEvent.StartsAtUtc,
                activeEvent.EndsAtUtc))
            .ReturnsAsync(new Dictionary<Guid, int>
            {
                { userId, 20000 }
            });

        
        
       
        var handler = CreateHandler();
        var query = new GetPublicProfileQuery(slug);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.DisplayName.Should().Be("John Doe");
        result.Projects.Should().HaveCount(1);

        var projectSummary = result.Projects[0];
        projectSummary.EventTotalWritten.Should().Be(20000);
        projectSummary.EventTargetWords.Should().Be(50000);
        projectSummary.EventPercent.Should().Be(40);
        projectSummary.ActiveEventName.Should().Be("NaNoWriMo");

        result.Highlight.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProfileIsNotPublic()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Slug = "private-user",
            IsProfilePublic = false
        };

        _userReadRepositoryMock
            .Setup(r => r.GetBySlugAsync("private-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var query = new GetPublicProfileQuery("private-user");

        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Perfil não é público.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserDoesNotExist()
    {
        _userReadRepositoryMock
            .Setup(r => r.GetBySlugAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var query = new GetPublicProfileQuery("unknown");

        Func<Task> act = async () =>
            await handler.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Perfil não encontrado.");
    }

    [Fact]
    public async Task Handle_ShouldReturnHighlight_WhenUserHasRecentWin()
    {
        var userId = Guid.NewGuid();
        var slug = "winner";

        var user = new User
        {
            Id = userId,
            Slug = slug,
            IsProfilePublic = true
        };

        var winEvent = new Event { Name = "NaNoWriMo" };

        _userReadRepositoryMock
            .Setup(r => r.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _eventRepositoryMock
            .Setup(r => r.GetActiveEvents())
            .ReturnsAsync(new List<EventDto>());

        _projectRepositoryMock
            .Setup(r => r.GetPublicProjectsByUserIdAsync(userId))
            .ReturnsAsync(new List<Project>());

      

        var handler = CreateHandler();
        var query = new GetPublicProfileQuery(slug);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Highlight.Should().Be("Winner — NaNoWriMo");
    }
}