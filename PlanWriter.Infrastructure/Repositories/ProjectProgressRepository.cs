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
        
        public async Task<ProjectProgress> AddProgressAsync(ProjectProgress progress)
        {
            progress.Id = Guid.NewGuid();
            if (progress.Date == default)
                progress.Date = DateTime.UtcNow;

            await _dbSet.AddAsync(progress);
            await _context.SaveChangesAsync();

            return progress;
        }
        
        public async Task<IEnumerable<ProjectProgress>> GetProgressHistoryAsync(Guid projectId, string userId)
        {
            return await _dbSet
                .Include(pp => pp.Project)
                .Where(pp => pp.ProjectId == projectId && pp.Project.UserId == userId)
                .OrderBy(pp => pp.Date)
                .ToListAsync();
        }
        public async Task<ProjectProgress> GetByIdAsync(Guid id, string userId)
        {
            return await _dbSet
                .Include(p => p.Project)
                .FirstOrDefaultAsync(p => p.Id == id && p.Project.UserId == userId);
        }
        
        public async Task<ProjectProgress> GetLastProgressBeforeAsync(Guid projectId, DateTime date)
        {
            return await _dbSet
                .Where(p => p.ProjectId == projectId && p.Date < date)
                .OrderByDescending(p => p.Date)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteAsync(Guid id, string userId)
        {
            var progress = await GetByIdAsync(id, userId);
            if (progress == null)
                return false;

            _dbSet.Remove(progress);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}