using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Auth.Commands;

public class ChangePasswordCommandHandler(IUserReadRepository userReadRepository, IUserPasswordRepository passwordRepository,
    IPasswordHasher<User> passwordHasher, IJwtTokenGenerator tokenGenerator, ILogger<ChangePasswordCommandHandler> logger)
    : IRequestHandler<ChangePasswordCommand, string>
{
    public async Task<string> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var newPassword = request.Request.NewPassword;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            logger.LogWarning("Change password failed: password must have at least 6 characters");

            throw new InvalidOperationException("Password must have at least 6 characters.");
        }

        var user = await userReadRepository.GetByIdAsync(request.UserId, ct);

        if (user is null)
        {
            logger.LogWarning("Change password failed: user {UserId} not found", request.UserId);

            throw new InvalidOperationException("User not found");
        }

        var newHash = passwordHasher.HashPassword(user, newPassword);

        await passwordRepository.UpdatePasswordAsync(user.Id, newHash, ct);

        logger.LogInformation("Password changed for user {UserId}", user.Id);
        
        return tokenGenerator.Generate(user);
    }
   
}