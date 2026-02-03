using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectProgressRepository : IRepository<ProjectProgress>
{
    Task<ProjectProgress> AddProgressAsync(ProjectProgress progress, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    
    
    
   
    
    
}