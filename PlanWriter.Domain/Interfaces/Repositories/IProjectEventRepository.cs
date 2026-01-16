using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Events;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IProjectEventRepository
{
    Task<List<ProjectEvent>> GetByUserIdAsync(Guid userId);
}