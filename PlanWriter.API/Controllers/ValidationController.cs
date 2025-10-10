// PlanWriter.API/Controllers/ValidationController.cs
using Microsoft.AspNetCore.Mvc;
using PlanWriter.Domain.Dtos;

[ApiController]
[Route("api/[controller]")]
public class ValidationController : ControllerBase
{
    // ... injete seus services no ctor

    /// <summary>Valida por upload de arquivo.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")] // <- ESSENCIAL pro Swagger
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateUpload([FromForm] ValidateUploadForm form, CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest("Arquivo ausente ou vazio.");

        // leia o stream se precisar
        using var stream = form.File.OpenReadStream();
        // chame seu serviço de validação passando stream / projectId / notes
        // var result = await _validationService.ValidateUploadAsync(form.ProjectId, stream, form.File.FileName, form.Notes, ct);

        // return Ok(result);
        return Ok(new { ok = true }); // placeholder
    }

    /// <summary>Valida por texto (JSON).</summary>
    [HttpPost("text")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateText([FromBody] ValidateTextDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto?.Text))
            return BadRequest("Texto vazio.");

        // var result = await _validationService.ValidateTextAsync(dto.ProjectId, dto.Text, dto.Notes, ct);
        // return Ok(result);
        return Ok(new { ok = true }); // placeholder
    }
}