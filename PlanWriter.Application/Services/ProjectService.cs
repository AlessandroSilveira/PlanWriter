// Services/ProjectService.cs

using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Interfaces.Repositories;


namespace PlanWriter.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepo;
        private readonly IProjectProgressRepository _progressRepo;
        public ProjectService(IProjectRepository projectRepo, IProjectProgressRepository progressRepo)
        {
            _projectRepo = projectRepo;
            _progressRepo = progressRepo;
        }
        private string GetUserId(ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException();

        public async Task CreateProjectAsync(CreateProjectDto dto, ClaimsPrincipal user)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                WordCountGoal = dto.WordCountGoal,
                Deadline = dto.Deadline,
                UserId = GetUserId(user),
                CurrentWordCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _projectRepo.CreateAsync(project);
        }

        public async Task<IEnumerable<ProjectDto>> GetUserProjectsAsync(ClaimsPrincipal user)
        {
            var userId = GetUserId(user);

            var dados = await _projectRepo.GetUserProjectsAsync(userId);
            return dados.Where(p => p.UserId == userId)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    CurrentWordCount = p.CurrentWordCount,
                    WordCountGoal = p.WordCountGoal,
                    Deadline = p.Deadline,
                    ProgressPercent = p.WordCountGoal.HasValue && p.WordCountGoal > 0
                        ? (double)p.CurrentWordCount / p.WordCountGoal.Value * 100
                        : 0
                }).ToList();
        }

        public async Task<ProjectDto> GetProjectByIdAsync(Guid id, ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            var project = await _projectRepo.GetUserProjectByIdAsync(id, userId);
            
            if (project is null) 
                throw new KeyNotFoundException("Project not found");

            return new ProjectDto
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                CurrentWordCount = project.CurrentWordCount,
                WordCountGoal = project.WordCountGoal,
                Deadline = project.Deadline,
                ProgressPercent = project.WordCountGoal.HasValue && project.WordCountGoal > 0
                    ? (double)project.CurrentWordCount / project.WordCountGoal.Value * 100
                    : 0
            };
        }

        public async Task AddProgressAsync(AddProjectProgressDto dto, ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            var project = await _projectRepo.GetUserProjectByIdAsync(dto.ProjectId, userId);
            
            if (project is null) 
                throw new KeyNotFoundException("Project not found");

            var progress = new ProjectProgress
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                WordsWritten = dto.WordsWritten,
                Date = dto.Date
            };

            project.CurrentWordCount += dto.WordsWritten;
            _progressRepo. .ProjectProgresses.Add(progress);

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProgressHistoryDto>> GetProgressHistoryAsync(Guid projectId, ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            return await _context.ProjectProgresses
                .Where(p => p.ProjectId == projectId && p.Project.UserId == userId)
                .OrderBy(p => p.Date)
                .Select(p => new ProgressHistoryDto
                {
                    Date = p.Date,
                    WordsWritten = p.WordsWritten
                })
                .ToListAsync();
        }

        public async Task SetGoalAsync(Guid projectId, SetGoalDto dto, ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
            if (project == null) throw new KeyNotFoundException("Project not found");

            project.WordCountGoal = dto.WordCountGoal;
            project.Deadline = dto.Deadline;
            await _context.SaveChangesAsync();
        }

        public async Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            var progressList = await _context.ProjectProgresses
                .Where(p => p.ProjectId == projectId && p.Project.UserId == userId)
                .ToListAsync();

            if (!progressList.Any())
                return new ProjectStatisticsDto();

            var totalWords = progressList.Sum(p => p.WordsWritten);
            var avgWords = progressList
                .GroupBy(p => p.Date.Date)
                .Average(g => g.Sum(x => x.WordsWritten));

            var mostProductive = progressList
                .GroupBy(p => p.Date.Date)
                .OrderByDescending(g => g.Sum(x => x.WordsWritten))
                .First().Key;

            var project = await _context.Projects.FirstAsync(p => p.Id == projectId);
            double? completion = project.WordCountGoal.HasValue
                ? (double)totalWords / project.WordCountGoal.Value * 100
                : null;

            return new ProjectStatisticsDto
            {
                TotalWordsWritten = totalWords,
                AverageWordsPerDay = Math.Round(avgWords, 2),
                MostProductiveDay = mostProductive,
                CompletionPercentage = completion
            };
        }

        public async Task DeleteProjectAsync(Guid id, ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (project == null) throw new KeyNotFoundException("Project not found");

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }
    }
}
