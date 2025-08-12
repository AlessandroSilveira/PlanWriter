using System.Threading.Tasks;
using BCrypt.Net;
using PlanWriter.Application.DTO;
using PlanWriter.Application.Interfaces;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces;

namespace PlanWriter.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> RegisterUserAsync(RegisterUserDto dto)
    {
        if (await _userRepository.EmailExistsAsync(dto.Email))
            return false;

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        await _userRepository.AddAsync(user);
        return true;
    }
}