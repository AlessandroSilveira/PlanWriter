using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Domain.Interfaces.ReadModels;

public interface IProjectReadRepository
{
    Task<List<ProjectDto>> GetUserProjectsAsync(Guid userId);
    Task<ProjectDto?> GetProjectByIdAsync(Guid projectId, Guid userId);
}