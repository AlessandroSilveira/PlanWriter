using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public sealed class UserRepository(IDbExecutor db) : IUserRepository
{
    public Task CreateAsync(User user, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO Users
            (
                Id,
                FirstName,
                LastName,
                DateOfBirth,
                Email,
                PasswordHash,
                Bio,
                AvatarUrl,
                IsProfilePublic,
                Slug,
                DisplayName,
                IsAdmin,
                MustChangePassword
            )
            VALUES
            (
                @Id,
                @FirstName,
                @LastName,
                @DateOfBirth,
                @Email,
                @PasswordHash,
                @Bio,
                @AvatarUrl,
                @IsProfilePublic,
                @Slug,
                @DisplayName,
                @IsAdmin,
                @MustChangePassword
            );
        ";

        return db.ExecuteAsync(sql, user, ct: ct);
    }

    public Task UpdateAsync(User user, CancellationToken ct)
    {
        const string sql = @"
            UPDATE Users
            SET
                FirstName = @FirstName,
                LastName = @LastName,
                DateOfBirth = @DateOfBirth,
                Email = @Email,
                PasswordHash = @PasswordHash,
                Bio = @Bio,
                AvatarUrl = @AvatarUrl,
                IsProfilePublic = @IsProfilePublic,
                Slug = @Slug,
                DisplayName = @DisplayName,
                IsAdmin = @IsAdmin,
                MustChangePassword = @MustChangePassword
            WHERE Id = @Id;
        ";

        return db.ExecuteAsync(sql, user, ct: ct);
    }
}