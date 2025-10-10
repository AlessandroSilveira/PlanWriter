using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Domain.Interfaces.Services;

// PlanWriter.Application/Services/IValidationService.cs
public interface IValidationService
{
    Task<ValidationResultDto> ValidateTextAsync(Guid projectId, string text, bool save, CancellationToken ct);
    Task<ValidationResultDto> ValidateUploadAsync(Guid projectId, string fileName, Stream fileStream, bool save, CancellationToken ct);
}
