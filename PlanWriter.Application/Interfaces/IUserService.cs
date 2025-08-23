using System.Security.Claims;
using System.Threading.Tasks;
using PlanWriter.Application.DTO;

namespace PlanWriter.Application.Interfaces;

public interface IUserService
{
    Task<bool> RegisterUserAsync(RegisterUserDto dto);
    string GetUserId(ClaimsPrincipal user);
}