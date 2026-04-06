using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Infrastructure.Persistence.Configurations;

public class OrganizerProfileConfiguration : IEntityTypeConfiguration<OrganizerProfile>
{
    public void Configure(EntityTypeBuilder<OrganizerProfile> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.UserId).IsUnique();
        builder.HasIndex(o => o.VerificationStatus);
        builder.Property(o => o.OrganizationName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Email).IsRequired().HasMaxLength(150);
        builder.Property(o => o.LegalDocumentUrl).HasMaxLength(500);
        builder.Property(o => o.RejectionReason).HasMaxLength(1000);
    }
}
