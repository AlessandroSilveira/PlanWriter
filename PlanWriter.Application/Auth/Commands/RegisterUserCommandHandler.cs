using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PlanWriter.Application.Auth.Dtos.Commands;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Auth.Commands;

public class RegisterUserCommandHandler(IUserRepository userRepository, ILogger<RegisterUserCommandHandler> logger, 
    IPasswordHasher<User> passwordHasher) 
    : IRequestHandler<RegisterUserCommand, bool>
{
    public async Task<bool> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.GetByEmailAsync(request.Request.Email) != null)
        {
            logger.LogWarning("User {Email} already exists", request.Request.Email);
            throw new InvalidOperationException("E-mail já cadastrado.");
        }

        var user = MapRequestToUser(request);
        
        ApplyPasswordHashToUser(request, user);
        
        user.MakeRegularUser();

        await userRepository.AddAsync(user);
        
        logger.LogInformation("User {Email} created", request.Request.Email);
        return true;
    }

    private void ApplyPasswordHashToUser(RegisterUserCommand request, User user)
    {
        user.ChangePassword(
            passwordHasher.HashPassword(user, request.Request.Password)
        );
    }

    private static User MapRequestToUser(RegisterUserCommand request)
    {
        var user = new User
        {
            FirstName = request.Request.FirstName,
            LastName = request.Request.LastName,
            DateOfBirth = request.Request.DateOfBirth,
            Email = request.Request.Email
        };
        return user;
    }
}