using System;
using System.Collections.Generic;
using System.Linq;
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
            var response = await DbSet.AnyAsync(p => p.ProjectId == projectId
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
            await DbSet.AddRangeAsync(badges);
            await Context.SaveChangesAsync();   
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

    public Task<bool> FindAsync(Func<object, bool> func)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FindAsync(Guid projectId, object id, string evName)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> FindAsync(Guid projectId, Guid eventId, string name)
    {
      
       try
       {
           var response = await DbSet.AnyAsync(b => b.ProjectId == projectId && b.EventId == eventId
                                                                              && b.Name.Contains(name));
           return response;
       }
       catch (Exception e)
       {
           Console.WriteLine(e);
           throw;
       }
    }
}