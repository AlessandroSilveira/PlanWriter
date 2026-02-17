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
        var user = User;
        if (user?.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userName = user.Identity?.Name ?? "Autor(a)";
        var userId = userService.GetUserId(user);
        
        var pdf =  await mediator.Send(new GetCertificateQuery(eventId, projectId, userName, userId));
        
         return File(pdf, "application/pdf", $"certificado.pdf");
    }
}
