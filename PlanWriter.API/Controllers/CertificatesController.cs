using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Services;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.API.Controllers;

// Controllers/CertificatesController.cs
[ApiController]
[Route("api/events")]
public class CertificatesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICertificateService _cert;

    public CertificatesController(AppDbContext db, ICertificateService cert)
    {
        _db = db; _cert = cert;
    }

    [HttpGet("{eventId:guid}/projects/{projectId:guid}/certificate")]
    public async Task<IActionResult> GetCertificate(Guid eventId, Guid projectId)
    {
        var pe = await _db.ProjectEvents.Include(x => x.Event)
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.EventId == eventId);

        if (pe == null || pe.ValidatedAtUtc == null || !pe.Won)
            return NotFound("Certificado disponível apenas para vencedores após validação.");

        // pegue o projeto/usuário
        var project = await _db.Set<Project>().FirstOrDefaultAsync(p => p.Id == projectId);
        var userName = User?.Identity?.Name ?? "Autor(a)";
        var projectTitle = project?.Title ?? "Projeto";
        var pdf = _cert.GenerateWinnerCertificate(userName, projectTitle, pe.Event!.Name, pe.FinalWordCount ?? 0);

        return File(pdf, "application/pdf", $"certificado-{pe.Event.Slug}.pdf");
    }
}
