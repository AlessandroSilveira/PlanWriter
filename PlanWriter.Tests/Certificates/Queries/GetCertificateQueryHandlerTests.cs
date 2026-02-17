using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Common.WinnerEligibility;
using PlanWriter.Application.Certificates.Dtos.Queries;
using PlanWriter.Application.Certificates.Queries;
using PlanWriter.Domain.Dtos.Certificates;
using PlanWriter.Domain.Interfaces.ReadModels.Certificates;
using Xunit;

namespace PlanWriter.Tests.Certificates.Queries;

public class GetCertificateQueryHandlerTests
{
    private readonly Mock<ICertificateReadRepository> _certificateReadRepository = new();
    private readonly Mock<IWinnerEligibilityService> _winnerEligibilityService = new();
    private readonly Mock<ILogger<GetCertificateQueryHandler>> _logger = new();

    private readonly GetCertificateQueryHandler _handler;

    public GetCertificateQueryHandlerTests()
    {
        _handler = new GetCertificateQueryHandler(
            _certificateReadRepository.Object,
            _winnerEligibilityService.Object,
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
        _winnerEligibilityService
            .Setup(x => x.EvaluateForCertificate(true, true))
            .Returns(new WinnerEligibilityResult(true, false, "eligible", "Certificado liberado."));

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
    public async Task Handle_ShouldThrowNotFoundException_WhenRowNotFound()
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
            .ThrowAsync<NotFoundException>()
            .WithMessage("Projeto e evento não encontrados para este usuário.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenProjectEventIsNotValidated()
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
        _winnerEligibilityService
            .Setup(x => x.EvaluateForCertificate(false, true))
            .Returns(new WinnerEligibilityResult(false, false, "not_eligible",
                "O certificado é liberado apenas para participantes vencedores com validação final."));

        // ✅ ORDEM CORRETA
        var query = new GetCertificateQuery(eventId, projectId, "User", userId);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("O certificado é liberado apenas para participantes vencedores com validação final.");
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenProjectEventIsNotWinner()
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
        _winnerEligibilityService
            .Setup(x => x.EvaluateForCertificate(true, false))
            .Returns(new WinnerEligibilityResult(false, false, "not_eligible",
                "O certificado é liberado apenas para participantes vencedores com validação final."));

        // ✅ ORDEM CORRETA
        var query = new GetCertificateQuery(eventId, projectId, "User", userId);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("O certificado é liberado apenas para participantes vencedores com validação final.");
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
        _winnerEligibilityService
            .Setup(x => x.EvaluateForCertificate(true, true))
            .Returns(new WinnerEligibilityResult(true, false, "eligible", "Certificado liberado."));

        // ✅ ORDEM CORRETA
        var query = new GetCertificateQuery(eventId, projectId, "User", userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }
}
