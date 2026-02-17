using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Common.WinnerEligibility;
using PlanWriter.Application.Goodies.Dtos.Queries;
using PlanWriter.Application.Goodies.Queries;
using PlanWriter.Domain.Dtos.Events;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.ReadModels.Badges;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using IEventReadRepository = PlanWriter.Domain.Interfaces.ReadModels.Events.IEventReadRepository;
using Xunit;

namespace PlanWriter.Tests.Goodies.Queries;

public class GetEventGoodiesQueryHandlerTests
{
    private readonly Mock<ILogger<GetEventGoodiesQueryHandler>> _loggerMock = new();
    private readonly Mock<IEventReadRepository> _eventReadRepositoryMock = new();
    private readonly Mock<IProjectReadRepository> _projectReadRepositoryMock = new();
    private readonly Mock<IProjectEventsReadRepository> _projectEventsReadRepositoryMock = new();
    private readonly Mock<IProjectProgressReadRepository> _projectProgressReadRepositoryMock = new();
    private readonly Mock<IBadgeReadRepository> _badgeReadRepositoryMock = new();
    private readonly Mock<IWinnerEligibilityService> _winnerEligibilityServiceMock = new();

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenEventDoesNotExist()
    {
        var query = new GetEventGoodiesQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _eventReadRepositoryMock
            .Setup(x => x.GetEventByIdAsync(query.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

        var sut = CreateHandler();
        var act = async () => await sut.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Evento não encontrado.");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenProjectDoesNotBelongToUser()
    {
        var query = new GetEventGoodiesQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _eventReadRepositoryMock
            .Setup(x => x.GetEventByIdAsync(query.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                query.EventId,
                "NaNoWriMo",
                "nanowrimo",
                "Nanowrimo",
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(10),
                50000,
                true));

        _projectReadRepositoryMock
            .Setup(x => x.GetUserProjectByIdAsync(query.ProjectId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var sut = CreateHandler();
        var act = async () => await sut.Handle(query, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Projeto não encontrado.");
    }

    [Fact]
    public async Task Handle_ShouldReturnAggregatedGoodies_WhenDataIsValid()
    {
        var now = DateTime.UtcNow;
        var query = new GetEventGoodiesQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _eventReadRepositoryMock
            .Setup(x => x.GetEventByIdAsync(query.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                query.EventId,
                "Evento de Fevereiro",
                "fev-2026",
                "Nanowrimo",
                now.AddDays(-30),
                now.AddDays(2),
                50000,
                true));

        _projectReadRepositoryMock
            .Setup(x => x.GetUserProjectByIdAsync(query.ProjectId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project
            {
                Id = query.ProjectId,
                Title = "Meu Romance"
            });

        _projectEventsReadRepositoryMock
            .Setup(x => x.GetByProjectAndEventWithEventAsync(query.ProjectId, query.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                ProjectId = query.ProjectId,
                EventId = query.EventId,
                TargetWords = 50000,
                Won = true,
                ValidatedAtUtc = now,
                FinalWordCount = 52000
            });

        _projectProgressReadRepositoryMock
            .Setup(x => x.GetProgressByProjectIdAsync(query.ProjectId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectProgress { ProjectId = query.ProjectId, CreatedAt = now.AddDays(-3), WordsWritten = 1000 },
                new ProjectProgress { ProjectId = query.ProjectId, CreatedAt = now.AddDays(-2), WordsWritten = 900 }
            });

        _winnerEligibilityServiceMock
            .Setup(x => x.EvaluateForGoodies(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                50000,
                52000,
                true,
                true))
            .Returns(new WinnerEligibilityResult(
                IsEligible: true,
                CanValidate: false,
                Status: "eligible",
                Message: "Parabéns! Você liberou os goodies de vencedor."));

        _badgeReadRepositoryMock
            .Setup(x => x.GetByProjectIdAsync(query.ProjectId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Badge
                {
                    Id = 1,
                    Name = "Winner",
                    Description = "Evento concluído",
                    Icon = "🏆",
                    ProjectId = query.ProjectId,
                    EventId = query.EventId,
                    AwardedAt = now
                },
                new Badge
                {
                    Id = 2,
                    Name = "First Steps",
                    Description = "Badge geral",
                    Icon = "✨",
                    ProjectId = query.ProjectId,
                    EventId = null,
                    AwardedAt = now
                }
            });

        var sut = CreateHandler();
        var result = await sut.Handle(query, CancellationToken.None);

        result.EventId.Should().Be(query.EventId);
        result.ProjectId.Should().Be(query.ProjectId);
        result.ProjectTitle.Should().Be("Meu Romance");
        result.TotalWords.Should().Be(52000);
        result.Eligibility.IsEligible.Should().BeTrue();
        result.Certificate.Available.Should().BeTrue();
        result.Certificate.DownloadUrl.Should().Be($"/api/events/{query.EventId}/projects/{query.ProjectId}/certificate");
        result.Badges.Should().HaveCount(1);
        result.Badges[0].Name.Should().Be("Winner");
    }

    [Fact]
    public async Task Handle_ShouldReturnPendingValidation_WhenTargetReachedWithoutValidation()
    {
        var now = DateTime.UtcNow;
        var query = new GetEventGoodiesQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _eventReadRepositoryMock
            .Setup(x => x.GetEventByIdAsync(query.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventDto(
                query.EventId,
                "Evento",
                "evento",
                "Nanowrimo",
                now.AddDays(-20),
                now.AddDays(3),
                50000,
                true));

        _projectReadRepositoryMock
            .Setup(x => x.GetUserProjectByIdAsync(query.ProjectId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = query.ProjectId, Title = "Projeto X" });

        _projectEventsReadRepositoryMock
            .Setup(x => x.GetByProjectAndEventWithEventAsync(query.ProjectId, query.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEvent
            {
                ProjectId = query.ProjectId,
                EventId = query.EventId,
                TargetWords = 50000,
                Won = false,
                ValidatedAtUtc = null,
                FinalWordCount = null
            });

        _projectProgressReadRepositoryMock
            .Setup(x => x.GetProgressByProjectIdAsync(query.ProjectId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProjectProgress { ProjectId = query.ProjectId, CreatedAt = now.AddDays(-2), WordsWritten = 30000 },
                new ProjectProgress { ProjectId = query.ProjectId, CreatedAt = now.AddDays(-1), WordsWritten = 20000 }
            });

        _winnerEligibilityServiceMock
            .Setup(x => x.EvaluateForGoodies(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                50000,
                50000,
                false,
                false))
            .Returns(new WinnerEligibilityResult(
                IsEligible: false,
                CanValidate: true,
                Status: "pending_validation",
                Message: "Meta atingida. Faça a validação final para liberar os goodies."));

        _badgeReadRepositoryMock
            .Setup(x => x.GetByProjectIdAsync(query.ProjectId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Badge>());

        var sut = CreateHandler();
        var result = await sut.Handle(query, CancellationToken.None);

        result.Eligibility.CanValidate.Should().BeTrue();
        result.Certificate.Available.Should().BeFalse();
        result.Certificate.Message.Should().Be("Faça a validação final para liberar o certificado.");
    }

    private GetEventGoodiesQueryHandler CreateHandler()
    {
        return new GetEventGoodiesQueryHandler(
            _loggerMock.Object,
            _eventReadRepositoryMock.Object,
            _projectReadRepositoryMock.Object,
            _projectEventsReadRepositoryMock.Object,
            _projectProgressReadRepositoryMock.Object,
            _badgeReadRepositoryMock.Object,
            _winnerEligibilityServiceMock.Object);
    }
}
