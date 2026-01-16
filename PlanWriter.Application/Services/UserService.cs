using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;
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
    
    public Guid GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);

        if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
            throw new UnauthorizedAccessException("Usuário não autenticado.");

        if (!Guid.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("Identificador de usuário inválido.");

        return userId;
    }
}