// PlanWriter.API/Controllers/ValidationController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Services;

namespace PlanWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ValidationController(IValidationService service) : ControllerBase
{
    [HttpPost("text")]
    [RequestSizeLimit(2_000_000)]
    public async Task<ActionResult<ValidationResultDto>> ValidateText([FromBody] ValidateTextRequest req, CancellationToken ct)
    {
        var res = await service.ValidateTextAsync(req.ProjectId, req.Text, req.Save, ct);
        return Ok(res);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ValidationResultDto>> ValidateUpload([FromForm] Guid projectId, [FromForm] bool save, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Arquivo obrigatório.");
        if (file.Length > 10_000_000) return BadRequest("Arquivo muito grande (limite 10MB).");

        await using var stream = file.OpenReadStream();
        var res = await service.ValidateUploadAsync(projectId, file.FileName, stream, save, ct);
        return Ok(res);
    }
}