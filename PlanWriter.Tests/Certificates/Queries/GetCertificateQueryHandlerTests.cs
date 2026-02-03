using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Certificates.Dtos.Queries;
using PlanWriter.Application.Certificates.Queries;
using PlanWriter.Domain.Dtos.Certificates;
using PlanWriter.Domain.Interfaces.ReadModels.Certificates;
using Xunit;

namespace PlanWriter.Tests.Certificates.Queries;

public class GetCertificateQueryHandlerTests
{
    private readonly Mock<ICertificateReadRepository> _certificateReadRepository = new();
    private readonly Mock<ILogger<GetCertificateQueryHandler>> _logger = new();

    private readonly GetCertificateQueryHandler _handler;

    public GetCertificateQueryHandlerTests()
    {
        _handler = new GetCertificateQueryHandler(
            _certificateReadRepository.Object,
            _logger.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnPdfBytes_WhenProjectEventIsValidatedWinner()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _certificateReadRepository
            .Setup(r => r.GetWinnerRowAsync(projectId, eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateWinnerRow
            {
                EventName = "NaNoWriMo",
                ProjectTitle = "Meu Romance",
                Won = true,
                ValidatedAtUtc = DateTime.UtcNow,
                FinalWordCount = 50000
            });

        // ✅ ORDEM CORRETA: (EventId, ProjectId, UserName, UserId)
        var query = new GetCertificateQuery(
            eventId,
            projectId,
            "Alessandro",
            userId
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().BeOfType<byte[]>();

        _certificateReadRepository.Verify(
            r => r.GetWinnerRowAsync(projectId, eventId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenRowNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _certificateReadRepository
            .Setup(r => r.GetWinnerRowAsync(projectId, eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateWinnerRow?)null);

        // ✅ ORDEM CORRETA
        var query = new GetCertificateQuery(eventId, projectId, "User", userId);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Project/Event not found for this user.");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenProjectEventIsNotValidated()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _certificateReadRepository
            .Setup(r => r.GetWinnerRowAsync(projectId, eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateWinnerRow
            {
                EventName = "Evento Teste",
                ProjectTitle = "Projeto X",
                Won = true,
                ValidatedAtUtc = null,
                FinalWordCount = 123
            });

        // ✅ ORDEM CORRETA
        var query = new GetCertificateQuery(eventId, projectId, "User", userId);

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
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _certificateReadRepository
            .Setup(r => r.GetWinnerRowAsync(projectId, eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateWinnerRow
            {
                EventName = "Evento Teste",
                ProjectTitle = "Projeto Y",
                Won = false,
                ValidatedAtUtc = DateTime.UtcNow,
                FinalWordCount = 999
            });

        // ✅ ORDEM CORRETA
        var query = new GetCertificateQuery(eventId, projectId, "User", userId);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Certificate is available only for validated winners.");
    }

    [Fact]
    public async Task Handle_ShouldGenerateCertificate_WhenProjectTitleIsDefault()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _certificateReadRepository
            .Setup(r => r.GetWinnerRowAsync(projectId, eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateWinnerRow
            {
                EventName = "Evento Teste",
                ProjectTitle = "Projeto",
                Won = true,
                ValidatedAtUtc = DateTime.UtcNow,
                FinalWordCount = 12345
            });

        // ✅ ORDEM CORRETA
        var query = new GetCertificateQuery(eventId, projectId, "User", userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }
}
