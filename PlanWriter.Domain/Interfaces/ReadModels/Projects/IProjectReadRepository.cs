using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.ReadModels.Projects;

public interface IProjectReadRepository
{
    Task<IReadOnlyList<ProjectDto>> GetUserProjectsAsync(Guid userId, CancellationToken ct);

    Task<ProjectDto?> GetProjectByIdAsync(Guid projectId, Guid userId, CancellationToken ct);

    Task<Project?> GetUserProjectByIdAsync(Guid projectId, Guid userId, CancellationToken ct);
}