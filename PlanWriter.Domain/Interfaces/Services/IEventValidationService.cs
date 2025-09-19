using System;
using System.Threading.Tasks;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IEventValidationService
{
    Task<(int target, int total)> PreviewAsync(Guid userId, Guid eventId, Guid projectId);
    Task ValidateAsync(Guid userId, Guid eventId, Guid projectId, int words, string source);
}