using Microsoft.EntityFrameworkCore;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Events;
// se o enum estiver noutro namespace, ajuste abaixo:
using PlanWriter.Domain.Enums; // GoalUnit { Words=0, Minutes=1, Pages=2 }

namespace PlanWriter.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserFollow> UserFollows { get; set; } = default!;
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectProgress> ProjectProgresses { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<Event> Events => Set<Event>();
    public DbSet<ProjectEvent> ProjectEvents => Set<ProjectEvent>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Milestone> Milestones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== User =====
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(u => u.Id);

            b.Property(u => u.FirstName).IsRequired().HasMaxLength(80);
            b.Property(u => u.LastName).IsRequired().HasMaxLength(80);
            b.Property(u => u.DateOfBirth).IsRequired();
            b.Property(u => u.Email).IsRequired().HasMaxLength(254);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            b.Property(u => u.Bio).HasMaxLength(280);
            b.Property(u => u.AvatarUrl).HasMaxLength(256);
            b.Property(u => u.IsProfilePublic).HasDefaultValue(false);
            b.Property(u => u.Slug).HasMaxLength(80);
            b.HasIndex(u => u.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
            b.Property(u => u.DisplayName).HasMaxLength(100);
            b.HasIndex(u => new { u.FirstName, u.LastName });

            // Região (ON DELETE SET NULL)
            b.HasOne(u => u.Region)
             .WithMany(r => r.Users)
             .HasForeignKey(u => u.RegionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== UserFollow (ÚNICO BLOCO — sem duplicar) =====
        modelBuilder.Entity<UserFollow>(b =>
        {
            b.ToTable("UserFollows");
            b.HasKey(x => new { x.FollowerId, x.FolloweeId });

            b.HasOne(x => x.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(x => x.FollowerId)
                .OnDelete(DeleteBehavior.NoAction); // evita múltiplos cascades

            b.HasOne(x => x.Followee)
                .WithMany(u => u.Followers)
                .HasForeignKey(x => x.FolloweeId)
                .OnDelete(DeleteBehavior.NoAction); // evita múltiplos cascades

            b.HasCheckConstraint("CK_UserFollow_NoSelfFollow", "[FollowerId] <> [FolloweeId]");
            b.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        });

        // ===== Project (inclui metas flexíveis) =====
        modelBuilder.Entity<Project>(b =>
        {
            // relação com progressos
            b.HasMany(p => p.ProgressEntries)
             .WithOne(pp => pp.Project)
             .HasForeignKey(pp => pp.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);

            // metas flexíveis
            b.Property(p => p.GoalAmount)
             .HasDefaultValue(0);

            b.Property(p => p.GoalUnit)
             .HasConversion<byte>()       // enum -> tinyint
             .HasDefaultValue(GoalUnit.Words);

            // (opcional) sanity checks
            b.HasCheckConstraint("CK_Project_GoalAmount_NonNegative", "[GoalAmount] >= 0");
        });

        // ===== ProjectProgress (novos campos Minutes/Pages) =====
        modelBuilder.Entity<ProjectProgress>(b =>
        {
            b.Property(pp => pp.Minutes).HasDefaultValue(0);
            b.Property(pp => pp.Pages).HasDefaultValue(0);

            b.HasCheckConstraint("CK_Progress_Minutes_NonNegative", "[Minutes] >= 0");
            b.HasCheckConstraint("CK_Progress_Pages_NonNegative", "[Pages] >= 0");
        });

        // ===== Event =====
        modelBuilder.Entity<Event>(b =>
        {
            b.HasIndex(e => e.Slug).IsUnique();
            b.Property(e => e.Name).HasMaxLength(120).IsRequired();
            b.Property(e => e.Slug).HasMaxLength(80).IsRequired();
        });

        // ===== ProjectEvent =====
        modelBuilder.Entity<ProjectEvent>(b =>
        {
            b.HasIndex(pe => new { pe.ProjectId, pe.EventId }).IsUnique();

            b.HasOne(pe => pe.Event)
             .WithMany(e => e.Participants)
             .HasForeignKey(pe => pe.EventId);

            b.HasOne(pe => pe.Project)
             .WithMany() // ajuste se tiver navegação inversa
             .HasForeignKey(pe => pe.ProjectId);
        });

        // ===== Region =====
        modelBuilder.Entity<Region>(b =>
        {
            b.HasIndex(r => r.Slug).IsUnique();
            b.Property(r => r.Name).HasMaxLength(120).IsRequired();
            b.Property(r => r.Slug).HasMaxLength(80).IsRequired();
            b.Property(r => r.CountryCode).HasMaxLength(10);
        });
        modelBuilder.Entity<Milestone>(b =>
        {
            b.ToTable("Milestones");

            b.HasKey(x => x.Id);

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(80);

            b.Property(x => x.TargetAmount)
                .IsRequired();

            b.Property(x => x.Order)
                .HasDefaultValue(0);

            b.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            b.Property(x => x.ProjectId)
                .IsRequired();

            b.HasOne(x => x.Project)
                .WithMany(p => p.Milestones) // 🔴 IMPORTANTE
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔒 garante que não existam milestones duplicados no mesmo target
            b.HasIndex(x => new { x.ProjectId, x.TargetAmount })
                .IsUnique();
        });

        
        // modelBuilder.Entity<Milestone>(entity =>
        // {
        //     entity.HasKey(x => x.Id);
        //
        //     entity.Property(x => x.Name)
        //         .IsRequired()
        //         .HasMaxLength(200);
        //
        //     entity.Property(x => x.ProjectId);
        //
        //     entity.Property(x => x.TargetAmount)
        //         .IsRequired();
        //
        //     entity.Property(x => x.Order)
        //         .IsRequired();
        //
        //     entity.Property(x => x.CreatedAt)
        //         .IsRequired();
        //
        //     entity.HasIndex(x => new { x.ProjectId, x.Order })
        //         .IsUnique(false);
        //     modelBuilder.Entity<Milestone>()
        //         .HasIndex(m => new { m.ProjectId, m.Name });
        // });       
    }
}
