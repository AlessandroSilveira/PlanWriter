using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PlanWriter.Application.DTOs;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IConfiguration configuration,
    IPasswordHasher<User> passwordHasher)
    : IAuthService
{
    public async Task<string?> LoginAsync(LoginUserDto dto)
    {
        var user = await userRepository.GetByEmailAsync(dto.Email);
        //
        // if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        //     return null;

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

        if (result != PasswordVerificationResult.Success)
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            // padrão
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FirstName),

            // 🔑 CONTROLE DE ACESSO
            new Claim("isAdmin", user.IsAdmin ? "true" : "false"),
            new Claim("mustChangePassword", user.MustChangePassword ? "true" : "false")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Audience = configuration["Jwt:Audience"],
            Issuer = configuration["Jwt:Issuer"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<string> ChangePasswordAsync(Guid userId, string newPassword)
    {
        var user = await userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new Exception("User not found");

        // 🔐 GERA HASH USANDO PADRÃO ÚNICO DO SISTEMA
        user.ChangePassword(
            passwordHasher.HashPassword(user, newPassword)
        );

        await userRepository.UpdateAsync(user);

        // 🔑 GERA NOVO TOKEN (MustChangePassword = false para admin)
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FirstName),

            // 👑 ADMIN FLAG
            new Claim("isAdmin", user.IsAdmin ? "true" : "false"),

            // 🔐 SÓ ADMIN PODE TER ESSA FLAG (agora false)
            new Claim(
                "mustChangePassword",
                user.IsAdmin && user.MustChangePassword ? "true" : "false"
            )
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Audience = configuration["Jwt:Audience"],
            Issuer = configuration["Jwt:Issuer"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}