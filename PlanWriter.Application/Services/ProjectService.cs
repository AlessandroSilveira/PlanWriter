// Services/ProjectService.cs

using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Services
{
    public class ProjectService(IProjectRepository projectRepo, IProjectProgressRepository progressRepo, IUserService userService)
        : IProjectService
    {
        

        public async Task<Project> CreateProjectAsync(CreateProjectDto dto, ClaimsPrincipal user)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Genre = dto.Genre,
                WordCountGoal = dto.WordCountGoal,
                Deadline = dto.Deadline,
                UserId = userService.GetUserId(user),
                CurrentWordCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await projectRepo.CreateAsync(project);
            return project;
        }

        public async Task<IEnumerable<ProjectDto>> GetUserProjectsAsync(ClaimsPrincipal user)
        {
            var userId = userService.GetUserId(user);

            var dados = await projectRepo.GetUserProjectsAsync(userId);
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
                        : 0,
                    Genre = p.Genre
                }).ToList();
        }

        public async Task<ProjectDto> GetProjectByIdAsync(Guid id, ClaimsPrincipal user)
        {
            var userId = userService.GetUserId(user);
            var project = await projectRepo.GetUserProjectByIdAsync(id, userId);

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
                    : 0,
                Genre = project.Genre
                    
            };
        }

        public async Task AddProgressAsync(AddProjectProgressDto dto, ClaimsPrincipal user)
        {
            var userId = userService.GetUserId(user);
            var project = await projectRepo.GetUserProjectByIdAsync(dto.ProjectId, userId);

            if (project is null)
                throw new KeyNotFoundException("Project not found");

            var progress = new ProjectProgress
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                WordsWritten = dto.WordsWritten.Value,
                Date = dto.Date,
                Notes = dto.Notes
            };

            project.CurrentWordCount += dto.WordsWritten.Value;

            await progressRepo.AddProgressAsync(progress);
        }

        public async Task<IEnumerable<ProgressHistoryDto>> GetProgressHistoryAsync(Guid projectId, ClaimsPrincipal user)
        {
            var userId = userService.GetUserId(user);
            var history = await progressRepo.GetProgressHistoryAsync(projectId, userId);

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
            return await projectRepo.SetGoalAsync(projectId, userId, wordCountGoal, deadline);
        }

        public async Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, string userId)
        {
            return await projectRepo.GetStatisticsAsync(projectId, userId);
        }

        public async Task<bool> DeleteProjectAsync(Guid projectId, string userId)
        {
            return await projectRepo.DeleteProjectAsync(projectId, userId);
        }

        public async Task<bool> DeleteProgressAsync(Guid progressId, string userId)
        {
            // Buscar progresso
            var progress = await progressRepo.GetByIdAsync(progressId, userId);
            if (progress == null)
                return false;

            var projectId = progress.ProjectId;
            var date = progress.Date;

            // Excluir progresso
            var deleted = await progressRepo.DeleteAsync(progressId, userId);
            if (!deleted) return false;

            // Buscar último progresso anterior
            var lastProgress = await progressRepo.GetLastProgressBeforeAsync(projectId, date);

            // Atualizar projeto
            var project = await projectRepo.GetUserProjectByIdAsync(projectId, userId);
            if (project != null)
            {
                project.CurrentWordCount = lastProgress?.WordsWritten ?? 0;
                await projectRepo.UpdateAsync(project); // ✅ agora existe
            }

            return true;
        }

        public async Task<ProjectStatsDto> GetStatsAsync(Guid projectId, ClaimsPrincipal user)
        {
            var userId = userService.GetUserId(user);
            var project = await projectRepo.GetUserProjectByIdAsync(projectId, userId);


            if (project == null)
                return null;

            var entries = await progressRepo.GetProgressByProjectIdAsync(projectId, userId);
            if (entries == null)
            {
                return new ProjectStatsDto
                {
                    TotalWords = 0,
                    AveragePerDay = 0,
                    BestDay = null,
                    ActiveDays = 0,
                    WordsRemaining = project.WordCountGoal
                };
            }

            var totalWords = entries.Sum(p => p.WordsWritten);
            var groupedByDate = entries
                .GroupBy(p => p.Date.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(p => p.WordsWritten) })
                .ToList();

            var averagePerDay = groupedByDate.Any()
                ? (int)groupedByDate.Average(g => g.Total)
                : 0;

            var bestDay = groupedByDate
                .OrderByDescending(g => g.Total)
                .FirstOrDefault();

            var wordsRemaining = project.WordCountGoal.HasValue
                ? Math.Max(0, project.WordCountGoal.Value - totalWords)
                : 0;

            var activeDays = groupedByDate.Count;

            // 💬 Mensagem motivacional
            string motivationMessage;
            if (groupedByDate.Any())
            {
                var lastEntryDate = groupedByDate.Max(g => g.Date);
                var daysSinceLast = (DateTime.Today - lastEntryDate).Days;

                if (daysSinceLast >= 3)
                    motivationMessage = "Vamos lá! Já faz alguns dias desde a última escrita.";
                else if (wordsRemaining == 0)
                    motivationMessage = "Parabéns! Você alcançou sua meta! 🎉";
                else if (averagePerDay >= 500)
                    motivationMessage = "Ótimo ritmo! Mantenha esse foco!";
                else
                    motivationMessage = "Cada palavra conta. Continue escrevendo!";
            }
            else
            {
                motivationMessage = "Que tal começar hoje mesmo sua primeira escrita?";
            }

            return new ProjectStatsDto
            {
                TotalWords = totalWords,
                AveragePerDay = averagePerDay,
                BestDay = bestDay != null
                    ? new BestDayDto
                    {
                        Date = bestDay.Date.ToString("yyyy-MM-dd"),
                        Words = bestDay.Total
                    }
                    : null,
                ActiveDays = activeDays,
                WordsRemaining = wordsRemaining,
                MotivationMessage = motivationMessage
            };
        }
private static ProjectDto MapToDto(Project p) => new ProjectDto
    {
        Id = p.Id,
        Title = p.Title,
        Description = p.Description,
        CurrentWordCount = p.CurrentWordCount,
        WordCountGoal = p.WordCountGoal,
        Deadline = p.Deadline,
        ProgressPercent = p.WordCountGoal.HasValue && p.WordCountGoal.Value > 0
            ? (double)p.CurrentWordCount / p.WordCountGoal.Value * 100
            : 0,
        Genre = p.Genre,
        HasCover = p.CoverBytes != null && p.CoverBytes.Length > 0,
        CoverUpdatedAt = p.CoverUpdatedAt
    };

    // Onde retornar listas de projetos, aplique .Select(MapToDto)
    // Onde retornar 1 projeto por id, aplique MapToDto

    public async Task UploadCoverAsync(Guid projectId, ClaimsPrincipal user, byte[] bytes, string mime, int size)
    {
        var userId = userService.GetUserId(user);
        var p = await projectRepo.GetUserProjectByIdAsync(projectId, userId)
                ?? throw new InvalidOperationException("Projeto não encontrado ou sem permissão");

        p.CoverBytes = bytes;
        p.CoverMime = mime;
        p.CoverSize = size;
        p.CoverUpdatedAt = DateTime.UtcNow;

        await projectRepo.UpdateAsync(p);
    }

    public async Task DeleteCoverAsync(Guid projectId, ClaimsPrincipal user)
    {
        var userId = userService.GetUserId(user);
        var p = await projectRepo.GetUserProjectByIdAsync(projectId, userId)
                ?? throw new InvalidOperationException("Projeto não encontrado ou sem permissão");

        p.CoverBytes = null;
        p.CoverMime = null;
        p.CoverSize = null;
        p.CoverUpdatedAt = null;

        await projectRepo.UpdateAsync(p);
    }

    public async Task<(byte[] bytes, string mime, int size, DateTime? updatedAt)?> GetCoverAsync(Guid projectId, ClaimsPrincipal user)
    {
        var userId = userService.GetUserId(user);
        var p = await projectRepo.GetUserProjectByIdAsync(projectId, userId);
        if (p == null || p.CoverBytes == null) return null;

        return (p.CoverBytes, p.CoverMime ?? "application/octet-stream", p.CoverSize ?? p.CoverBytes.Length, p.CoverUpdatedAt);
    }



    public async Task SetFlexibleGoalAsync(Guid projectId, Guid userId, int goalAmount, GoalUnit goalUnit, DateTime? deadline, CancellationToken ct = default)
    {
        if (goalAmount < 0) throw new ArgumentOutOfRangeException(nameof(goalAmount), "GoalAmount deve ser >= 0.");

        var owns = await projectRepo.UserOwnsProjectAsync(projectId, userId, ct);
        if (!owns) throw new UnauthorizedAccessException("Você não tem permissão para alterar este projeto.");

        await projectRepo.UpdateFlexibleGoalAsync(projectId, goalAmount, goalUnit, deadline, ct);
    }
    }
}