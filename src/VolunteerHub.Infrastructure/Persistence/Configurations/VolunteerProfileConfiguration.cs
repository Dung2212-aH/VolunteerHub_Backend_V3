using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Infrastructure.Persistence.Configurations;

public class VolunteerProfileConfiguration : IEntityTypeConfiguration<VolunteerProfile>
{
    public void Configure(EntityTypeBuilder<VolunteerProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Phone).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Address).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Bio).HasMaxLength(2000);
        builder.Property(p => p.BloodGroup).HasMaxLength(10);
        builder.Property(p => p.Avatar).HasMaxLength(500);
        builder.Property(p => p.LanguagesText).HasMaxLength(1000);
        builder.Property(p => p.InterestsText).HasMaxLength(1000);
        builder.HasMany(p => p.Skills).WithOne(s => s.Profile).HasForeignKey(s => s.VolunteerProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class VolunteerSkillConfiguration : IEntityTypeConfiguration<VolunteerSkill>
{
    public void Configure(EntityTypeBuilder<VolunteerSkill> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => new { s.VolunteerProfileId, s.Name }).IsUnique();
    }
}
