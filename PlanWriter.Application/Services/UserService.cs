using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Services;

public class UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task<bool> RegisterUserAsync(RegisterUserDto dto)
    {
        if (await userRepository.GetByEmailAsync(dto.Email) != null)
        {
            throw new InvalidOperationException("E-mail já cadastrado.");
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            Email = dto.Email,
            //PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        
        user.ChangePassword(
            passwordHasher.HashPassword(user, dto.Password)
        );
        
        // 👤 garante usuário comum
        user.MakeRegularUser();

        await userRepository.AddAsync(user);
        return true;
    }
    
    public string GetUserId(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();
}