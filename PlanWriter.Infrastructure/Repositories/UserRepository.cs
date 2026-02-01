using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<bool> EmailExistsAsync(string email) 
        => await context.Users.AnyAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }
    
    public async Task<User?> GetByEmailAsync(string email) 
        => await context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(Guid userId) 
        => await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

    public async Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task<List<User>> GetUsersByIdsAsync(IEnumerable<Guid> buddyIds)
    {
        return await context.Users.Where(u => buddyIds.Contains(u.Id)).ToListAsync();
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid userId)
    {
        return await context.Users.AnyAsync(a => a.Id != userId && a.Slug == slug);
    }

    public Task<User?> GetBySlugAsync(string requestSlug)
    {
       return context.Users.FirstOrDefaultAsync(s => s.Slug == requestSlug);
    }
}