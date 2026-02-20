using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;

namespace PlanWriter.Infrastructure.Auth;

public class JwtTokenGenerator(
    IConfiguration configuration,
    IOptions<AuthTokenOptions> tokenOptions) : IJwtTokenGenerator
{
    public string Generate(User user)
    {
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]!);
        var accessTokenMinutes = Math.Max(1, tokenOptions.Value.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FirstName),
            new("isAdmin", user.IsAdmin.ToString().ToLowerInvariant()),
            new("mustChangePassword", user.MustChangePassword.ToString().ToLowerInvariant())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Audience = configuration["Jwt:Audience"],
            Issuer = configuration["Jwt:Issuer"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
