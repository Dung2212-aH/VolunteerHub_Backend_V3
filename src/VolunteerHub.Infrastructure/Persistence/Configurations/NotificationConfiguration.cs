using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.UserId, e.Status });
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Message).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.RelatedEntityType).HasMaxLength(100);
    }
}
