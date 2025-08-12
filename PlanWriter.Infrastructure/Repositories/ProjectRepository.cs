using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces;
using PlanWriter.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace PlanWriter.Infrastructure.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Project project)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
        }

        public async Task<Project?> GetByIdAsync(Guid id)
        {
            return await _context.Projects
                .Include(p => p.ProgressEntries)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdateAsync(Project project)
        {
            _context.Projects.Update(project);
            await _context.SaveChangesAsync();
        }

        public async Task AddProgressEntryAsync(ProjectProgressEntry entry)
        {
            _context.ProjectProgressEntries.Add(entry);
            await _context.SaveChangesAsync();
        }
    }
}