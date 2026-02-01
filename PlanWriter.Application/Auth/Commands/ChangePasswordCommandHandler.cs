using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Auth.Commands;

public class ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher<User> passwordHasher,
    IJwtTokenGenerator tokenGenerator, ILogger<ChangePasswordCommandHandler> logger) : IRequestHandler<ChangePasswordCommand, string>
{
    public async Task<string> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Request.NewPassword) || request.Request.NewPassword.Length < 6)
        {
            logger.LogWarning("Change password failed: password must have at least 6 characters");
            throw new InvalidOperationException("Password must have at least 6 characters.");
        }

        var user = await userRepository.GetByIdAsync(request.UserId);

        if (user is null)
        {
            logger.LogWarning("Change password failed: user {UserId} not found", request.UserId);
            throw new InvalidOperationException("User not found");
        }

        user.ChangePassword(passwordHasher.HashPassword(user, request.Request.NewPassword));

        await userRepository.UpdateAsync(user);

        logger.LogInformation("Password changed for user {UserId}", user.Id);

        return tokenGenerator.Generate(user);
    }
}