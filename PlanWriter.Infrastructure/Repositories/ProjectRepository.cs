using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces;
using PlanWriter.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
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
        
        public async Task<bool> SetGoalAsync(Guid projectId, string userId, int wordCountGoal, DateTime? deadline = null)
        {
            var project = await _dbSet
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

            if (project == null)
                return false;

            project.WordCountGoal = wordCountGoal;
            project.Deadline = deadline;

            _context.Update(project);
            await _context.SaveChangesAsync();

            return true;
        }
        
        public async Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, string userId)
        {
            var project = await _dbSet
                .Include(p => p.ProgressEntries)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

            if (project == null)
                return null;

            var totalWordsWritten = project.ProgressEntries.Sum(pe => pe.WordsWritten);
            var totalTimeSpent = project.ProgressEntries.Sum(pe => pe.TimeSpentInMinutes);
            var firstEntryDate = project.ProgressEntries.Min(pe => (DateTime?)pe.Date) ?? project.CreatedAt;
            var daysElapsed = Math.Max(1, (DateTime.UtcNow.Date - firstEntryDate.Date).Days + 1);
            var avgWordsPerDay = totalWordsWritten / (double)daysElapsed;

            var remainingWords = project.WordCountGoal.HasValue
                ? Math.Max(0, project.WordCountGoal.Value - totalWordsWritten)
                : 0;

            var progressPercentage = project.WordCountGoal.HasValue && project.WordCountGoal.Value > 0
                ? Math.Round((double)totalWordsWritten / project.WordCountGoal.Value * 100, 2)
                : 0;

            var daysRemaining = project.Deadline.HasValue
                ? (project.Deadline.Value.Date - DateTime.UtcNow.Date).Days
                : 0;

            return new ProjectStatisticsDto
            {
                TotalWordsWritten = totalWordsWritten,
                WordCountGoal = project.WordCountGoal,
                RemainingWords = remainingWords,
                ProgressPercentage = progressPercentage,
                AverageWordsPerDay = avgWordsPerDay,
                TotalTimeSpentInMinutes = totalTimeSpent,
                Deadline = project.Deadline,
                DaysRemaining = daysRemaining
            };
        }
        
        public async Task<bool> DeleteProjectAsync(Guid projectId, string userId)
        {
            var project = await _dbSet
                .Include(p => p.ProgressEntries)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

            if (project == null)
                return false;

            _dbSet.Remove(project);
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<Project?> GetProjectById(Guid id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Id == id) ;
        }
    }
}