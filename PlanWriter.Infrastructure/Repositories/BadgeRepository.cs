using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class BadgeRepository(AppDbContext context) : Repository<Badge>(context), IBadgeRepository
{
    public async Task<bool> HasFirstStepsBadge(Guid projectId)
    {
        try
        {
            var response = await _dbSet.AnyAsync(p => p.ProjectId == projectId
                                                      && p.Name == "First Steps");
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
      
    }

    public async Task SaveBadges(List<Badge> badges)
    {
        try
        {
            await _dbSet.AddRangeAsync(badges);
            await _context.SaveChangesAsync();   
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
    }

    public async Task<IEnumerable<Badge>> GetBadgesByProjectIdAsync(Guid projectId)
    {
        try
        {
            var response = await FindAsync(p => p.ProjectId == projectId);
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}