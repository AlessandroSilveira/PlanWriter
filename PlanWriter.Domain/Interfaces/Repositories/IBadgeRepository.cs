using System.Collections.Generic;
using System.Threading.Tasks;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Repositories;

public interface IBadgeRepository
{
  
   
    Task SaveAsync(IEnumerable<Badge> badges);
   
}
