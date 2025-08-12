using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectProgressRepository : IRepository<ProjectProgress>
{
    Task<IEnumerable<ProjectProgress>> GetProgressByProjectIdAsync(Guid projectId, string userId);
}