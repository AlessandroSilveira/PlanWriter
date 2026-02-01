using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Infrastructure.Repositories
{
    public class ProjectRepository(AppDbContext context, IDbConnectionFactory connectionFactory, IDbExecutor db) : Repository<Project>(context), IProjectRepository

    {
        // public async Task<Project> CreateAsync(Project project)
        // {
        //     project.Id = Guid.NewGuid();
        //     project.CreatedAt = DateTime.UtcNow;
        //
        //     await DbSet.AddAsync(project);
        //     await Context.SaveChangesAsync();
        //
        //     return project;
        // }
        public async Task<Project> CreateAsync(Project project, CancellationToken ct)
        {
            const string sql = @"
                INSERT INTO Projects
                (
                    Id,
                    UserId,
                    Title,
                    Description,
                    Genre,
                    WordCountGoal,
                    GoalAmount,
                    GoalUnit,
                    StartDate,
                    Deadline,
                    CreatedAt,
                    CurrentWordCount,
                    CoverBytes,
                    CoverUpdatedAt,
                    IsPublic
                )
                VALUES
                (
                    @Id,
                    @UserId,
                    @Title,
                    @Description,
                    @Genre,
                    @WordCountGoal,
                    @GoalAmount,
                    @GoalUnit,
                    @StartDate,
                    @Deadline,
                    @CreatedAt,
                    @CurrentWordCount,
                    @CoverBytes,
                    @CoverUpdatedAt,
                    @IsPublic
                );";

            using var conn = connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            
            var affected = await db.ExecuteAsync(
                conn,
                sql,
                new
                {
                    project.Id,
                    project.UserId,
                    project.Title,
                    project.Description,
                    project.Genre,
                    project.WordCountGoal,
                    project.GoalAmount,
                    GoalUnit = (int)project.GoalUnit,
                    project.StartDate,
                    project.Deadline,
                    project.CreatedAt,
                    project.CurrentWordCount,
                    project.CoverBytes,
                    project.CoverUpdatedAt,
                    project.IsPublic
                },
                transaction: null,
                ct
            );

            return affected != 1 ? throw new InvalidOperationException($"Insert Projects expected 1 row, affected={affected}.") : project;
        }
        
        public async Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId)
        {
            return await DbSet
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project> GetProjectWithProgressAsync(Guid id, Guid userId)
        {
            return await DbSet
                .Include(p => p.ProgressEntries)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        }
        
        // public async Task<Project> GetUserProjectByIdAsync(Guid id, Guid userId)
        // {
        //     return await DbSet
        //         .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        // }
        
        public async Task<bool> SetGoalAsync(Guid projectId, Guid userId, int wordCountGoal, DateTime? deadline = null)
        {
            var project = await DbSet
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

            if (project == null)
                return false;

            project.WordCountGoal = wordCountGoal;
            project.Deadline = deadline;

            Context.Update(project);
            await Context.SaveChangesAsync();

            return true;
        }
        
        public async Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, Guid userId)
        {
            var project = await DbSet
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
        
        public async Task<bool> DeleteProjectAsync(Guid projectId, Guid userId)
        {
            var project = await DbSet
                .Include(p => p.ProgressEntries)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

            if (project == null)
                return false;

            DbSet.Remove(project);
            await Context.SaveChangesAsync();

            return true;
        }
        public async Task<Project?> GetProjectById(Guid id)
        {
            return await DbSet
                .FirstOrDefaultAsync(p => p.Id == id) ;
        }
        
        public async Task ApplyValidationAsync(Guid projectId, ValidationResultDto res, CancellationToken ct)
        {
            var p = await DbSet.FirstOrDefaultAsync(x => x.Id == projectId, ct)
                    ?? throw new KeyNotFoundException("Projeto não encontrado.");

            p.ValidatedWords = res.Words;
            p.ValidatedAtUtc = res.ValidatedAtUtc;
            p.ValidationPassed = res.MeetsGoal;

            await Context.SaveChangesAsync(ct);
        }
        
        public async Task<(int? goalWords, string? title)> GetGoalAndTitleAsync(Guid projectId, CancellationToken ct)
        {
            var proj = await DbSet
                .Where(p => p.Id == projectId)
                .Select(p => new { p.WordCountGoal, p.Title })
                .FirstOrDefaultAsync(ct);

            return proj is null ? throw new KeyNotFoundException("Projeto não encontrado.") : (proj.WordCountGoal, proj.Title);
        }

        public async Task SaveValidationAsync(Guid projectId, int words, bool passed, DateTime utcNow, CancellationToken ct)
        {
            var proj = await DbSet.FirstOrDefaultAsync(p => p.Id == projectId, ct)
                       ?? throw new KeyNotFoundException("Projeto não encontrado.");

            proj.ValidatedWords = words;
            proj.ValidatedAtUtc = utcNow;
            proj.ValidationPassed = passed;

            await Context.SaveChangesAsync(ct);
        }
        
        public async Task<(int goalAmount, GoalUnit unit)> GetGoalAsync(Guid projectId, CancellationToken ct)
        {
            var row = await DbSet
                .Where(p => p.Id == projectId)
                .Select(p => new { p.GoalAmount, p.GoalUnit })
                .FirstOrDefaultAsync(ct);
            if (row is null) throw new KeyNotFoundException("Projeto não encontrado.");
            return (row.GoalAmount, row.GoalUnit);
        }

        public async Task<bool> UserOwnsProjectAsync(Guid projectId, Guid userId, CancellationToken ct)
        {
            return await DbSet.AnyAsync(p => p.Id == projectId && p.UserId == userId, ct);
            // ^ ajuste 'UserId' se seu Project usa outro campo para dono
        }

        public async Task UpdateFlexibleGoalAsync(Guid projectId, int goalAmount, GoalUnit unit, DateTime? deadline, CancellationToken ct)
        {
            var p = await DbSet.FirstOrDefaultAsync(x => x.Id == projectId, ct)
                    ?? throw new KeyNotFoundException("Projeto não encontrado.");

            p.GoalAmount = goalAmount;
            p.GoalUnit   = unit;
            if (deadline.HasValue) p.Deadline = deadline.Value; // ajuste o nome do campo se diferente

            await Context.SaveChangesAsync(ct);
        }

        public async Task<List<Project>> GetByUserIdAsync(Guid userId)
        {
            return await DbSet
                .Where(p =>  p.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetPublicProjectsByUserIdAsync(Guid userId)
        {
            return await DbSet
                .Where(p =>  p.UserId == userId && p.IsPublic == true)
                .ToListAsync();
        }

        public async Task<Project?> GetUserProjectByIdAsync(Guid projectId, Guid userId)
        {
            const string sql = @"
            SELECT TOP 1
                p.Id,
                p.UserId,
                p.Title,
                p.Description,
                p.CurrentWordCount,
                p.WordCountGoal,
                p.GoalAmount,
                p.GoalUnit,
                p.StartDate,
                p.Deadline,
                p.Genre,
                p.CoverUpdatedAt
            FROM Projects p
            WHERE p.Id = @projectId
              AND p.UserId = @userId;
            ";

            return await connection.QueryFirstOrDefaultAsync<Project>(sql, new { projectId, userId });
        }

        // public async Task UpdateAsync(Project project)
        // {
        //     // aqui atualizo o necessário pro handler
        //     const string sql = @"
        //         UPDATE Projects
        //         SET CurrentWordCount = @CurrentWordCount
        //         WHERE Id = @Id;
        //         ";
        //
        //     await connection.ExecuteAsync(sql, new { project.Id, project.CurrentWordCount });
        // }
        
        public async Task UpdateAsync(Project project, CancellationToken ct)
        {
            // 🔐 Ownership garantido pelo WHERE UserId
            const string sql = @"
                UPDATE Projects
                SET
                    Title               = @Title,
                    Description         = @Description,
                    Genre               = @Genre,
                    WordCountGoal       = @WordCountGoal,
                    GoalAmount          = @GoalAmount,
                    GoalUnit            = @GoalUnit,
                    StartDate           = @StartDate,
                    Deadline            = @Deadline,
                    CurrentWordCount    = @CurrentWordCount,
                    CoverUpdatedAt      = @CoverUpdatedAt
                WHERE Id = @Id
                  AND UserId = @UserId;
                ";

            using var conn = connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            
            var affected = await db.ExecuteAsync(
                conn,
                sql,
                new
                {
                    project.Id,
                    project.UserId,
                    project.Title,
                    project.Description,
                    project.Genre,
                    project.WordCountGoal,
                    project.GoalAmount,
                    GoalUnit = (int)project.GoalUnit,
                    project.StartDate,
                    project.Deadline,
                    project.CurrentWordCount,
                    project.CoverBytes,
                    project.CoverUpdatedAt,
                    project.IsPublic
                },
                transaction: null,
                ct
            );

            if (affected == 0)
            {
                throw new InvalidOperationException("Project not found or not owned by user.");
            }
        }
    }
}