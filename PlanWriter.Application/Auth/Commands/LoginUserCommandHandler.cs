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

public class LoginUserCommandHandler(IUserAuthReadRepository userReadRepository, IPasswordHasher<User> passwordHasher,
    IJwtTokenGenerator tokenGenerator, ILogger<LoginUserCommandHandler> logger)
    : IRequestHandler<LoginUserCommand, string?>
{
    public async Task<string?> Handle(LoginUserCommand request, CancellationToken ct)
    {
        var email = request.Request.Email.Trim().ToLowerInvariant();

        logger.LogInformation("Login attempt for user {Email}", email);

        var user = await userReadRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            logger.LogWarning("Login failed for {Email}: user not found", email);
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Request.Password);

        if (result != PasswordVerificationResult.Success)
        {
            logger.LogWarning("Login failed for {Email}: invalid password", email);
            return null;
        }

        var token = tokenGenerator.Generate(user);

        logger.LogInformation("User {Email} logged in successfully", email);

        return token;
    }
}