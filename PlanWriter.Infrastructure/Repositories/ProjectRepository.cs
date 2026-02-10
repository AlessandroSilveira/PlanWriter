using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using PlanWriter.Domain.Dtos;
using PlanWriter.Domain.Dtos.Projects;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Enums;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories
{
    public class ProjectRepository(IDbConnectionFactory connectionFactory, IDbExecutor db)
        : IProjectRepository
    {
        private sealed record ProjectGoalTitleRow(int? WordCountGoal, string? Title);
        private sealed record ProjectGoalRow(int GoalAmount, int GoalUnit);
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

            var affected = await db.ExecuteAsync(
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
                ct
            );

            return affected != 1 ? throw new InvalidOperationException($"Insert Projects expected 1 row, affected={affected}.") : project;
        }

        public Task<IReadOnlyList<Project>> GetUserProjectsAsync(Guid userId, CancellationToken ct)
        {
            const string sql = @"
                SELECT
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
                    IsPublic,
                    ValidatedWords,
                    ValidatedAtUtc,
                    ValidationPassed
                FROM Projects
                WHERE UserId = @UserId
                ORDER BY CreatedAt DESC;
            ";

            return db.QueryAsync<Project>(sql, new { UserId = userId }, ct);
        }

        public async Task<Project?> GetProjectWithProgressAsync(Guid id, Guid userId, CancellationToken ct)
        {
            const string projectSql = @"
                SELECT TOP 1
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
                    IsPublic,
                    ValidatedWords,
                    ValidatedAtUtc,
                    ValidationPassed
                FROM Projects
                WHERE Id = @Id
                  AND UserId = @UserId;
            ";

            var project = await db.QueryFirstOrDefaultAsync<Project>(projectSql, new { Id = id, UserId = userId }, ct);
            if (project is null)
                return null;

            const string progressSql = @"
                SELECT
                    Id,
                    ProjectId,
                    TotalWordsWritten,
                    RemainingWords,
                    RemainingPercentage,
                    CreatedAt,
                    [Date],
                    TimeSpentInMinutes,
                    WordsWritten,
                    Notes,
                    Minutes,
                    Pages
                FROM ProjectProgresses
                WHERE ProjectId = @ProjectId
                ORDER BY [Date] ASC;
            ";

            var progress = await db.QueryAsync<ProjectProgress>(progressSql, new { ProjectId = project.Id }, ct);
            project.ProgressEntries = progress.ToList();

            return project;
        }

        public async Task<bool> SetGoalAsync(Guid projectId, Guid userId, int goalAmount, DateTime? deadline, CancellationToken ct)
        {
            const string sql = @"
                UPDATE Projects
                SET
                    WordCountGoal = @GoalAmount,
                    GoalAmount    = @GoalAmount,
                    Deadline      = @Deadline
                WHERE Id = @ProjectId
                  AND UserId = @UserId;
            ";

            var affected = await db.ExecuteAsync(sql, new { ProjectId = projectId, UserId = userId, GoalAmount = goalAmount, Deadline = deadline }, ct);

            return affected == 1;
        }

        public async Task<ProjectStatisticsDto> GetStatisticsAsync(Guid projectId, Guid userId)
        {
            var project = await GetProjectWithProgressAsync(projectId, userId, CancellationToken.None);

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

        public async Task<bool> DeleteProjectAsync(Guid projectId, Guid userId, CancellationToken ct)
        {
            using var conn = connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                const string deleteProgressSql = @"
                    DELETE pp
                    FROM ProjectProgresses pp
                    INNER JOIN Projects p ON p.Id = pp.ProjectId
                    WHERE pp.ProjectId = @ProjectId
                      AND p.UserId = @UserId;
                ";

                await conn.ExecuteAsync(new CommandDefinition(deleteProgressSql, new { ProjectId = projectId, UserId = userId }, transaction: tx, cancellationToken: ct));

                const string deleteProjectEventsSql = @"
                    DELETE pe
                    FROM ProjectEvents pe
                    INNER JOIN Projects p ON p.Id = pe.ProjectId
                    WHERE pe.ProjectId = @ProjectId
                      AND p.UserId = @UserId;
                ";

                await conn.ExecuteAsync(new CommandDefinition(deleteProjectEventsSql, new { ProjectId = projectId, UserId = userId }, transaction: tx, cancellationToken: ct));

                const string deleteProjectSql = @"
                    DELETE FROM Projects
                    WHERE Id = @ProjectId
                      AND UserId = @UserId;
                ";

                var affected = await conn.ExecuteAsync(new CommandDefinition(deleteProjectSql, new { ProjectId = projectId, UserId = userId }, transaction: tx, cancellationToken: ct)
                );

                tx.Commit();
                return affected == 1;
            }
            catch
            {
                try { tx.Rollback(); } catch { /* ignore */ }
                throw;
            }
        }

        public Task<Project?> GetProjectById(Guid id)
        {
            const string sql = @"
                SELECT TOP 1
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
                    IsPublic,
                    ValidatedWords,
                    ValidatedAtUtc,
                    ValidationPassed
                FROM Projects
                WHERE Id = @Id;
            ";

            return db.QueryFirstOrDefaultAsync<Project>(sql, new { Id = id });
        }

        public async Task ApplyValidationAsync(Guid projectId, ValidationResultDto res, CancellationToken ct)
        {
            const string sql = @"
                UPDATE Projects
                SET
                    ValidatedWords = @Words,
                    ValidatedAtUtc = @ValidatedAtUtc,
                    ValidationPassed = @ValidationPassed
                WHERE Id = @ProjectId;
            ";

            var affected = await db.ExecuteAsync(sql, new
            {
                ProjectId = projectId,
                Words = res.Words,
                res.ValidatedAtUtc,
                ValidationPassed = res.MeetsGoal
            }, ct);

            if (affected == 0)
                throw new KeyNotFoundException("Projeto não encontrado.");
        }

        public async Task<(int? goalWords, string? title)> GetGoalAndTitleAsync(Guid projectId, CancellationToken ct)
        {
            const string sql = @"
                SELECT TOP 1
                    WordCountGoal,
                    Title
                FROM Projects
                WHERE Id = @ProjectId;
            ";

            var row = await db.QueryFirstOrDefaultAsync<ProjectGoalTitleRow>(sql, new { ProjectId = projectId }, ct);
            if (row is null)
                throw new KeyNotFoundException("Projeto não encontrado.");

            return (row.WordCountGoal, row.Title);
        }

        public async Task SaveValidationAsync(Guid projectId, int words, bool passed, DateTime utcNow,
            CancellationToken ct)
        {
            const string sql = @"
                UPDATE Projects
                SET
                    ValidatedWords = @Words,
                    ValidatedAtUtc = @ValidatedAtUtc,
                    ValidationPassed = @ValidationPassed
                WHERE Id = @ProjectId;
            ";

            var affected = await db.ExecuteAsync(sql, new
            {
                ProjectId = projectId,
                Words = words,
                ValidatedAtUtc = utcNow,
                ValidationPassed = passed
            }, ct);

            if (affected == 0)
                throw new KeyNotFoundException("Projeto não encontrado.");
        }

        public async Task<(int goalAmount, GoalUnit unit)> GetGoalAsync(Guid projectId, CancellationToken ct)
        {
            const string sql = @"
                SELECT TOP 1
                    GoalAmount,
                    GoalUnit
                FROM Projects
                WHERE Id = @ProjectId;
            ";

            var row = await db.QueryFirstOrDefaultAsync<ProjectGoalRow>(sql, new { ProjectId = projectId }, ct);
            if (row is null)
                throw new KeyNotFoundException("Projeto não encontrado.");

            return (row.GoalAmount, (GoalUnit)row.GoalUnit);
        }

        public async Task<bool> UserOwnsProjectAsync(Guid projectId, Guid userId, CancellationToken ct)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Projects
                WHERE Id = @ProjectId
                  AND UserId = @UserId;
            ";

            var count = await db.QueryFirstOrDefaultAsync<int>(sql, new { ProjectId = projectId, UserId = userId }, ct);
            return count > 0;
        }

        public async Task UpdateFlexibleGoalAsync(Guid projectId, int goalAmount, GoalUnit unit, DateTime? deadline,
            CancellationToken ct)
        {
            const string sql = @"
                UPDATE Projects
                SET
                    GoalAmount = @GoalAmount,
                    GoalUnit = @GoalUnit,
                    Deadline = @Deadline
                WHERE Id = @ProjectId;
            ";

            var affected = await db.ExecuteAsync(sql, new
            {
                ProjectId = projectId,
                GoalAmount = goalAmount,
                GoalUnit = (int)unit,
                Deadline = deadline
            }, ct);

            if (affected == 0)
                throw new KeyNotFoundException("Projeto não encontrado.");
        }

        public Task<IReadOnlyList<ProjectDto>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            const string sql = @"
                SELECT
                    Id,
                    Title,
                    Description,
                    CurrentWordCount,
                    WordCountGoal,
                    GoalAmount,
                    GoalUnit,
                    StartDate,
                    Deadline,
                    Genre,
                    CoverUpdatedAt,
                    IsPublic
                FROM Projects
                WHERE UserId = @UserId
                ORDER BY CreatedAt DESC;
            ";

            return db.QueryAsync<ProjectDto>(sql, new { UserId = userId }, ct: ct);
        }

        public Task<IReadOnlyList<Project>> GetPublicProjectsByUserIdAsync(Guid userId)
        {
            const string sql = @"
                SELECT
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
                    IsPublic,
                    ValidatedWords,
                    ValidatedAtUtc,
                    ValidationPassed
                FROM Projects
                WHERE UserId = @UserId
                  AND IsPublic = 1;
            ";

            return db.QueryAsync<Project>(sql, new { UserId = userId });
        }

        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = @"
                SELECT
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
                    IsPublic,
                    ValidatedWords,
                    ValidatedAtUtc,
                    ValidationPassed
                FROM Projects;
            ";

            return db.QueryAsync<Project>(sql, ct: ct);
        }

        public async Task UpdateAsync(Project project, CancellationToken ct)
        {
            const string sql = @"
                UPDATE Projects
                SET
                    Title = @Title,
                    Description = @Description,
                    Genre = @Genre,
                    WordCountGoal = @WordCountGoal,
                    GoalAmount = @GoalAmount,
                    GoalUnit = @GoalUnit,
                    StartDate = @StartDate,
                    Deadline = @Deadline,
                    CurrentWordCount = @CurrentWordCount,
                    CoverBytes = @CoverBytes,
                    CoverUpdatedAt = @CoverUpdatedAt,
                    IsPublic = @IsPublic
                WHERE
                    Id = @Id
                    AND UserId = @UserId;";

            var affected = await db.ExecuteAsync(
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
                ct
            );

            if (affected != 1)
                throw new InvalidOperationException($"Update Projects expected 1 row, affected={affected}.");
        }
    }
}
