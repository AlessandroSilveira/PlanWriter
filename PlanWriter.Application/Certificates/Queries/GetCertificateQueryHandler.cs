using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Common.Exceptions;
using PlanWriter.Application.Common.WinnerEligibility;
using PlanWriter.Application.Certificates.Dtos.Queries;
using PlanWriter.Domain.Interfaces.ReadModels.Certificates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PlanWriter.Application.Certificates.Queries;

public class GetCertificateQueryHandler(
    ICertificateReadRepository readRepository,
    IWinnerEligibilityService winnerEligibilityService,
    ILogger<GetCertificateQueryHandler> logger)
    : IRequestHandler<GetCertificateQuery, byte[]>
{
    public async Task<byte[]> Handle(GetCertificateQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting certificate for project {ProjectId} event {EventId}", request.ProjectId, request.EventId);

        var row = await readRepository.GetWinnerRowAsync(request.ProjectId, request.EventId, request.UserId, ct);

        if (row is null)
            throw new NotFoundException("Projeto e evento não encontrados para este usuário.");

        var eligibility = winnerEligibilityService.EvaluateForCertificate(
            row.ValidatedAtUtc is not null,
            row.Won);

        if (!eligibility.IsEligible)
            throw new BusinessRuleException(eligibility.Message);

        var pdf = GenerateWinnerCertificate(request.UserName, row.ProjectTitle, row.EventName, row.FinalWordCount);

        logger.LogInformation("Certificate generated for project {ProjectId}", request.ProjectId);

        return pdf;
    }

    private static byte[] GenerateWinnerCertificate(string requestUserName, string projectTitle, string eventName, int finalWordCount)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Background(Colors.Grey.Lighten3);

                page.Header()
                    .AlignCenter()
                    .Text(eventName)
                    .FontSize(24)
                    .SemiBold();

                page.Content()
                    .AlignCenter()
                    .Column(col =>
                    {
                        col.Item().Text("CERTIFICADO DE VENCEDOR").FontSize(32).Bold();
                        col.Item().PaddingTop(20).Text(requestUserName).FontSize(22);
                        col.Item().Text($"venceu com o projeto “{projectTitle}”").FontSize(16);
                        col.Item().PaddingTop(10).Text($"Palavras no evento: {finalWordCount:N0}").FontSize(14);
                        col.Item().PaddingTop(20).Text("Parabéns!").FontSize(18);
                    });

                page.Footer()
                    .AlignRight()
                    .Text(DateTime.UtcNow.ToString("dd/MM/yyyy"))
                    .FontSize(10);
            });
        }).GeneratePdf();
    }
}
