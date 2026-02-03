using System;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories.Auth.Register;

public class UserRegistrationRepository(IDbExecutor db) : IUserRegistrationRepository
{
    public async Task CreateAsync(User user, CancellationToken ct)
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
                IsAdmin,
                MustChangePassword,
                IsProfilePublic,
                Slug,
                DisplayName
            )
            VALUES
            (
                @Id,
                @FirstName,
                @LastName,
                @DateOfBirth,
                @Email,
                @PasswordHash,
                @IsAdmin,
                @MustChangePassword,
                @IsProfilePublic,
                @Slug,
                @DisplayName
            );
        ";

        var affected = await db.ExecuteAsync(
            sql,
            new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.DateOfBirth,
                user.Email,
                user.PasswordHash,
                user.IsAdmin,
                user.MustChangePassword,
                user.IsProfilePublic,
                user.Slug,
                user.DisplayName
            },
            ct
        );

        if (affected != 1)
            throw new InvalidOperationException(
                $"Insert Users expected 1 row, affected={affected}."
            );
    }
}