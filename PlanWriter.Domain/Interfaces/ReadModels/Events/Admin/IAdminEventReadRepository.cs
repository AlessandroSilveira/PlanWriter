using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Events;

namespace PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;

public interface IAdminEventReadRepository
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);
    Task<EventDto?> GetByIdAsync(Guid eventId, CancellationToken ct);
    Task<IReadOnlyList<EventDto>?> GetAllAsync(CancellationToken cancellationToken);
}