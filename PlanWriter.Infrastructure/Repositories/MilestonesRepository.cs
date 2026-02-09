using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public sealed class MilestonesRepository(IDbExecutor db) : IMilestonesRepository
{
    public Task CreateAsync(Milestone m, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO Milestones
            (
                Id,
                ProjectId,
                Name,
                [Order]
            )
            VALUES
            (
                @Id,
                @ProjectId,
                @Name,
                @Order
            );
        ";

        if (m.Id == Guid.Empty)
            m.Id = Guid.NewGuid();

        return db.ExecuteAsync(sql, m, ct: ct);
    }

    public Task UpdateAsync(Milestone m, CancellationToken ct)
    {
        const string sql = @"
            UPDATE Milestones
            SET
                Name = @Name,
                [Order] = @Order
            WHERE Id = @Id;
        ";

        return db.ExecuteAsync(sql, m, ct: ct);
    }

    public Task DeleteAsync(Guid milestoneId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
        DELETE m
        FROM Milestones m
        INNER JOIN Projects p ON p.Id = m.ProjectId
        WHERE m.Id = @MilestoneId
          AND p.UserId = @UserId;
    ";

        return db.ExecuteAsync(
            sql,
            new
            {
                MilestoneId = milestoneId,
                UserId = userId
            },
            ct: ct
        );
    }

}