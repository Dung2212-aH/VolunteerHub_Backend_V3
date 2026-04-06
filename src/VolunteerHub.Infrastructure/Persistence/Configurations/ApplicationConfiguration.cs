using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Infrastructure.Persistence.Configurations;

public class EventApplicationConfiguration : IEntityTypeConfiguration<EventApplication>
{
    public void Configure(EntityTypeBuilder<EventApplication> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Event).WithMany(ev => ev.Applications).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.VolunteerProfile).WithMany(v => v.Applications).HasForeignKey(e => e.VolunteerProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.EventId, e.VolunteerProfileId });
        builder.HasIndex(e => e.Status);
        builder.Property(e => e.MotivationText).HasMaxLength(1500);
        builder.Property(e => e.AvailabilityNote).HasMaxLength(500);
        builder.Property(e => e.ReviewNote).HasMaxLength(2000);
        builder.Property(e => e.RejectionReason).HasMaxLength(1000);
    }
}
