using System;
using System.Threading.Tasks;
using PlanWriter.Application.DTO;

namespace PlanWriter.Application.Interfaces;

public interface IAuthService
{
    Task<string?> LoginAsync(LoginUserDto dto);
    Task<string> ChangePasswordAsync(Guid userId, string newPassword);
}
