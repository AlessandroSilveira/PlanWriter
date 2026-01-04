// Infrastructure/Repositories/Repository.cs
using Microsoft.EntityFrameworkCore;
using PlanWriter.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PlanWriter.Domain.Interfaces.Repositories;

namespace PlanWriter.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<T> DbSet;

        public Repository(AppDbContext context)
        {
            Context = context;
            DbSet = context.Set<T>();
        }

        public virtual async Task<T> GetByIdAsync(Guid id) => await DbSet.FindAsync(id);

        public virtual async Task<IEnumerable<T>> GetAllAsync() => await DbSet.ToListAsync();

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            await DbSet.Where(predicate).ToListAsync();

        public virtual async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

        public virtual void Update(T entity) => DbSet.Update(entity);

        public virtual void Remove(T entity) => DbSet.Remove(entity);

        public virtual async Task<int> SaveChangesAsync() => await Context.SaveChangesAsync();
        
        public virtual async Task UpdateAsync(T entity)
        {
            DbSet.Update(entity);
            await Context.SaveChangesAsync();
        }
    }
}