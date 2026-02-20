using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Application.Security;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;

namespace PlanWriter.Application.Auth.Commands;

public class RegisterUserCommandHandler(IUserRegistrationReadRepository readRepository, IUserRegistrationRepository writeRepository,
    IPasswordHasher<User> passwordHasher, ILogger<RegisterUserCommandHandler> logger)
    : IRequestHandler<RegisterUserCommand, bool>
{
    public async Task<bool> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var email = request.Request.Email.Trim().ToLowerInvariant();

        if (await readRepository.EmailExistsAsync(email, ct))
        {
            logger.LogWarning("Register failed: email {Email} already exists", email);

            throw new InvalidOperationException($"Register failed: email {email} already exists");
        }

        var passwordValidationError = PasswordPolicy.Validate(request.Request.Password);
        if (passwordValidationError is not null)
        {
            logger.LogWarning("Register failed: weak password for {Email}. Reason: {ValidationError}", email, passwordValidationError);
            throw new InvalidOperationException(passwordValidationError);
        }

        var user = MapRequestToUser(request, email);

        user.ChangePassword(passwordHasher.HashPassword(user, request.Request.Password));

        user.MakeRegularUser();

        await writeRepository.CreateAsync(user, ct);

        logger.LogInformation("User {Email} created with id {UserId}", email, user.Id);

        return true;
    }

    private static User MapRequestToUser(RegisterUserCommand request, string normalizedEmail)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.Request.FirstName,
            LastName = request.Request.LastName,
            DateOfBirth = request.Request.DateOfBirth,
            Email = normalizedEmail
        };
    }
}
