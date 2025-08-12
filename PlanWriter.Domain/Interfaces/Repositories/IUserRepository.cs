using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(User user);
    Task<User?> GetByEmailAsync(string email);
}