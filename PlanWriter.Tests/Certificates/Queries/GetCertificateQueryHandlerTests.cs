using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Certificates.Dtos.Queries;
using PlanWriter.Application.Certificates.Queries;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
using PlanWriter.Domain.Interfaces.Repositories;
using Xunit;

namespace PlanWriter.Tests.Certificates.Queries;

public class GetCertificateQueryHandlerTests
{
    private readonly Mock<PlanWriter.Domain.Interfaces.Repositories.IProjectEventsRepository> _projectEventsRepository;
    private readonly Mock<IProjectRepository> _projectRepository;
    private readonly Mock<ILogger<GetCertificateQueryHandler>> _logger;

    private readonly GetCertificateQueryHandler _handler;

    public GetCertificateQueryHandlerTests()
    {
        _projectEventsRepository = new Mock<PlanWriter.Domain.Interfaces.Repositories.IProjectEventsRepository>();
        _projectRepository = new Mock<IProjectRepository>();
        _logger = new Mock<ILogger<GetCertificateQueryHandler>>();

        _handler = new GetCertificateQueryHandler(
            _projectEventsRepository.Object,
            _projectRepository.Object,
            _logger.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnPdfBytes_WhenProjectEventIsValidatedWinner()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var projectEvent = new ProjectEvent
        {
            ProjectId = projectId,
            EventId = eventId,
            Won = true,
            ValidatedAtUtc = DateTime.UtcNow,
            FinalWordCount = 50000,
            Event = new Event
            {
                Name = "NaNoWriMo"
            }
        };

        var project = new Project
        {
            Id = projectId,
            Title = "Meu Romance"
        };
        
        _projectEventsRepository
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(projectEvent);


        _projectRepository
            .Setup(r => r.GetProjectById(It.IsAny<Guid>()))
            .ReturnsAsync(project);

        var query = new GetCertificateQuery
        (
            projectId,
            eventId,
            "Alessandro"
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().BeOfType<byte[]>();

      

        
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenProjectEventIsNotValidated()
    {
        // Arrange
        var projectEvent = new ProjectEvent
        {
            Won = true,
            ValidatedAtUtc = null
        };

        _projectEventsRepository
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(projectEvent);

        var query = new GetCertificateQuery
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User"
        );

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Certificate is available only for validated winners.");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenProjectEventIsNotWinner()
    {
        // Arrange
        var projectEvent = new ProjectEvent
        {
            Won = false,
            ValidatedAtUtc = DateTime.UtcNow
        };

        _projectEventsRepository
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(projectEvent);

        var query = new GetCertificateQuery
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User"
        );

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Certificate is available only for validated winners.");
    }

    [Fact]
    public async Task Handle_ShouldGenerateCertificate_WhenProjectIsNull_UsingDefaultTitle()
    {
        // Arrange
        var projectEvent = new ProjectEvent
        {
            Won = true,
            ValidatedAtUtc = DateTime.UtcNow,
            FinalWordCount = 12345,
            Event = new Event
            {
                Name = "Evento Teste"
            }
        };

        _projectEventsRepository
            .Setup(r => r.GetProjectEventByProjectIdAndEventId(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(projectEvent);

        _projectRepository
            .Setup(r => r.GetProjectById(It.IsAny<Guid>()))
            .ReturnsAsync((Project?)null);

        var query = new GetCertificateQuery
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User"
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }
}