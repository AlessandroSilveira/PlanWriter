using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using PlanWriter.Application.DTO;

namespace PlanWriter.Application.Interfaces;

public interface IUserService
{
    Task<bool> RegisterUserAsync(RegisterUserDto dto, CancellationToken ct);
    Guid GetUserId(ClaimsPrincipal user);
}