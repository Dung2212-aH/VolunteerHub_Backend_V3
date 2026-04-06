using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Domain.Common;
using VolunteerHub.Domain.Entities;
using VolunteerHub.Infrastructure.Identity;

namespace VolunteerHub.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<VolunteerProfile> VolunteerProfiles => Set<VolunteerProfile>();
    public DbSet<VolunteerSkill> VolunteerSkills => Set<VolunteerSkill>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventSkillRequirement> EventSkillRequirements => Set<EventSkillRequirement>();
    public DbSet<OrganizerProfile> OrganizerProfiles => Set<OrganizerProfile>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<EventApplication> EventApplications => Set<EventApplication>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<VolunteerBadge> VolunteerBadges => Set<VolunteerBadge>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<FeedbackReport> FeedbackReports => Set<FeedbackReport>();
    public DbSet<SponsorProfile> SponsorProfiles => Set<SponsorProfile>();
    public DbSet<SponsorContribution> SponsorContributions => Set<SponsorContribution>();
    public DbSet<EventSponsor> EventSponsors => Set<EventSponsor>();
    public DbSet<SkillCatalogItem> SkillCatalogItems => Set<SkillCatalogItem>();
    public DbSet<AdminActionLog> AdminActionLogs => Set<AdminActionLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var falseConstant = Expression.Constant(false);
                var condition = Expression.Equal(property, falseConstant);
                var lambda = Expression.Lambda(condition, parameter);
                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added: entry.Entity.CreatedAt = DateTime.UtcNow; break;
                case EntityState.Modified: entry.Entity.UpdatedAt = DateTime.UtcNow; break;
            }
        }
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
