// PlanWriter.Domain/Dtos/ValidateUploadForm.cs

using System;
using Microsoft.AspNetCore.Http;

namespace PlanWriter.Domain.Dtos;

public class ValidateUploadForm
{
    public Guid ProjectId { get; set; }          // outros campos simples do form
    public string? Notes { get; set; }           // opcional
    public IFormFile File { get; set; } = default!; // ARQUIVO (obrigatório no upload)
}