using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Certificates.Dtos.Queries;

namespace PlanWriter.API.Controllers;

// Controllers/CertificatesController.cs
[ApiController]
[Route("api/events")]
public class CertificatesController(IMediator mediator) : ControllerBase
{
    [HttpGet("{eventId:guid}/projects/{projectId:guid}/certificate")]
    public async Task<IActionResult> GetCertificate(Guid eventId, Guid projectId)
    {
        var userName = User?.Identity?.Name ?? "Autor(a)";
        var pdf =  await mediator.Send(new GetCertificateQuery(eventId, projectId, userName));

        // var pe = await db.ProjectEvents.Include(x => x.Event)
        //     .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.EventId == eventId);
        //
        // if (pe == null || pe.ValidatedAtUtc == null || !pe.Won)
        //     return NotFound("Certificado disponível apenas para vencedores após validação.");
        //
        // // pegue o projeto/usuário
        // var project = await db.Set<Project>().FirstOrDefaultAsync(p => p.Id == projectId);
         //var userName = User?.Identity?.Name ?? "Autor(a)";
        // var projectTitle = project?.Title ?? "Projeto";
        //var pdf = cert.GenerateWinnerCertificate(userName, projectTitle, pe.Event!.Name, pe.FinalWordCount ?? 0);
        //
         return File(pdf, "application/pdf", $"certificado.pdf");
    }
}
