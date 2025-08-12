using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectProgressEntry> ProjectProgressEntries { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Se tiver configurações personalizadas, adicione aqui
    }
}