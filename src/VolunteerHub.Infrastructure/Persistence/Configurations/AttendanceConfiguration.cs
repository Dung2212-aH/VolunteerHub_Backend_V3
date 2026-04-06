using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.EventId);
        builder.HasIndex(a => a.VolunteerProfileId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => new { a.EventId, a.VolunteerProfileId }).IsUnique();
        builder.HasOne(a => a.Event).WithMany().HasForeignKey(a => a.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.VolunteerProfile).WithMany().HasForeignKey(a => a.VolunteerProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}
