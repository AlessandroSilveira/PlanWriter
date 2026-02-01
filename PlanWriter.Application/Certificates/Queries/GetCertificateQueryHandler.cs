using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Certificates.Dtos.Queries;
using PlanWriter.Domain.Interfaces.Repositories;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PlanWriter.Application.Certificates.Queries;

public class GetCertificateQueryHandler(PlanWriter.Domain.Interfaces.Repositories.IProjectEventsRepository projectEventsRepository, IProjectRepository projectRepository,
     ILogger<GetCertificateQueryHandler> logger)
    : IRequestHandler<GetCertificateQuery, byte[]>
{
    public async Task<byte[]> Handle(GetCertificateQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting certificate for project {ProjectId}", request.ProjectId);

        var projectEvent =
            await projectEventsRepository
                .GetProjectEventByProjectIdAndEventId(request.ProjectId, request.EventId);

        if (projectEvent?.ValidatedAtUtc is null || !projectEvent.Won)
            throw new InvalidOperationException("Certificate is available only for validated winners.");

        var project = await projectRepository
            .GetProjectById(request.ProjectId);

        var projectTitle = project?.Title ?? "Projeto";

        var pdf = GenerateWinnerCertificate(
            request.UserName,
            projectTitle,
            projectEvent.Event!.Name,
            projectEvent.FinalWordCount ?? 0
        );

        logger.LogInformation("Certificate generated for project {ProjectId}", request.ProjectId);

        return pdf;
    }

    private byte[] GenerateWinnerCertificate(string requestUserName, string projectTitle, string name, int projectEventFinalWordCount)
    {
        
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Background(Colors.Grey.Lighten3);
                    page.Header().AlignCenter().Text(name).FontSize(24).SemiBold();
                    page.Content().AlignCenter().Column(col =>
                    {
                        col.Item().Text("CERTIFICADO DE VENCEDOR").FontSize(32).Bold();
                        col.Item().PaddingTop(20).Text(requestUserName).FontSize(22);
                        col.Item().Text($"venceu com o projeto “{projectTitle}”").FontSize(16);
                        col.Item().PaddingTop(10).Text($"Palavras no evento: {projectEventFinalWordCount:N0}").FontSize(14);
                        col.Item().PaddingTop(20).Text("Parabéns!").FontSize(18);
                    });
                    page.Footer().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy")).FontSize(10);
                });
            }).GeneratePdf();
        
    }
}