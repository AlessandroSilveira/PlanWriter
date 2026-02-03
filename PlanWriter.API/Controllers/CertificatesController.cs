using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Application.Certificates.Dtos.Queries;
using PlanWriter.Application.Interfaces;

namespace PlanWriter.API.Controllers;

// Controllers/CertificatesController.cs
[ApiController]
[Route("api/events")]
public class CertificatesController(IMediator mediator, IUserService userService) : ControllerBase
{
    [HttpGet("{eventId:guid}/projects/{projectId:guid}/certificate")]
    public async Task<IActionResult> GetCertificate(Guid eventId, Guid projectId)
    {
        var userName = User?.Identity?.Name ?? "Autor(a)";
        var userId = userService.GetUserId(User);
        
        var pdf =  await mediator.Send(new GetCertificateQuery(eventId, projectId, userName, userId));
        
         return File(pdf, "application/pdf", $"certificado.pdf");
    }
}
