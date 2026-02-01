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

public class LoginUserCommandHandler(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, IJwtTokenGenerator tokenGenerator,
    ILogger<LoginUserCommandHandler> logger) : IRequestHandler<LoginUserCommand, string?>
{
    public async Task<string?> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Login attempt for user {Email}", request.Request.Email);

        var user = await userRepository.GetByEmailAsync(request.Request.Email);

        if (user is null)
        {
            logger.LogWarning("Login failed for {Email}: user not found", request.Request.Email);
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Request.Password);

        if (result != PasswordVerificationResult.Success)
        {
            logger.LogWarning("Login failed for {Email}: invalid password", request.Request.Email);
            return null;
        }

        var token = tokenGenerator.Generate(user);

        logger.LogInformation("User {Email} logged in successfully", request.Request.Email);

        return token;
    }
}