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
using AddProjectProgressDto = PlanWriter.Application.DTO.AddProjectProgressDto;
using CreateProjectDto = PlanWriter.Application.DTO.CreateProjectDto;

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
        public string GetUserId(ClaimsPrincipal user) =>
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
                Date = dto.Date,
                Notes = dto.Notes
            };

            project.CurrentWordCount += dto.WordsWritten;
            
            await _progressRepo.AddProgressAsync(progress);
        }

        public async Task<IEnumerable<ProgressHistoryDto>> GetProgressHistoryAsync(Guid projectId, ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            var history = await _progressRepo.GetProgressHistoryAsync(projectId, userId);

            return history
                .Select(p => new ProgressHistoryDto
                {
                    Date = p.Date,
                    WordsWritten = p.WordsWritten
                })
                .OrderBy(p => p.Date)
                .ToList();
        }

        public async Task<bool> SetGoalAsync(Guid projectId, string userId, int wordCountGoal, DateTime? deadline)
        {
            return await _projectRepo.SetGoalAsync(projectId, userId, wordCountGoal, deadline);
        }
        public async Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, string userId)
        {
            return await _projectRepo.GetStatisticsAsync(projectId, userId);
        }

        public async Task<bool> DeleteProjectAsync(Guid projectId, string userId)
        {
            return await _projectRepo.DeleteProjectAsync(projectId, userId);
        }
        public async Task<bool> DeleteProgressAsync(Guid progressId, string userId)
        {
            // Buscar progresso
            var progress = await _progressRepo.GetByIdAsync(progressId, userId);
            if (progress == null)
                return false;

            var projectId = progress.ProjectId;
            var date = progress.Date;

            // Excluir progresso
            var deleted = await _progressRepo.DeleteAsync(progressId, userId);
            if (!deleted) return false;

            // Buscar último progresso anterior
            var lastProgress = await _progressRepo.GetLastProgressBeforeAsync(projectId, date);

            // Atualizar projeto
            var project = await _projectRepo.GetUserProjectByIdAsync(projectId, userId);
            if (project != null)
            {
                project.CurrentWordCount = lastProgress?.WordsWritten ?? 0;
                await _projectRepo.UpdateAsync(project); // ✅ agora existe
            }

            return true;
        }

        
    }
}
