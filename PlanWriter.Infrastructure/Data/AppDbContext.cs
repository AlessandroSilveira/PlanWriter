using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;

namespace PlanWriter.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectProgress> ProjectProgresses { get; set; }
    public DbSet<Badge> Badges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.ProgressEntries)
            .WithOne(pp => pp.Project)
            .HasForeignKey(pp => pp.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}