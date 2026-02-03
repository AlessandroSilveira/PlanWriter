using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Data;

namespace PlanWriter.Infrastructure.Repositories;

public class BadgeRepository(AppDbContext context) : IBadgeRepository
{
   

   

    public async Task SaveAsync(IEnumerable<Badge> badges)
    {
        await context.Set<Badge>().AddRangeAsync(badges);
        await context.SaveChangesAsync();
    }
}