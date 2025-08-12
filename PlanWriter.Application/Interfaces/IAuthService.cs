using System.Threading.Tasks;
using PlanWriter.Application.DTOs;

namespace PlanWriter.Application.Interfaces;

public interface IAuthService
{
    Task<string?> LoginAsync(LoginUserDto dto);
}