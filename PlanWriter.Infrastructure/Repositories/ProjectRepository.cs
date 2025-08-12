using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces;
using PlanWriter.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Infrastructure.Repositories
{
    public class ProjectRepository(AppDbContext context) : Repository<Project>(context), IProjectRepository

    {
        public async Task<Project> CreateAsync(Project project)
        {
            project.Id = Guid.NewGuid();
            project.CreatedAt = DateTime.UtcNow;

            await _dbSet.AddAsync(project);
            await _context.SaveChangesAsync();

            return project;
        }
        
        public async Task<IEnumerable<Project>> GetUserProjectsAsync(string userId)
        {
            return await _dbSet
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project> GetProjectWithProgressAsync(Guid id, string userId)
        {
            return await _dbSet
                .Include(p => p.ProgressEntries)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        }
        
        public async Task<Project> GetUserProjectByIdAsync(Guid id, string userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        }

    }
}