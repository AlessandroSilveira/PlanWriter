using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;

namespace PlanWriter.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectProgress> ProjectProgresses { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<Event> Events => Set<Event>();
    public DbSet<ProjectEvent> ProjectEvents => Set<ProjectEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.ProgressEntries)
            .WithOne(pp => pp.Project)
            .HasForeignKey(pp => pp.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        // Event
        modelBuilder.Entity<Event>(b =>
        {
            b.HasIndex(e => e.Slug).IsUnique();
            b.Property(e => e.Name).HasMaxLength(120).IsRequired();
            b.Property(e => e.Slug).HasMaxLength(80).IsRequired();
        });

        // ProjectEvent
        modelBuilder.Entity<ProjectEvent>(b =>
        {
            b.HasIndex(pe => new { pe.ProjectId, pe.EventId }).IsUnique();

            b.HasOne(pe => pe.Event)
                .WithMany(e => e.Participants)
                .HasForeignKey(pe => pe.EventId);

            b.HasOne(pe => pe.Project)
                .WithMany() // se já houver navegação, ajuste aqui
                .HasForeignKey(pe => pe.ProjectId);
        });
    }
}