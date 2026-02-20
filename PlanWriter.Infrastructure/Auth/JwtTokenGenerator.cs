using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;

namespace PlanWriter.Infrastructure.Auth;

public class JwtTokenGenerator(
    IOptions<JwtOptions> jwtOptions,
    IOptions<AuthTokenOptions> tokenOptions) : IJwtTokenGenerator
{
    public string Generate(User user, bool adminMfaVerified = false)
    {
        var options = jwtOptions.Value;
        var now = DateTime.UtcNow;
        var nowEpoch = EpochTime.GetIntDate(now);
        var accessTokenMinutes = Math.Max(1, tokenOptions.Value.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(options.Key))
        {
            KeyId = options.CurrentKid
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FirstName),
            new("isAdmin", user.IsAdmin.ToString().ToLowerInvariant()),
            new("adminMfaVerified", (!user.IsAdmin || adminMfaVerified).ToString().ToLowerInvariant()),
            new("mustChangePassword", user.MustChangePassword.ToString().ToLowerInvariant()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, nowEpoch.ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf, nowEpoch.ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(accessTokenMinutes),
            SigningCredentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256Signature
            ),
            Audience = options.Audience,
            Issuer = options.Issuer
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
