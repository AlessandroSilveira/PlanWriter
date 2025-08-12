// Infrastructure/Repositories/ProjectProgressRepository.cs
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Infrastructure.Repositories
{
    public class ProjectProgressRepository(AppDbContext context)
        : Repository<ProjectProgress>(context), IProjectProgressRepository
    {
        public async Task<IEnumerable<ProjectProgress>> GetProgressByProjectIdAsync(Guid projectId, string userId)
        {
            return await _dbSet
                .Where(p => p.ProjectId == projectId && p.Project.UserId == userId)
                .OrderBy(p => p.Date)
                .ToListAsync();
        }
    }
}